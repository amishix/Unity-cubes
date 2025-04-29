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
    private const int RESOLUTION = 11;  // Number of spheres per side.
    private const int EXTENT = 4;       // Cube size: half of the size length.

    public float stopAfterSecs = 0;     // Time after which the update stops.
    public float strobeTime = 10f;      // Time for the strobe effect.
    public GameObject spherePrefab;     // Prefab for the spheres.

    readonly System.Func<float, float, float, float, float, float> map =
        (v, from1, to1, from2, to2) =>
            Mathf.Lerp(from2, to2, Mathf.InverseLerp(from1, to1, v));

    // Arrays to hold the sphere cubes.
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
        float t = 0; // Time variable for strobe effect.
        for (int x = 0; x < RESOLUTION; x++) {
            for (int y = 0; y < RESOLUTION; y++) {
                for (int z = 0; z < RESOLUTION; z++) {
                    float normalX = map(x, 0, RESOLUTION - 1, -1, 1);
                    float normalY = map(y, 0, RESOLUTION - 1, -1, 1);
                    float normalZ = map(z, 0, RESOLUTION - 1, -1, 1);
                    Vector4 normals = new Vector4(normalX, normalY, normalZ, t);
                    Vector4 vCoords = coords(normals);
                    float xPos = map(vCoords.x, -1, 1, -EXTENT, EXTENT);
                    float yPos = map(vCoords.y, -1, 1, -EXTENT, EXTENT);
                    float zPos = map(vCoords.z, -1, 1, -EXTENT, EXTENT);

                    // Specific update for cube3
                    if (cube == cube3) {
                        Vector4 pos = new Vector4(xPos, yPos, zPos, t);
                        pos.x += (pos.y + pos.z) * 0.5f;
                        xPos = pos.x; // Update xPos with the altered value
                    }

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
            // Update cube1 with xCentre = +10, yCentre = -10
            UpdateCube(cube1, +10, -10,
                       // Cube1 color changes based on x position: black for x < 0, white otherwise
                       colour: (pos) =>
                       {
                           if (pos.x < 0)
                           {
                               return new Vector4(-1, -1, -1, -1); // Black color
                           }
                           else
                           {
                               return new Vector4(1, 1, 1, 1); // White color
                           }
                       },
                       size: (_) => new Vector4(1, 1, 1, 1), // Uniform size
                       coords: (v) => v); // Directly use the coordinates

            // Update cube2 with xCentre = +10, yCentre = +10
            UpdateCube(cube2, +10, +10,
                       // Cube2 color changes to black if on the edge, white otherwise
                       colour: (pos) =>
                       {
                           if (Math.Abs(pos.x) == 1 && Math.Abs(pos.y) == 1 && Math.Abs(pos.z) == 1)
                           {
                               return new Vector4(-1, -1, -1, 1); // Black color on edges
                           }
                           else
                           {
                               return new Vector4(1, 1, 1, 1); // White color otherwise
                           }
                       },
                       // Cube2 size changes based on distance from origin: larger size for distant spheres
                       size: (pos) =>
                       {
                           if (pos.magnitude > 1.6)
                           {
                               float f = 5;
                               return new Vector4(f, 10, f, 1); // Larger size for distant spheres
                           }
                           else
                           {
                               return new Vector4(-1, -1, -1, 1); // Default size otherwise
                           }
                       },
                       coords: (v) => v); // Directly use the coordinates

            // Update cube3 with xCentre = -10, yCentre = -10
            UpdateCube(cube3, -10, -10,
                       // Cube3 adjusts x position based on y and z, color remains white
                       colour: (pos) =>
                       {
                           pos.x += (pos.y + pos.z) * 0.5f; // Adjust x position
                           return new Vector4(1, 1, 1, 1); // White color
                       },
                       size: (_) => new Vector4(1, 1, 1, 1), // Uniform size
                       coords: (v) => v); // Directly use the coordinates

            // Update cube4 with xCentre = -10, yCentre = +10
            UpdateCube(cube4, -10, +10,
                       // Cube4 color changes randomly: 10% chance of red, otherwise grey
                       colour: (pos) =>
                       {
                           if (UnityEngine.Random.value < 0.1)
                           {
                               return new Vector4(1, 0, 0, 1); // Red color for 10%
                           }
                           else
                           {
                               return new Vector4(0.5f, 0.5f, 0.5f, 1); // Grey color otherwise
                           }
                       },
                       size: (_) => new Vector4(1, 0.2f, 1, 1), // Non-uniform size
                       coords: (v) => v); // Directly use the coordinates
        }
    }
}