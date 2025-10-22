using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    public GameObject outsideCornerPrefab;      // 1
    public GameObject outsideWallPrefab;        // 2
    public GameObject insideCornerPrefab;       // 3
    public GameObject insideWallPrefab;         // 4
    public GameObject pelletPrefab;             // 5
    public GameObject powerPelletPrefab;        // 6
    public GameObject tJunctionPrefab;          // 7
    public GameObject ghostExitWallPrefab;      // 8
    public float tileSize = 1f;
    public Vector2 levelOffset = Vector2.zero;

    public int[,] LevelMap { get; private set; }

    private readonly int[,] quadrant =
    {
        {1,2,2,2,2,2,2,2,2,2,2,2,2,7},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,4},
        {2,6,4,0,0,4,5,4,0,0,0,4,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,3},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,5},
        {2,5,3,4,4,3,5,3,3,5,3,4,4,4},
        {2,5,3,4,4,3,5,4,4,5,3,4,4,3},
        {2,5,5,5,5,5,5,4,4,5,5,5,5,4},
        {1,2,2,2,2,1,5,4,3,4,4,3,0,4},
        {0,0,0,0,0,2,5,4,3,4,4,3,0,3},
        {0,0,0,0,0,2,5,4,4,0,0,0,0,0},
        {0,0,0,0,0,2,5,4,4,0,3,4,4,8},
        {2,2,2,2,2,1,5,3,3,0,4,0,0,0},
        {0,0,0,0,0,0,5,0,0,0,4,0,0,0},
    };


    // Start is called before the first frame update
    void Start()
    {
        var current = GameObject.Find("Level");
        if (current) Destroy(current);

        LevelMap = BuildFullMap(quadrant);

        GameObject levelRoot = new GameObject("Level");
        Populate(LevelMap, levelRoot.transform);

        levelRoot.transform.position = levelOffset;

        FitCamera(LevelMap);
    }

    private int[,] BuildFullMap(int[,] q)
    {
        int h = q.GetLength(0); 
        int w = q.GetLength(1); 

        int fullH = h * 2 - 1;
        int fullW = w * 2;

        int[,] map = new int[fullH, fullW];

        for (int r = 0; r < h; r++)
        {
            for (int c = 0; c < w; c++)
            {
                int v = q[r, c];

                // Top Left
                map[r, c] = v;

                // First Mirror (Top Right)
                map[r, fullW - 1 - c] = v;

                // Bottom side
                if (r < h - 1)
                {
                    int vr = fullH - 1 - r;

                    map[vr, c] = v; // Bottom-left
                    map[vr, fullW - 1 - c] = v; // Bottom-right
                }
            }
        }

        return map;
    }

    // Instantiate
    private void Populate(int[,] map, Transform parent)
    {
        int rows = map.GetLength(0);
        int cols = map.GetLength(1);

        for (int r = 0; r < rows; ++r)
        {
            for (int c = 0; c < cols; ++c)
            {
                int id = map[r, c];
                if (id == 0) continue; 

                GameObject prefab = PrefabFromId(id);
                if (!prefab) continue;

                Vector3 pos = new Vector3(c, -r, 0);
                Quaternion rot = RotationForTile(map, r, c, id);

                Instantiate(prefab, pos, rot, parent);
            }
        }
    }

    private GameObject PrefabFromId(int id)
    {
        switch (id)
        {
            case 1: return outsideCornerPrefab;
            case 2: return outsideWallPrefab;
            case 3: return insideCornerPrefab;
            case 4: return insideWallPrefab;
            case 5: return pelletPrefab;
            case 6: return powerPelletPrefab;
            case 7: return tJunctionPrefab;
            case 8: return ghostExitWallPrefab;
            default: return null;
        }
    }

    private bool IsWall(int v)
    {
        return v == 1 || v == 2 || v == 3 || v == 4 || v == 7 || v == 8;
    }

    private int CountRun(int[,] map, int r, int c, int dr, int dc)
    {
        int rows = map.GetLength(0);
        int cols = map.GetLength(1);
        int len = 0;

        r += dr;
        c += dc;
        while (r >= 0 && r < rows && c >= 0 && c < cols && IsWall(map[r, c]))
        {
            len++;
            r += dr;
            c += dc;
        }
        return len;
    }

    private Quaternion RotationForTile(int[,] map, int r, int c, int id)
    {
        // Pellet Tile
        if (id == 5 || id == 6) return Quaternion.identity;

        int rows = map.GetLength(0);
        int cols = map.GetLength(1);

        bool up = r > 0 && IsWall(map[r - 1, c]);
        bool right = c < cols - 1 && IsWall(map[r, c + 1]);
        bool down = r < rows - 1 && IsWall(map[r + 1, c]);
        bool left = c > 0 && IsWall(map[r, c - 1]);

        // Wall Tiles
        if (id == 2 || id == 4 || id == 8)
        {
            // Wall Corridor length, for Double walls
            int horizRun = (left ? 1 + CountRun(map, r, c, 0, -1) : 0) +
                           (right ? 1 + CountRun(map, r, c, 0, 1) : 0);

            int vertRun = (up ? 1 + CountRun(map, r, c, -1, 0) : 0) +
                           (down ? 1 + CountRun(map, r, c, 1, 0) : 0);

            if (vertRun > horizRun)
                return Quaternion.Euler(0, 0, 90); // Vertical

            
            return Quaternion.identity; // Horizontal
        }


        // Corner Tiles
        if (id == 1)
        {
            if (right && down) return Quaternion.identity; // Top left
            if (down && left) return Quaternion.Euler(0, 0, 270); // Top right
            if (left && up) return Quaternion.Euler(0, 0, 180); // Bottom right
            return Quaternion.Euler(0, 0, 90); // Bottom-left
        }

        if (id == 3)
        {
            // score[0] → RIGHT+DOWN, score[1] → DOWN+LEFT
            int[] score = new int[4];

            // RIGHT + DOWN
            if (right) score[0] += 1 + CountRun(map, r, c, 0, 1);
            if (down) score[0] += 1 + CountRun(map, r, c, 1, 0);

            // DOWN + LEFT
            if (down) score[1] += 1 + CountRun(map, r, c, 1, 0);
            if (left) score[1] += 1 + CountRun(map, r, c, 0, -1);

            // LEFT + UP
            if (left) score[2] += 1 + CountRun(map, r, c, 0, -1);
            if (up) score[2] += 1 + CountRun(map, r, c, -1, 0);

            // UP + RIGHT
            if (up) score[3] += 1 + CountRun(map, r, c, -1, 0);
            if (right) score[3] += 1 + CountRun(map, r, c, 0, 1);


            int best = 0;
            for (int i = 1; i < 4; i++)
                if (score[i] > score[best]) best = i;

            switch (best)
            {
                case 0: return Quaternion.identity;          // RIGHT+DOWN
                case 1: return Quaternion.Euler(0, 0, 270);  // DOWN+LEFT
                case 2: return Quaternion.Euler(0, 0, 180);  // LEFT+UP
            }
            return Quaternion.Euler(0, 0, 90);  // UP+RIGHT
        }

        // T Junction Tile
        if (id == 7)
        {
            if (!up) return Quaternion.identity; // T up
            if (!right) return Quaternion.Euler(0, 0, 90); // T right
            if (!down) return Quaternion.Euler(0, 0, 180); // T down
            return Quaternion.Euler(0, 0, 270); // T left
        }


        return Quaternion.identity;
    }

    private void FitCamera(int[,] map)
    {
        Camera cam = Camera.main;
        if (!cam) return;

        cam.orthographic = true;

        int rows = map.GetLength(0);
        int cols = map.GetLength(1);

        cam.transform.position = new Vector3((cols - 1) * 0.5f * tileSize,
                                             -(rows - 1) * 0.5f * tileSize,
                                             -10f);

        cam.orthographicSize = rows * 0.5f * tileSize + 1f;

        float targetHalfWidth = cols * 0.5f * tileSize;
        float currentHalfWidth = cam.orthographicSize * cam.aspect;
        if (targetHalfWidth > currentHalfWidth)
            cam.orthographicSize = targetHalfWidth / cam.aspect + 1f;
    }
}
