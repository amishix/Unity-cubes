using System;
using System.Data;
using System.Numerics;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Video;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;
using Random = UnityEngine.Random;
using System.Collections.Generic;


public class SpawnScript : MonoBehaviour
{
    private const int RESOLUTION = 11;  // Number of spheres per side in the cube.
    private const int EXTENT = 4;       // Cube size: half of the size length.

    public float stopAfterSecs = 0;     // Time after which the update stops.
    public float strobeTime = 10f;      // Time duration for the strobe effect.
    public GameObject spherePrefab;     // Prefab for the spheres.

    // A mapping function to convert values from one range to another.
    readonly Func<float, float, float, float, float, float> map =
        (v, from1, to1, from2, to2) =>
            Mathf.Lerp(from2, to2, Mathf.InverseLerp(from1, to1, v));

    // Arrays to hold the spheres for each cube.
    private GameObject[,,] cube1, cube2, cube3, cube4;

    // Method to create a cube of spheres at the origin.
    private GameObject[,,] CreateCube() {
        GameObject[,,] result = new GameObject[RESOLUTION, RESOLUTION, RESOLUTION];
        for (int x = 0; x < RESOLUTION; x++) {
            for (int y = 0; y < RESOLUTION; y++) {
                for (int z = 0; z < RESOLUTION; z++) {
                    result[x, y, z] = Instantiate(spherePrefab, Vector3.zero, spherePrefab.transform.rotation);
                }
            }
        }
        return result;
    }

    // Method to update the position, scale, and color of spheres in a cube.
    private void UpdateCube(GameObject[,,] cube,
                            float xCentre,
                            float yCentre,
                            Func<Vector4, Vector4> colour,
                            Func<Vector4, Vector4> size,
                            Func<Vector4, Vector4> coords) {
        float t = 0; // Time variable for the strobe effect (currently not used).
        for (int x = 0; x < RESOLUTION; x++) {
            for (int y = 0; y < RESOLUTION; y++) {
                for (int z = 0; z < RESOLUTION; z++) {
                    // Normalized (-1..1) values for x, y, z ranges.
                    float normalX = map(x, 0, RESOLUTION - 1, -1, 1);
                    float normalY = map(y, 0, RESOLUTION - 1, -1, 1);
                    float normalZ = map(z, 0, RESOLUTION - 1, -1, 1);
                    Vector4 normals = new Vector4(normalX, normalY, normalZ, t);
                    Vector4 vCoords = coords(normals);
                    float xPos = map(vCoords.x, -1, 1, -EXTENT, EXTENT);
                    float yPos = map(vCoords.y, -1, 1, -EXTENT, EXTENT);
                    float zPos = map(vCoords.z, -1, 1, -EXTENT, EXTENT);

                    GameObject obj = cube[x, y, z];
                    obj.transform.position = new Vector3(xCentre + xPos, yCentre + yPos, zPos);
                    Vector4 thisSize = size(new Vector4(normalX, normalY, normalZ, t));
                    obj.transform.localScale = new Vector3(map(thisSize.x, -1, 1, 0, 1),
                                                           map(thisSize.y, -1, 1, 0, 1),
                                                           map(thisSize.z, -1, 1, 0, 1));
                    Renderer r = obj.GetComponent<Renderer>();
                    Vector4 v = colour(new Vector4(normalX, normalY, normalZ, t));
                    r.material.color = new Color(map(v.x, -1, 1, 0, 1),
                                                 map(v.y, -1, 1, 0, 1),
                                                 map(v.z, -1, 1, 0, 1));
                }
            }
        }
    }

    // Start is called before the first frame update
    void Start() {
        cube1 = CreateCube();
        cube2 = CreateCube();
        cube3 = CreateCube();
        cube4 = CreateCube();
    }

    // Update is called once per frame
    void Update() {
        if (Time.time <= stopAfterSecs) {
            // Update cube1: Color is randomized
            UpdateCube(cube1, -10, -10,
                       colour: (pos) => {
                           // Randomly assign grayscale color to each sphere.
                           float f = UnityEngine.Random.Range(-1f, 1f);
                           return new Vector4(f, f, f, 1);
                       },
                       size: (_) => new Vector4(1, 1, 1, 1), // Uniform size for all spheres.
                       coords: (v) => v); // Use the provided coordinates directly.

            // Update cube2: Sphere size depends on their positions
            UpdateCube(cube2, +10, -10,
                       colour: (pos) => {
                           // Color based on the sum of z and y positions or x position.
                           if (pos.z + pos.y > 0.23) {
                               return new Vector4(0, 1, 1, 1); // Cyan color
                           } else if (pos.x > -0.33) {
                               return new Vector4(0, 0, 1, 1); // Blue color
                           } else {
                               return new Vector4(1, 0, 1, 1); // Magenta color
                           }
                       },
                       size: (pos) => {
                           // Size based on the sum of z and y positions.
                           if (pos.z + pos.y > 0.43) {
                               float f = UnityEngine.Random.Range(-1f, 1f);
                               return new Vector4(f, UnityEngine.Random.Range(-1f, 1f), f, 1); // Random size for larger spheres
                           } else {
                               return new Vector4(1, 0, 1, 0); // Default size otherwise
                           }
                       },
                       coords: (v) => v); // Use the provided coordinates directly.

            // Update cube3: Position (coords) varies over time
            UpdateCube(cube3, -10, +10,
                       colour: (pos) => {
                           // Color based on the sum of z and y positions or x position.
                           if (pos.z + pos.y > 0.33) {
                               return new Vector4(0, 1, 0, 1); 
                           } else if (pos.x > -0.33) {
                               return new Vector4(1, -1, -1, 1); 
                           } else {
                               return new Vector4(0, 0, 0, 1); 
                           }
                       },
                       size: (pos) => {
                           // Size based on magnitude of the position.
                           if (pos.magnitude < 1) {
                               float f = UnityEngine.Random.Range(-1f, 1f);
                               return new Vector4(f, UnityEngine.Random.Range(-1f, 1f), f, 1); // Random size for smaller spheres
                           } else {
                               return new Vector4(-1, -1, -1, 1); // Default size otherwise
                           }
                       },
                       coords: (v) => new Vector4(-v.x, -v.y, -v.z, v.w)); // Invert the coordinates over time.
        }
    }
}