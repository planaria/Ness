using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace SuzuFactory.Ness
{
    [DefaultExecutionOrder(-10)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Stage : UdonSharpBehaviour
    {
        public UdonBehaviour eventReceiver;

        public GameObject emptyPrefab;
        public GameObject pathPrefab;
        public GameObject whitePathPrefab;
        public GameObject disjointPathPrefab;
        public GameObject cellColliderPrefab;
        public GameObject gridColliderPrefab;
        public GameObject startColliderPrefab;
        public GameObject squarePrefab;
        public GameObject sunStarPrefab;
        public GameObject polyominoPrefab;
        public GameObject bluePolyominoPrefab;
        public GameObject yPrefab;
        public GameObject triangle1Prefab;
        public GameObject triangle2Prefab;
        public GameObject triangle3Prefab;
        public GameObject startPrefab;
        public GameObject endPrefab;
        public GameObject hexagonPrefab;
        public Material[] colorMaterials;
        public Material errorMaterial;
        public AudioClip succeededAudioClip;
        public AudioClip failedAudioClip;

        private const int MIN_NUM_ROWS = 0;
        private const int MIN_NUM_COLS = 0;
        private const int MAX_NUM_ROWS = 10;
        private const int MAX_NUM_COLS = 10;

        private const int MAX_NUM_POLYOMINO_ROWS = 5;
        private const int MAX_NUM_POLYOMINO_COLS = 5;

        private int numRows = MIN_NUM_ROWS;
        private int numCols = MIN_NUM_COLS;

        private int version = 0;
        private int lastVersion = -1;

        private const float BLOCK_SIZE = 0.03f;
        private const float POLYNOMIO_SIZE = 0.023f;
        private const float END_SIZE = 0.005f;
        private const float DEFAULT_PATH_RADIUS = 0.003f;
        private const float DEFAULT_START_RADIUS = 0.007f;
        private const float DISJOINT_WIDTH = 0.006f;

        private const int CELL_BITS_TYPE = 4;
        private const int CELL_BITS_COLOR = 4;
        private const int CELL_BITS_PATTERN = MAX_NUM_POLYOMINO_ROWS * MAX_NUM_POLYOMINO_COLS;

        private const int CELL_SHIFT_TYPE = 0;
        private const int CELL_SHIFT_COLOR = CELL_SHIFT_TYPE + CELL_BITS_TYPE;
        private const int CELL_SHIFT_PATTERN = CELL_SHIFT_COLOR + CELL_BITS_COLOR;

        private const ulong CELL_MASK_TYPE = (1ul << CELL_BITS_TYPE) - 1;
        private const ulong CELL_MASK_COLOR = (1ul << CELL_BITS_COLOR) - 1;
        private const ulong CELL_MASK_PATTERN = (1ul << CELL_BITS_PATTERN) - 1;

        private const uint CELL_TYPE_EMPTY = 0;
        private const uint CELL_TYPE_SQUARE = 1;
        private const uint CELL_TYPE_SUN_STAR = 2;
        private const uint CELL_TYPE_POLYOMINO = 3;
        private const uint CELL_TYPE_BLUE_POLYOMINO = 4;
        private const uint CELL_TYPE_FREE_POLYOMINO = 5;
        private const uint CELL_TYPE_FREE_BLUE_POLYOMINO = 6;
        private const uint CELL_TYPE_Y = 7;
        private const uint CELL_TYPE_TRIANGLE = 8;
        private ulong[] cells;

        private const ulong GRID_EMPTY = 0;
        private const ulong GRID_START = 1;
        private const ulong GRID_END = 2;
        private const ulong GRID_DISJOINT = 3;
        private const ulong GRID_HEXAGON = 4;
        private ulong[] grids;

        private GameObject[] cellGameObjects;
        private GameObject[] gridGameObjects;

        private const int TOOL_NONE = 0;
        private const int TOOL_SQUARE = 1;
        private const int TOOL_SUN_STAR = 2;
        private const int TOOL_POLYOMINO = 3;
        private const int TOOL_BLUE_POLYOMINO = 4;
        private const int TOOL_FREE_POLYOMINO = 5;
        private const int TOOL_FREE_BLUE_POLYOMINO = 6;
        private const int TOOL_Y = 7;
        private const int TOOL_TRIANGLE_1 = 8;
        private const int TOOL_TRIANGLE_2 = 9;
        private const int TOOL_TRIANGLE_3 = 10;
        private const int TOOL_START = 11;
        private const int TOOL_END = 12;
        private const int TOOL_HEXAGON = 13;
        private const int TOOL_DISJONT = 14;
        private const int TOOL_ERASER = 15;
        private const int TOOL_TEST = 16;
        private int tool = TOOL_TEST;

        private uint color = 0;

        private uint pattern = 0x7;

        private const int SYMMETRY_NONE = 0;
        private const int SYMMETRY_HORIZONTAL = 1;
        private const int SYMMETRY_VERTICAL = 2;
        private const int SYMMETRY_ROTATIONAL = 3;
        private int symmetry = SYMMETRY_NONE;

        private const int COLLIDER_TYPE_CELL = 0;
        private const int COLLIDER_TYPE_GRID = 1;

        private bool solved = false;
        private bool solvedByMyself = false;
        private bool editor = true;

        private Transform scaleTransform;
        private Transform mapTransform;
        private Transform pathTransform;
        private Transform currentStartTransform;
        private Transform currentPathTransform;
        private Transform currentStartSymmetryTransform;
        private Transform currentPathSymmetryTransform;
        private SkinnedMeshRenderer currentStartMeshRenderer;
        private SkinnedMeshRenderer currentPathMeshRenderer;
        private SkinnedMeshRenderer currentStartSymmetryMeshRenderer;
        private SkinnedMeshRenderer currentPathSymmetryMeshRenderer;
        private Transform starTransform;
        private Transform whiteStarTransform;
        private AudioSource audioSource;

        private GameObject[] failedObjects;
        private int numFailedObjects;

        private float radiusWeight = 0.0f;

        void Start()
        {
            scaleTransform = transform.Find("Scale");
            mapTransform = scaleTransform.Find("Map");
            pathTransform = scaleTransform.Find("Path");
            currentStartTransform = scaleTransform.Find("CurrentStart");
            currentPathTransform = scaleTransform.Find("CurrentPath");
            currentStartSymmetryTransform = scaleTransform.Find("CurrentStartSymmetry");
            currentPathSymmetryTransform = scaleTransform.Find("CurrentPathSymmetry");
            currentStartMeshRenderer = (SkinnedMeshRenderer)currentStartTransform.GetChild(0).GetComponent(typeof(SkinnedMeshRenderer));
            currentPathMeshRenderer = (SkinnedMeshRenderer)currentPathTransform.GetChild(0).GetComponent(typeof(SkinnedMeshRenderer));
            currentStartSymmetryMeshRenderer = (SkinnedMeshRenderer)currentStartSymmetryTransform.GetChild(0).GetComponent(typeof(SkinnedMeshRenderer));
            currentPathSymmetryMeshRenderer = (SkinnedMeshRenderer)currentPathSymmetryTransform.GetChild(0).GetComponent(typeof(SkinnedMeshRenderer));
            starTransform = transform.Find("Star");
            whiteStarTransform = transform.Find("WhiteStar");
            audioSource = (AudioSource)transform.Find("Audio Source").GetComponent(typeof(AudioSource));

            ClearAll();
        }

        void Update()
        {
            if (lastVersion != version)
            {
                ResetTest();
                UpdateStage();
                lastVersion = version;
            }

            if (testState == TEST_STATE_END_ERROR)
            {
                var maxProgress = 1.0f;
                testProgress += Time.deltaTime;

                var r = (1.0f - Mathf.Cos(2.0f * Mathf.PI * testProgress * 5.0f)) * 0.5f;
                var a = (1.0f - Mathf.Cos(2.0f * Mathf.PI * testProgress)) * 0.5f;
                errorMaterial.SetVector("_Color", new Vector4(0.0f, 0.0f, 0.0f, a));
                errorMaterial.SetVector("_EmissionColor", new Vector3(r, 0.0f, 0.0f));

                if (testProgress > maxProgress)
                {
                    ResetTest();
                }
            }
        }

        private void UpdateStage()
        {
            for (int i = 0; i < mapTransform.childCount; ++i)
            {
                Destroy(mapTransform.GetChild(i).gameObject);
            }

            var maxSize = Mathf.Max(MAX_NUM_ROWS, MAX_NUM_COLS);
            var size = Mathf.Max(numRows, numCols);
            var wholeScale = (maxSize + 1.0f) / (size + 1.0f);
            scaleTransform.localScale = new Vector3(wholeScale, wholeScale, wholeScale);

            cellGameObjects = new GameObject[cells.Length];
            gridGameObjects = new GameObject[grids.Length];

            radiusWeight = 100.0f * (1.0f - Mathf.Clamp(2.0f / wholeScale, 0.15f, 1.0f));

            for (int i = 0; i <= numRows; ++i)
            {
                for (int j = 0; j < numCols; ++j)
                {
                    var joint = grids[GetGridIndex(i * 2, j * 2 + 1)] != GRID_DISJOINT;
                    var newPath = VRCInstantiate(joint ? pathPrefab : disjointPathPrefab);
                    var t = newPath.transform;
                    var meshRenderer = (SkinnedMeshRenderer)t.GetChild(0).GetComponent(typeof(SkinnedMeshRenderer));
                    meshRenderer.SetBlendShapeWeight(0, radiusWeight);
                    t.SetParent(mapTransform, false);
                    t.localPosition = GetHalfGridPosition(i, j);
                    t.localRotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);
                }
            }

            for (int i = 0; i < numRows; ++i)
            {
                for (int j = 0; j <= numCols; ++j)
                {
                    var joint = grids[GetGridIndex(i * 2 + 1, j * 2)] != GRID_DISJOINT;
                    var newPath = VRCInstantiate(joint ? pathPrefab : disjointPathPrefab);
                    var t = newPath.transform;
                    var meshRenderer = (SkinnedMeshRenderer)t.GetChild(0).GetComponent(typeof(SkinnedMeshRenderer));
                    meshRenderer.SetBlendShapeWeight(0, radiusWeight);
                    t.SetParent(mapTransform, false);
                    t.localPosition = GetHalfGridPosition(i, j);
                    t.localRotation = Quaternion.Euler(0.0f, 90.0f, 0.0f);
                }
            }

            if (IsCellToolActive())
            {
                for (int i = 0; i < numRows; ++i)
                {
                    for (int j = 0; j < numCols; ++j)
                    {
                        var p = GetCellPosition(i, j);

                        var newCellCollider = VRCInstantiate(cellColliderPrefab);
                        var indexedCollider = newCellCollider.GetComponent<IndexedCollider>();
                        indexedCollider.type = COLLIDER_TYPE_CELL;
                        indexedCollider.index = GetCellIndex(i, j);
                        var t = newCellCollider.transform;
                        t.SetParent(mapTransform, false);
                        t.localPosition = p;
                    }
                }
            }

            if (IsGridToolActive())
            {
                for (int i = 0; i <= numRows * 2; ++i)
                {
                    for (int j = 0; j <= numCols * 2; ++j)
                    {
                        if (i % 2 != 0 && j % 2 != 0)
                        {
                            continue;
                        }

                        switch (tool)
                        {
                            case TOOL_START:
                                if (i % 2 != 0 || j % 2 != 0)
                                {
                                    continue;
                                }
                                break;
                            case TOOL_END:
                                if (i % 2 != 0 || j % 2 != 0)
                                {
                                    continue;
                                }

                                if (!((i == 0 || i == numRows * 2) || (j == 0 || j == numCols * 2)))
                                {
                                    continue;
                                }
                                break;
                            case TOOL_DISJONT:
                                if (i % 2 == j % 2)
                                {
                                    continue;
                                }
                                break;
                        }

                        var p = GetGridPosition(i, j);

                        var newCellCollider = VRCInstantiate(gridColliderPrefab);
                        var indexedCollider = newCellCollider.GetComponent<IndexedCollider>();
                        indexedCollider.type = COLLIDER_TYPE_GRID;
                        indexedCollider.index = GetGridIndex(i, j);
                        var t = newCellCollider.transform;
                        t.SetParent(mapTransform, false);
                        t.localPosition = p;
                    }
                }
            }

            int maxNumPatternRows = 0;
            int maxNumPatternCols = 0;

            for (int i = 0; i < numRows; ++i)
            {
                for (int j = 0; j < numCols; ++j)
                {
                    var c = cells[GetCellIndex(i, j)];
                    var type = GetCellType(c);
                    var pattern = GetCellPattern(c);

                    switch (type)
                    {
                        case CELL_TYPE_POLYOMINO:
                        case CELL_TYPE_BLUE_POLYOMINO:
                        case CELL_TYPE_FREE_POLYOMINO:
                        case CELL_TYPE_FREE_BLUE_POLYOMINO:
                            {
                                maxNumPatternRows = Mathf.Max(maxNumPatternRows, GetPatternRows(pattern));
                                maxNumPatternCols = Mathf.Max(maxNumPatternCols, GetPatternCols(pattern));
                            }
                            break;
                    }
                }
            }

            for (int i = 0; i < numRows; ++i)
            {
                for (int j = 0; j < numCols; ++j)
                {
                    var p = GetCellPosition(i, j);
                    var c = cells[GetCellIndex(i, j)];
                    var type = GetCellType(c);
                    var color = GetCellColor(c);
                    var pattern = GetCellPattern(c);
                    GameObject newGameObject = null;

                    switch (type)
                    {
                        case CELL_TYPE_SQUARE:
                        case CELL_TYPE_SUN_STAR:
                        case CELL_TYPE_Y:
                        case CELL_TYPE_TRIANGLE:
                            {
                                GameObject prefab = null;

                                switch (type)
                                {
                                    case CELL_TYPE_SQUARE:
                                        prefab = squarePrefab;
                                        break;
                                    case CELL_TYPE_SUN_STAR:
                                        prefab = sunStarPrefab;
                                        break;
                                    case CELL_TYPE_Y:
                                        prefab = yPrefab;
                                        break;
                                    case CELL_TYPE_TRIANGLE:
                                        switch (pattern)
                                        {
                                            case 1:
                                                prefab = triangle1Prefab;
                                                break;
                                            case 2:
                                                prefab = triangle2Prefab;
                                                break;
                                            case 3:
                                                prefab = triangle3Prefab;
                                                break;
                                        }
                                        break;
                                }

                                newGameObject = VRCInstantiate(prefab);
                                var t = newGameObject.transform;
                                t.SetParent(mapTransform, false);
                                t.localPosition = p;
                                t.localRotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);
                                t.localScale = new Vector3(0.6f, 0.6f, 0.6f);
                            }
                            break;
                        case CELL_TYPE_POLYOMINO:
                        case CELL_TYPE_BLUE_POLYOMINO:
                        case CELL_TYPE_FREE_POLYOMINO:
                        case CELL_TYPE_FREE_BLUE_POLYOMINO:
                            {
                                newGameObject = VRCInstantiate(emptyPrefab);
                                var newGameObjectTransform = newGameObject.transform;

                                GameObject prefab = null;

                                switch (type)
                                {
                                    case CELL_TYPE_POLYOMINO:
                                    case CELL_TYPE_FREE_POLYOMINO:
                                        prefab = polyominoPrefab;
                                        break;
                                    case CELL_TYPE_BLUE_POLYOMINO:
                                    case CELL_TYPE_FREE_BLUE_POLYOMINO:
                                        prefab = bluePolyominoPrefab;
                                        break;
                                }

                                var numPatternRows = GetPatternRows(pattern);
                                var numPatternCols = GetPatternCols(pattern);

                                for (int patternRow = 0; patternRow < numPatternRows; ++patternRow)
                                {
                                    for (int patternCol = 0; patternCol < numPatternCols; ++patternCol)
                                    {
                                        if (GetPatternBit(pattern, patternRow, patternCol))
                                        {
                                            var newPoly = VRCInstantiate(prefab);
                                            var t = newPoly.transform;
                                            t.SetParent(newGameObjectTransform, false);
                                            t.localPosition = new Vector3((patternCol - (numPatternCols - 1) * 0.5f) * POLYNOMIO_SIZE, 0.0f, (patternRow - (numPatternRows - 1) * 0.5f) * POLYNOMIO_SIZE);
                                        }
                                    }
                                }

                                newGameObjectTransform.SetParent(mapTransform, false);
                                newGameObjectTransform.localPosition = p;

                                float scale = 0.8f;

                                switch (type)
                                {
                                    case CELL_TYPE_FREE_POLYOMINO:
                                    case CELL_TYPE_FREE_BLUE_POLYOMINO:
                                        newGameObjectTransform.localRotation = Quaternion.Euler(0.0f, -15.0f, 0.0f);
                                        scale = 0.6f;
                                        break;
                                }

                                scale /= Mathf.Max(3, Mathf.Max(maxNumPatternRows, maxNumPatternCols));
                                newGameObjectTransform.localScale = new Vector3(scale, scale, scale);
                            }
                            break;
                    }

                    if (newGameObject != null)
                    {
                        var meshRenderers = (MeshRenderer[])newGameObject.GetComponentsInChildren(typeof(MeshRenderer));

                        for (int k = 0; k < meshRenderers.Length; ++k)
                        {
                            meshRenderers[k].sharedMaterial = colorMaterials[color];
                        }

                        cellGameObjects[GetCellIndex(i, j)] = newGameObject;
                    }
                }
            }

            for (int i = 0; i <= numRows * 2; ++i)
            {
                for (int j = 0; j <= numCols * 2; ++j)
                {
                    var p = GetGridPosition(i, j);
                    var g = grids[GetGridIndex(i, j)];

                    GameObject prefab = null;

                    switch (g)
                    {
                        case GRID_START:
                            prefab = startPrefab;
                            break;
                        case GRID_END:
                            prefab = endPrefab;
                            break;
                        case GRID_HEXAGON:
                            prefab = hexagonPrefab;
                            break;
                        default:
                            continue;
                    }

                    var newGameObject = VRCInstantiate(prefab);
                    var t = newGameObject.transform;
                    var meshRenderer = (SkinnedMeshRenderer)t.GetChild(0).GetComponent(typeof(SkinnedMeshRenderer));
                    meshRenderer.SetBlendShapeWeight(0, radiusWeight);

                    t.SetParent(mapTransform, false);
                    t.localPosition = p;

                    switch (g)
                    {
                        case GRID_END:
                            if (j == 0)
                            {
                                t.localRotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                            }
                            else if (j == numCols * 2)
                            {
                                t.localRotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);
                            }
                            else if (i == 0)
                            {
                                t.localRotation = Quaternion.Euler(0.0f, -90.0f, 0.0f);
                            }
                            else
                            {
                                t.localRotation = Quaternion.Euler(0.0f, 90.0f, 0.0f);
                            }
                            break;
                        default:
                            t.localRotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);
                            break;
                    }

                    gridGameObjects[GetGridIndex(i, j)] = newGameObject;
                }
            }

            if (tool == TOOL_TEST)
            {
                for (int i = 0; i <= numRows * 2; ++i)
                {
                    for (int j = 0; j <= numCols * 2; ++j)
                    {
                        var p = GetGridPosition(i, j);
                        var g = grids[GetGridIndex(i, j)];

                        if (g == GRID_START)
                        {
                            var newGameObject = VRCInstantiate(startColliderPrefab);
                            var startCollider = newGameObject.GetComponent<StartCollider>();
                            startCollider.index = GetWaypointIndex(i * 2, j * 2);
                            var t = newGameObject.transform;
                            t.SetParent(mapTransform, false);
                            t.localPosition = p;
                        }
                    }
                }
            }
        }

        private void UpdateStars()
        {
            if (solvedByMyself)
            {
                starTransform.gameObject.SetActive(false);
                whiteStarTransform.gameObject.SetActive(true);
            }
            else if (solved)
            {
                starTransform.gameObject.SetActive(true);
                whiteStarTransform.gameObject.SetActive(false);
            }
            else
            {
                starTransform.gameObject.SetActive(false);
                whiteStarTransform.gameObject.SetActive(false);
            }
        }

        private Vector3 GetWaypointPosition(int i, int j)
        {
            return new Vector3((j * 0.25f - numCols * 0.5f) * BLOCK_SIZE, 0.0f, (i * 0.25f - numRows * 0.5f) * BLOCK_SIZE);
        }

        private Vector3 GetSymmetryWaypointPosition(int i, int j)
        {
            switch (symmetry)
            {
                case SYMMETRY_HORIZONTAL:
                    return GetWaypointPosition(i, numCols * 4 - j);
                case SYMMETRY_VERTICAL:
                    return GetWaypointPosition(numRows * 4 - i, j);
                case SYMMETRY_ROTATIONAL:
                    return GetWaypointPosition(numRows * 4 - i, numCols * 4 - j);
            }

            return GetWaypointPosition(i, j);
        }

        private Vector3 GetCellPosition(int i, int j)
        {
            return new Vector3(((j + 0.5f) - numCols * 0.5f) * BLOCK_SIZE, 0.0f, ((i + 0.5f) - numRows * 0.5f) * BLOCK_SIZE);
        }

        private Vector3 GetGridPosition(int i, int j)
        {
            return new Vector3((j - numCols) * 0.5f * BLOCK_SIZE, 0.0f, (i - numRows) * 0.5f * BLOCK_SIZE);
        }

        private Vector3 GetHalfGridPosition(int i, int j)
        {
            return new Vector3((j - numCols * 0.5f) * BLOCK_SIZE, 0.0f, (i - numRows * 0.5f) * BLOCK_SIZE);
        }

        private bool IsCellToolActive()
        {
            switch (tool)
            {
                case TOOL_SQUARE:
                case TOOL_SUN_STAR:
                case TOOL_Y:
                case TOOL_TRIANGLE_1:
                case TOOL_TRIANGLE_2:
                case TOOL_TRIANGLE_3:
                case TOOL_ERASER:
                    return true;
                case TOOL_POLYOMINO:
                case TOOL_BLUE_POLYOMINO:
                case TOOL_FREE_POLYOMINO:
                case TOOL_FREE_BLUE_POLYOMINO:
                    {
                        var normalizedPattern = NormalizePattern(pattern);

                        if (GetPatternRows(normalizedPattern) == 0 || GetPatternCols(normalizedPattern) == 0)
                        {
                            return false;
                        }

                        return true;
                    }
            }

            return false;
        }

        private bool IsGridToolActive()
        {
            switch (tool)
            {
                case TOOL_START:
                case TOOL_END:
                case TOOL_HEXAGON:
                case TOOL_DISJONT:
                case TOOL_ERASER:
                    return true;
            }

            return false;
        }

        private ulong MakeCell(uint type, uint color, uint pattern)
        {
            return ((type & CELL_MASK_TYPE) << CELL_SHIFT_TYPE) | ((color & CELL_MASK_COLOR) << CELL_SHIFT_COLOR) | ((pattern & CELL_MASK_PATTERN) << CELL_SHIFT_PATTERN);
        }

        private uint GetCellType(ulong cell)
        {
            return (uint)((cell >> CELL_SHIFT_TYPE) & CELL_MASK_TYPE);
        }

        private uint GetCellColor(ulong cell)
        {
            return (uint)((cell >> CELL_SHIFT_COLOR) & CELL_MASK_COLOR);
        }

        private uint GetCellPattern(ulong cell)
        {
            return (uint)((cell >> CELL_SHIFT_PATTERN) & CELL_MASK_PATTERN);
        }

        public void Resize(int deltaNumRows, int deltaNumCols, int offsetRow, int offsetCol)
        {
            ResetTest();

            var newNumRows = numRows + deltaNumRows;
            var newNumCols = numCols + deltaNumCols;

            if (newNumRows < MIN_NUM_ROWS || newNumCols < MIN_NUM_COLS || newNumRows > MAX_NUM_ROWS || newNumCols > MAX_NUM_COLS)
            {
                return;
            }

            var newCells = new ulong[newNumRows * newNumCols];

            for (int i = 0; i < newNumRows; ++i)
            {
                for (int j = 0; j < newNumCols; ++j)
                {
                    var oldRow = i + offsetRow;
                    var oldCol = j + offsetCol;

                    if (oldRow < 0 || oldRow >= numRows || oldCol < 0 || oldCol >= numCols)
                    {
                        newCells[i * newNumCols + j] = MakeCell(CELL_TYPE_EMPTY, 0, 0);
                    }
                    else
                    {
                        newCells[i * newNumCols + j] = cells[oldRow * numCols + oldCol];
                    }
                }
            }

            var newGrids = new ulong[(newNumRows * 2 + 1) * (newNumCols * 2 + 1)];

            for (int i = 0; i < newNumRows * 2 + 1; ++i)
            {
                for (int j = 0; j < newNumCols * 2 + 1; ++j)
                {
                    var oldRow = i + offsetRow * 2;
                    var oldCol = j + offsetCol * 2;

                    if (oldRow < 0 || oldRow >= numRows * 2 + 1 || oldCol < 0 || oldCol >= numCols * 2 + 1)
                    {
                        newGrids[i * (newNumCols * 2 + 1) + j] = GRID_EMPTY;
                    }
                    else
                    {
                        newGrids[i * (newNumCols * 2 + 1) + j] = grids[oldRow * (numCols * 2 + 1) + oldCol];
                    }
                }
            }

            numRows = newNumRows;
            numCols = newNumCols;
            cells = newCells;
            grids = newGrids;
            solved = false;
            solvedByMyself = false;

            if (symmetry == SYMMETRY_HORIZONTAL && !CanSetSymmetryHorizontal ||
                symmetry == SYMMETRY_VERTICAL && !CanSetSymmetryVertical ||
                symmetry == SYMMETRY_ROTATIONAL && !CanSetSymmetryRotational)
            {
                symmetry = SYMMETRY_NONE;
            }

            FixSymmetry();
            ++version;

            UpdateStars();
        }

        public int Tool
        {
            get
            {
                return tool;
            }
            set
            {
                ResetTest();
                tool = value;
                ++version;
            }
        }

        public uint Color
        {
            get
            {
                return color;
            }
            set
            {
                ResetTest();
                color = value;
                ++version;
            }
        }

        public uint Pattern
        {
            get
            {
                return pattern;
            }
            set
            {
                ResetTest();
                pattern = value;
                ++version;
            }
        }

        public bool CanSetSymmetryHorizontal
        {
            get
            {
                return numCols != 0;
            }
        }

        public bool CanSetSymmetryVertical
        {
            get
            {
                return numRows != 0;
            }
        }

        public bool CanSetSymmetryRotational
        {
            get
            {
                return numRows != 0;
            }
        }

        public int Symmetry
        {
            get
            {
                return symmetry;
            }
            set
            {
                if (value == SYMMETRY_HORIZONTAL && !CanSetSymmetryHorizontal)
                {
                    return;
                }

                if (value == SYMMETRY_VERTICAL && !CanSetSymmetryVertical)
                {
                    return;
                }

                if (value == SYMMETRY_ROTATIONAL && !CanSetSymmetryRotational)
                {
                    return;
                }

                ResetTest();
                symmetry = value;
                FixSymmetry();

                ++version;
            }
        }

        private void FixSymmetry()
        {
            if (symmetry != SYMMETRY_NONE)
            {
                for (int i = 0; i <= numRows * 2; ++i)
                {
                    for (int j = 0; j <= numCols * 2; ++j)
                    {
                        var index1 = GetGridIndex(i, j);
                        var index2 = GetSymmetryGridIndex(index1);
                        var g1 = grids[index1];
                        var g2 = grids[index2];

                        if (g1 == GRID_START || g2 == GRID_START)
                        {
                            g1 = GRID_START;
                            g2 = GRID_START;
                        }
                        else if (g1 == GRID_END || g2 == GRID_END)
                        {
                            g1 = GRID_END;
                            g2 = GRID_END;
                        }

                        grids[index1] = g1;
                        grids[index2] = g2;
                    }
                }
            }
        }

        public int GetPatternIndex(int row, int col)
        {
            return row * MAX_NUM_POLYOMINO_COLS + col;
        }

        public bool GetPatternBit(uint pattern, int row, int col)
        {
            var index = GetPatternIndex(row, col);
            return ((pattern >> index) & 1) != 0;
        }

        private int GetPatternCells(uint pattern)
        {
            uint c = pattern - ((pattern >> 1) & 0x55555555u);
            c = ((c >> 2) & 0x33333333u) + (c & 0x33333333u);
            c = ((c >> 4) + c) & 0x0F0F0F0Fu;
            c = ((c >> 8) + c) & 0x00FF00FFu;
            c = ((c >> 16) + c) & 0x0000FFFFu;
            return (int)c;
        }

        public uint SetPatternBit(uint pattern, int row, int col, bool value)
        {
            var index = GetPatternIndex(row, col);

            if (value)
            {
                return pattern | (1u << index);
            }
            else
            {
                return pattern & ~(1u << index);
            }
        }

        public void ClearAll()
        {
            ResetTest();

            numRows = 4;
            numCols = 4;

            cells = new ulong[numRows * numCols];

            for (int i = 0; i < cells.Length; ++i)
            {
                cells[i] = MakeCell(CELL_TYPE_EMPTY, 0, 0);
            }

            grids = new ulong[(numRows * 2 + 1) * (numCols * 2 + 1)];

            for (int i = 0; i < grids.Length; ++i)
            {
                grids[i] = GRID_EMPTY;
            }

            grids[0] = GRID_START;
            grids[grids.Length - 1] = GRID_END;

            symmetry = SYMMETRY_NONE;

            solved = false;
            solvedByMyself = false;
            ++version;

            UpdateStars();
        }

        public int NumRows
        {
            get
            {
                return numRows;
            }
        }

        public int NumCols
        {
            get
            {
                return numCols;
            }
        }

        public ulong[] ClonedCells
        {
            get
            {
                return (ulong[])cells.Clone();
            }
        }

        public ulong[] ClonedGrids
        {
            get
            {
                return (ulong[])grids.Clone();
            }
        }

        public bool IsSolved
        {
            get
            {
                return solved;
            }
        }

        public void Set(int numRows, int numCols, ulong[] cells, ulong[] grids, int symmetry, bool solved, bool solvedByMyself)
        {
            ResetTest();

            this.numRows = numRows;
            this.numCols = numCols;
            this.cells = (ulong[])cells.Clone();
            this.grids = (ulong[])grids.Clone();
            this.symmetry = symmetry;
            this.solved = solved;
            this.solvedByMyself = solvedByMyself;
            Tool = TOOL_TEST;
            editor = false;
            ++version;

            UpdateStars();
        }

        private uint NormalizePattern(uint pattern)
        {
            int minRow = 0;

            for (; minRow < MAX_NUM_POLYOMINO_ROWS; ++minRow)
            {
                bool found = false;

                for (int col = 0; col < MAX_NUM_POLYOMINO_COLS; ++col)
                {
                    if (GetPatternBit(pattern, minRow, col))
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    break;
                }
            }

            int minCol = 0;

            for (; minCol < MAX_NUM_POLYOMINO_COLS; ++minCol)
            {
                bool found = false;

                for (int row = 0; row < MAX_NUM_POLYOMINO_ROWS; ++row)
                {
                    if (GetPatternBit(pattern, row, minCol))
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    break;
                }
            }

            uint newPattern = 0;

            for (int i = 0; i < MAX_NUM_POLYOMINO_ROWS - minRow; ++i)
            {
                for (int j = 0; j < MAX_NUM_POLYOMINO_COLS - minCol; ++j)
                {
                    newPattern = SetPatternBit(newPattern, i, j, GetPatternBit(pattern, i + minRow, j + minCol));
                }
            }

            return newPattern;
        }

        private int GetPatternRows(uint pattern)
        {
            int row = MAX_NUM_POLYOMINO_ROWS - 1;

            for (; row >= 0; --row)
            {
                bool found = false;

                for (int col = 0; col < MAX_NUM_POLYOMINO_COLS; ++col)
                {
                    if (GetPatternBit(pattern, row, col))
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    break;
                }
            }

            return row + 1;
        }

        private int GetPatternCols(uint pattern)
        {
            int col = MAX_NUM_POLYOMINO_COLS - 1;

            for (; col >= 0; --col)
            {
                bool found = false;

                for (int row = 0; row < MAX_NUM_POLYOMINO_ROWS; ++row)
                {
                    if (GetPatternBit(pattern, row, col))
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    break;
                }
            }

            return col + 1;
        }

        public void OnIndexedCollider(int type, int index)
        {
            ResetTest();

            switch (type)
            {
                case COLLIDER_TYPE_CELL:
                    {
                        ulong newCell = cells[index];

                        switch (tool)
                        {
                            case TOOL_SQUARE:
                                newCell = MakeCell(CELL_TYPE_SQUARE, color, 0);
                                break;
                            case TOOL_SUN_STAR:
                                newCell = MakeCell(CELL_TYPE_SUN_STAR, color, 0);
                                break;
                            case TOOL_POLYOMINO:
                            case TOOL_BLUE_POLYOMINO:
                            case TOOL_FREE_POLYOMINO:
                            case TOOL_FREE_BLUE_POLYOMINO:
                                {
                                    var normalizedPattern = NormalizePattern(pattern);

                                    if (GetPatternRows(normalizedPattern) == 0 || GetPatternCols(normalizedPattern) == 0)
                                    {
                                        return;
                                    }

                                    switch (tool)
                                    {
                                        case TOOL_POLYOMINO:
                                            newCell = MakeCell(CELL_TYPE_POLYOMINO, color, normalizedPattern);
                                            break;
                                        case TOOL_BLUE_POLYOMINO:
                                            newCell = MakeCell(CELL_TYPE_BLUE_POLYOMINO, color, normalizedPattern);
                                            break;
                                        case TOOL_FREE_POLYOMINO:
                                            newCell = MakeCell(CELL_TYPE_FREE_POLYOMINO, color, normalizedPattern);
                                            break;
                                        case TOOL_FREE_BLUE_POLYOMINO:
                                            newCell = MakeCell(CELL_TYPE_FREE_BLUE_POLYOMINO, color, normalizedPattern);
                                            break;
                                    }
                                }
                                break;
                            case TOOL_Y:
                                newCell = MakeCell(CELL_TYPE_Y, color, 0);
                                break;
                            case TOOL_TRIANGLE_1:
                                newCell = MakeCell(CELL_TYPE_TRIANGLE, color, 1);
                                break;
                            case TOOL_TRIANGLE_2:
                                newCell = MakeCell(CELL_TYPE_TRIANGLE, color, 2);
                                break;
                            case TOOL_TRIANGLE_3:
                                newCell = MakeCell(CELL_TYPE_TRIANGLE, color, 3);
                                break;
                            case TOOL_ERASER:
                                newCell = MakeCell(CELL_TYPE_EMPTY, 0, 0);
                                break;
                        }

                        if (newCell != cells[index])
                        {
                            cells[index] = newCell;
                            solved = false;
                            solvedByMyself = false;
                            UpdateStars();
                            ++version;
                        }
                    }
                    break;
                case COLLIDER_TYPE_GRID:
                    {
                        ulong oldGrid = grids[index];
                        ulong newGrid = oldGrid;

                        switch (tool)
                        {
                            case TOOL_START:
                                newGrid = GRID_START;
                                break;
                            case TOOL_END:
                                newGrid = GRID_END;
                                break;
                            case TOOL_HEXAGON:
                                newGrid = GRID_HEXAGON;
                                break;
                            case TOOL_DISJONT:
                                newGrid = GRID_DISJOINT;
                                break;
                            case TOOL_ERASER:
                                newGrid = GRID_EMPTY;
                                break;
                        }

                        if (newGrid != grids[index])
                        {
                            grids[index] = newGrid;

                            bool needToCopy = false;

                            if (symmetry != SYMMETRY_NONE)
                            {
                                switch (newGrid)
                                {
                                    case GRID_START:
                                    case GRID_END:
                                        needToCopy = true;
                                        break;
                                    case GRID_EMPTY:
                                        switch (oldGrid)
                                        {
                                            case GRID_START:
                                            case GRID_END:
                                                needToCopy = true;
                                                break;
                                        }
                                        break;
                                }
                            }

                            if (needToCopy)
                            {
                                grids[GetSymmetryGridIndex(index)] = newGrid;
                            }

                            solved = false;
                            solvedByMyself = false;
                            UpdateStars();
                            ++version;
                        }
                    }
                    break;
            }
        }

        private const int TEST_STATE_INIT = 0;
        private const int TEST_STATE_START = 1;
        private const int TEST_STATE_END_OK = 2;
        private const int TEST_STATE_END_ERROR = 4;

        private int testState = TEST_STATE_INIT;
        private float testProgress = 0.0f;
        private int[] testWaypoints = null;
        private GameObject[] testPaths = null;
        private bool[] testMap = null;
        private bool testEnd = false;

        public int TestState
        {
            get
            {
                return testState;
            }
        }

        public int[] TestWaypoints
        {
            get
            {
                return (int[])testWaypoints.Clone();
            }
        }

        public void StartDrag(int index)
        {
            if (tool != TOOL_TEST)
            {
                return;
            }

            if (testState == TEST_STATE_START)
            {
                return;
            }

            ResetTest();

            testState = TEST_STATE_START;
            testWaypoints = new int[] { index };
            testPaths = new GameObject[0];
            testMap = new bool[(numRows * 4 + 1) * (numCols * 4 + 1)];

            for (int i = 0; i < testMap.Length; ++i)
            {
                testMap[i] = false;
            }

            testMap[index] = true;
            testMap[GetSymmetryWaypointIndex(index)] = true;

            RaiseStateChangedEvent();
        }

        private void RaiseStateChangedEvent()
        {
            if (eventReceiver != null)
            {
                eventReceiver.SendCustomEvent("OnStateChanged");
            }
        }

        public void Drag(Vector3 position)
        {
            if (tool != TOOL_TEST)
            {
                return;
            }

            if (testState != TEST_STATE_START)
            {
                return;
            }

            Vector3 firstPoint;
            Vector3 firstPointSymmetry;

            {
                var firstWaypoint = testWaypoints[0];
                var row = GetWaypointRow(firstWaypoint);
                var col = GetWaypointCol(firstWaypoint);
                firstPoint = GetWaypointPosition(row, col);
                firstPointSymmetry = GetSymmetryWaypointPosition(row, col);
                currentStartTransform.localPosition = firstPoint;
                currentStartSymmetryTransform.localPosition = firstPointSymmetry;
                currentStartMeshRenderer.SetBlendShapeWeight(0, radiusWeight);
                currentStartSymmetryMeshRenderer.SetBlendShapeWeight(0, radiusWeight);
            }

            testEnd = false;

            Vector3 lastPoint;
            Vector3 lastPointSymmetry;
            float length = 0.0f;
            float angle = 0.0f;

            while (true)
            {
                var lastWaypoint = testWaypoints[testWaypoints.Length - 1];
                var row = GetWaypointRow(lastWaypoint);
                var col = GetWaypointCol(lastWaypoint);

                lastPoint = GetWaypointPosition(row, col);
                lastPointSymmetry = GetSymmetryWaypointPosition(row, col);
                var delta = scaleTransform.InverseTransformPoint(position) - lastPoint;
                var isEnd = IsEnd(row, col);
                bool isEndTop = false;
                bool isEndBottom = false;
                bool isEndLeft = false;
                bool isEndRight = false;

                if (isEnd)
                {
                    if (col == 0)
                    {
                        isEndLeft = true;
                    }
                    else if (col == numCols * 4)
                    {
                        isEndRight = true;
                    }
                    else if (row == 0)
                    {
                        isEndBottom = true;
                    }
                    else
                    {
                        isEndTop = true;
                    }
                }

                if (row % 4 != 0)
                {
                    delta.x = 0.0f;
                }

                if (col % 4 != 0)
                {
                    delta.z = 0.0f;
                }

                if (!isEndLeft && col == 0)
                {
                    delta.x = Mathf.Max(delta.x, 0.0f);
                }

                if (!isEndRight && col == numCols * 4)
                {
                    delta.x = Mathf.Min(delta.x, 0.0f);
                }

                if (!isEndBottom && row == 0)
                {
                    delta.z = Mathf.Max(delta.z, 0.0f);
                }

                if (!isEndTop && row == numRows * 4)
                {
                    delta.z = Mathf.Min(delta.z, 0.0f);
                }

                var nextRow = row;
                var nextCol = col;

                if (Mathf.Abs(delta.x) < Mathf.Abs(delta.z))
                {
                    if (isEndBottom && row == 0 && delta.z < -END_SIZE)
                    {
                        testEnd = true;
                        delta.z = -END_SIZE;
                    }

                    if (isEndTop && row == numRows * 4 && delta.z > END_SIZE)
                    {
                        testEnd = true;
                        delta.z = END_SIZE;
                    }

                    length = Mathf.Abs(delta.z);

                    if (delta.z < 0.0f)
                    {
                        --nextRow;
                        angle = 90.0f;
                    }
                    else if (delta.z > 0.0f)
                    {
                        ++nextRow;
                        angle = -90.0f;
                    }
                }
                else
                {
                    if (isEndLeft && col == 0 && delta.x < -END_SIZE)
                    {
                        testEnd = true;
                        delta.x = -END_SIZE;
                    }

                    if (isEndRight && col == numCols * 4 && delta.x > END_SIZE)
                    {
                        testEnd = true;
                        delta.x = END_SIZE;
                    }

                    length = Mathf.Abs(delta.x);

                    if (delta.x < 0.0f)
                    {
                        --nextCol;
                        angle = 180.0f;
                    }
                    else if (delta.x > 0.0f)
                    {
                        ++nextCol;
                        angle = 0.0f;
                    }
                }

                var pathRadius = DEFAULT_PATH_RADIUS * (1.0f - radiusWeight / 100.0f);
                var startRadius = DEFAULT_START_RADIUS * (1.0f - radiusWeight / 100.0f);

                if (nextRow != row || nextCol != col)
                {
                    if (testWaypoints.Length >= 2)
                    {
                        var prevWaypoint = testWaypoints[testWaypoints.Length - 2];
                        var prevRow = GetWaypointRow(prevWaypoint);
                        var prevCol = GetWaypointCol(prevWaypoint);

                        if (nextRow == prevRow && nextCol == prevCol)
                        {
                            DeleteWaypoint();
                            continue;
                        }
                    }

                    if (IsDisjoint(nextRow, nextCol))
                    {
                        length = Mathf.Min(length, BLOCK_SIZE / 4.0f - DISJOINT_WIDTH);
                    }
                    else
                    {
                        if (testWaypoints.Length > 2)
                        {
                            var nextPoint = GetWaypointPosition(nextRow, nextCol);
                            length = Mathf.Min(length, BLOCK_SIZE / 4.0f - (startRadius + pathRadius - (firstPoint - nextPoint).magnitude));
                            length = Mathf.Min(length, BLOCK_SIZE / 4.0f - (startRadius + pathRadius - (firstPointSymmetry - nextPoint).magnitude));
                        }

                        bool blocked = false;

                        if (GetTestMap(nextRow, nextCol))
                        {
                            length = Mathf.Min(length, BLOCK_SIZE / 4.0f - pathRadius * 2.0f);
                            blocked = true;
                        }
                        else if (symmetry != SYMMETRY_NONE)
                        {
                            var nextIndex = GetWaypointIndex(nextRow, nextCol);

                            if (GetSymmetryWaypointIndex(nextIndex) == nextIndex)
                            {
                                length = Mathf.Min(length, BLOCK_SIZE / 4.0f - pathRadius);
                                blocked = true;
                            }
                        }

                        if (!blocked && nextRow >= 0 && nextRow <= numRows * 4 && nextCol >= 0 && nextCol <= numCols * 4 && length > (BLOCK_SIZE / 4.0f))
                        {
                            AddWaypoint(nextRow, nextCol);
                            continue;
                        }
                    }
                }

                break;
            }

            var blendShapeWeight = length / BLOCK_SIZE * 100.0f;

            currentPathTransform.localPosition = lastPoint;
            currentPathTransform.localRotation = Quaternion.Euler(0.0f, angle, 0.0f);
            currentPathMeshRenderer.SetBlendShapeWeight(0, blendShapeWeight);
            currentPathMeshRenderer.SetBlendShapeWeight(1, radiusWeight);

            currentStartTransform.gameObject.SetActive(true);
            currentPathTransform.gameObject.SetActive(true);

            if (symmetry != SYMMETRY_NONE)
            {
                currentPathSymmetryTransform.localPosition = lastPointSymmetry;
                currentPathSymmetryTransform.localRotation = Quaternion.Euler(0.0f, GetSymmetryAngle(angle), 0.0f);
                currentPathSymmetryMeshRenderer.SetBlendShapeWeight(0, blendShapeWeight);
                currentPathSymmetryMeshRenderer.SetBlendShapeWeight(1, radiusWeight);

                currentStartSymmetryTransform.gameObject.SetActive(true);
                currentPathSymmetryTransform.gameObject.SetActive(true);
            }
        }

        private float GetSymmetryAngle(float angle)
        {
            switch (symmetry)
            {
                case SYMMETRY_HORIZONTAL:
                    return 180.0f - angle;
                case SYMMETRY_VERTICAL:
                    return -angle;
                case SYMMETRY_ROTATIONAL:
                    return angle + 180.0f;
            }

            return angle;
        }

        public void EndDrag()
        {
            if (tool != TOOL_TEST)
            {
                return;
            }

            if (testState != TEST_STATE_START)
            {
                return;
            }

            if (testEnd)
            {
                var succeeded = Check();
                testState = succeeded ? TEST_STATE_END_OK : TEST_STATE_END_ERROR;
                testProgress = 0.0f;

                if (succeeded)
                {
                    solved = true;
                    solvedByMyself = true;
                    UpdateStars();

                    if (!editor)
                    {
                        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnSolved));
                    }
                }

                RaiseStateChangedEvent();
                return;
            }

            ResetTest();
        }

        public void OnSolved()
        {
            solved = true;
            UpdateStars();
        }

        private bool Check()
        {
            var gridStates = GetGridStates();
            var cellAreas = GetCellAreas(gridStates);
            var gridAreas = GetGridAreas(gridStates, cellAreas);

            var uniqueAreas = (int[])cellAreas.Clone();
            Sort(uniqueAreas);
            uniqueAreas = Unique(uniqueAreas);

            failedObjects = new GameObject[cellGameObjects.Length + gridGameObjects.Length];
            numFailedObjects = 0;

            for (int i = 0; i < uniqueAreas.Length; ++i)
            {
                var area = uniqueAreas[i];
                int numY = 0;

                for (int j = 0; j < cells.Length; ++j)
                {
                    if (cellAreas[j] == area)
                    {
                        var c = cells[j];
                        var type = GetCellType(c);

                        switch (type)
                        {
                            case CELL_TYPE_Y:
                                ++numY;
                                break;
                        }
                    }
                }

                var fo = CheckArea(gridStates, cellAreas, gridAreas, area, cells, grids);

                Debug.Log("Check1 " + numY + " " + fo.Length);

                if (fo.Length < numY)
                {
                    int remain = numY - fo.Length;

                    for (int j = 0; j < cells.Length && remain != 0; ++j)
                    {
                        if (cellAreas[j] == area)
                        {
                            var c = cells[j];
                            var type = GetCellType(c);

                            switch (type)
                            {
                                case CELL_TYPE_Y:
                                    failedObjects[numFailedObjects++] = cellGameObjects[j];
                                    --remain;
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    var disables = FirstCombination(fo.Length, numY);

                    while (true)
                    {
                        var newCells = (ulong[])cells.Clone();
                        var newGrids = (ulong[])grids.Clone();

                        for (int j = 0; j < disables.Length; ++j)
                        {
                            if (disables[j])
                            {
                                for (int k = 0; k < newCells.Length; ++k)
                                {
                                    if (cellGameObjects[k] == fo[j])
                                    {
                                        newCells[k] = MakeCell(CELL_TYPE_EMPTY, 0, 0);
                                        break;
                                    }
                                }

                                for (int k = 0; k < newGrids.Length; ++k)
                                {
                                    if (gridGameObjects[k] == fo[j])
                                    {
                                        newGrids[k] = GRID_EMPTY;
                                        break;
                                    }
                                }
                            }
                        }

                        for (int j = 0; j < newCells.Length; ++j)
                        {
                            if (GetCellType(newCells[j]) == CELL_TYPE_Y)
                            {
                                newCells[j] = MakeCell(CELL_TYPE_EMPTY, 0, 0);
                            }
                        }

                        var fo2 = CheckArea(gridStates, cellAreas, gridAreas, area, newCells, newGrids);

                        Debug.Log("Check2 " + fo.Length);

                        if (fo2.Length == 0)
                        {
                            break;
                        }

                        disables = NextCombination(disables);

                        if (disables == null)
                        {
                            for (int j = 0; j < fo.Length; ++j)
                            {
                                failedObjects[numFailedObjects++] = fo[j];
                            }

                            break;
                        }
                    }
                }
            }

            for (int i = 0; i < numFailedObjects; ++i)
            {
                var baseObj = failedObjects[i];
                var newObj = VRCInstantiate(baseObj);
                var meshRenderers = (MeshRenderer[])newObj.GetComponentsInChildren(typeof(MeshRenderer));

                for (int j = 0; j < meshRenderers.Length; ++j)
                {
                    meshRenderers[j].sharedMaterial = errorMaterial;
                }

                var t = newObj.transform;
                t.SetParent(baseObj.transform, false);
                t.localPosition = new Vector3(0.0f, 0.0001f, 0.0f);
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;
                failedObjects[i] = newObj;
            }

            var succeeded = numFailedObjects == 0;
            audioSource.PlayOneShot(succeeded ? succeededAudioClip : failedAudioClip);

            return succeeded;
        }

        private bool[] FirstCombination(int n, int r)
        {
            var a = new bool[n];

            for (int i = 0; i < r; ++i)
            {
                a[i] = true;
            }

            return a;
        }

        private bool[] NextCombination(bool[] a)
        {
            a = (bool[])a.Clone();

            int firstIndex = 0;

            while (firstIndex < a.Length && !a[firstIndex])
            {
                ++firstIndex;
            }

            if (firstIndex == a.Length)
            {
                return null;
            }

            var secondIndex = firstIndex;

            while (secondIndex < a.Length && a[secondIndex])
            {
                ++secondIndex;
            }

            if (secondIndex == a.Length)
            {
                return null;
            }

            a[secondIndex] = true;

            int count = 0;

            for (int i = 0; i < secondIndex; ++i)
            {
                if (a[i])
                {
                    ++count;
                }

                a[i] = false;
            }

            for (int i = 0; i < count - 1; ++i)
            {
                a[i] = true;
            }

            return a;
        }

        private bool[] GetGridStates()
        {
            var gridStates = new bool[(numRows * 2 + 1) * (numCols * 2 + 1)];

            for (int i = 0; i < testWaypoints.Length; ++i)
            {
                var wp = testWaypoints[i];
                var row = GetWaypointRow(wp);
                var col = GetWaypointCol(wp);

                if (row % 2 == 0 && col % 2 == 0)
                {
                    var index = GetGridIndex(row / 2, col / 2);
                    gridStates[index] = true;
                    gridStates[GetSymmetryGridIndex(index)] = true;
                }
            }

            return gridStates;
        }

        private int[] GetCellAreas(bool[] gridStates)
        {
            var cellAreas = new int[numRows * numCols];

            for (int i = 0; i < cellAreas.Length; ++i)
            {
                cellAreas[i] = i;
            }

            while (true)
            {
                bool changed = false;

                for (int i = 0; i < numRows; ++i)
                {
                    for (int j = 0; j < numCols - 1; ++j)
                    {
                        if (!gridStates[GetGridIndex(i * 2 + 1, j * 2 + 2)])
                        {
                            var index1 = GetCellIndex(i, j);
                            var index2 = GetCellIndex(i, j + 1);
                            var a1 = cellAreas[index1];
                            var a2 = cellAreas[index2];

                            if (a1 != a2)
                            {
                                if (a1 < a2)
                                {
                                    cellAreas[index2] = a1;
                                }
                                else
                                {
                                    cellAreas[index1] = a2;
                                }

                                changed = true;
                            }
                        }
                    }
                }

                for (int i = 0; i < numRows - 1; ++i)
                {
                    for (int j = 0; j < numCols; ++j)
                    {
                        if (!gridStates[GetGridIndex(i * 2 + 2, j * 2 + 1)])
                        {
                            var index1 = GetCellIndex(i, j);
                            var index2 = GetCellIndex(i + 1, j);
                            var a1 = cellAreas[index1];
                            var a2 = cellAreas[index2];

                            if (a1 != a2)
                            {
                                if (a1 < a2)
                                {
                                    cellAreas[index2] = a1;
                                }
                                else
                                {
                                    cellAreas[index1] = a2;
                                }

                                changed = true;
                            }
                        }
                    }
                }

                if (!changed)
                {
                    break;
                }
            }

            return cellAreas;
        }

        private int[] GetGridAreas(bool[] gridStates, int[] cellAreas)
        {
            var gridAreas = new int[(numRows * 2 + 1) * (numCols * 2 + 1)];

            if (numRows == 0 || numCols == 0)
            {
                for (int i = 0; i < gridAreas.Length; ++i)
                {
                    gridAreas[i] = -1;
                }
            }
            else
            {
                for (int i = 0; i <= numRows * 2; ++i)
                {
                    for (int j = 0; j <= numCols * 2; ++j)
                    {
                        var gridIndex = GetGridIndex(i, j);

                        if (gridStates[gridIndex])
                        {
                            gridAreas[gridIndex] = -1;
                        }
                        else
                        {
                            var neighborRow = i / 2;
                            var neighborCol = j / 2;

                            if (neighborRow == numRows)
                            {
                                --neighborRow;
                            }

                            if (neighborCol == numCols)
                            {
                                --neighborCol;
                            }

                            gridAreas[gridIndex] = cellAreas[GetCellIndex(neighborRow, neighborCol)];
                        }
                    }
                }
            }

            return gridAreas;
        }

        private GameObject[] CheckArea(bool[] gridStates, int[] cellAreas, int[] gridAreas, int area, ulong[] cells, ulong[] grids)
        {
            var failedObjects = new GameObject[cellGameObjects.Length + gridGameObjects.Length];
            var numFailedObjects = 0;

            Debug.Log("CheckArea 1");

            {
                // Square and SunStar

                var squareExists = new bool[colorMaterials.Length];
                var numColoredElements = new int[colorMaterials.Length];

                Debug.Log("CheckArea 2");

                for (int i = 0; i < cells.Length; ++i)
                {
                    if (cellAreas[i] == area)
                    {
                        var c = cells[i];
                        var type = GetCellType(c);
                        var color = GetCellColor(c);

                        if (type != CELL_TYPE_EMPTY)
                        {
                            ++numColoredElements[color];

                            switch (type)
                            {
                                case CELL_TYPE_SQUARE:
                                    squareExists[color] = true;
                                    break;
                            }
                        }
                    }
                }

                Debug.Log("CheckArea 3");

                int numSquareColors = 0;

                for (int i = 0; i < squareExists.Length; ++i)
                {
                    if (squareExists[i])
                    {
                        ++numSquareColors;
                    }
                }

                Debug.Log("CheckArea 4");

                if (numSquareColors > 1)
                {
                    for (int i = 0; i < cells.Length; ++i)
                    {
                        if (cellAreas[i] == area)
                        {
                            var c = cells[i];
                            var type = GetCellType(c);

                            if (type == CELL_TYPE_SQUARE)
                            {
                                failedObjects[numFailedObjects++] = cellGameObjects[i];
                            }
                        }
                    }
                }

                Debug.Log("CheckArea 5");

                for (int i = 0; i < cells.Length; ++i)
                {
                    if (cellAreas[i] == area)
                    {
                        var c = cells[i];
                        var type = GetCellType(c);

                        if (type == CELL_TYPE_SUN_STAR)
                        {
                            var color = GetCellColor(c);

                            if (numColoredElements[color] != 2)
                            {
                                failedObjects[numFailedObjects++] = cellGameObjects[i];
                            }
                        }
                    }
                }
            }

            Debug.Log("CheckArea 6");

            {
                // Triangle

                for (int i = 0; i < numRows; ++i)
                {
                    for (int j = 0; j < numCols; ++j)
                    {
                        var index = GetCellIndex(i, j);

                        if (cellAreas[index] == area)
                        {
                            var c = cells[index];
                            var type = GetCellType(c);

                            switch (type)
                            {
                                case CELL_TYPE_TRIANGLE:
                                    {
                                        var expectedNum = GetCellPattern(c);
                                        int actualNum = 0;

                                        if (gridStates[GetGridIndex(i * 2 + 1, j * 2)])
                                        {
                                            ++actualNum;
                                        }

                                        if (gridStates[GetGridIndex(i * 2 + 1, j * 2 + 2)])
                                        {
                                            ++actualNum;
                                        }

                                        if (gridStates[GetGridIndex(i * 2, j * 2 + 1)])
                                        {
                                            ++actualNum;
                                        }

                                        if (gridStates[GetGridIndex(i * 2 + 2, j * 2 + 1)])
                                        {
                                            ++actualNum;
                                        }

                                        if (actualNum != expectedNum)
                                        {
                                            failedObjects[numFailedObjects++] = cellGameObjects[index];
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
            }

            Debug.Log("CheckArea 7");

            {
                // Polyomino

                int numPolyominos = 0;
                int numAreaCells = 0;
                int numPolyominoCells = 0;

                for (int i = 0; i < cells.Length; ++i)
                {
                    if (cellAreas[i] == area)
                    {
                        var c = cells[i];
                        var type = GetCellType(c);

                        switch (type)
                        {
                            case CELL_TYPE_POLYOMINO:
                            case CELL_TYPE_FREE_POLYOMINO:
                            case CELL_TYPE_BLUE_POLYOMINO:
                            case CELL_TYPE_FREE_BLUE_POLYOMINO:
                                ++numPolyominos;
                                break;
                        }

                        switch (type)
                        {
                            case CELL_TYPE_POLYOMINO:
                            case CELL_TYPE_FREE_POLYOMINO:
                                numPolyominoCells += GetPatternCells(GetCellPattern(c));
                                break;
                            case CELL_TYPE_BLUE_POLYOMINO:
                            case CELL_TYPE_FREE_BLUE_POLYOMINO:
                                numPolyominoCells -= GetPatternCells(GetCellPattern(c));
                                break;
                        }

                        ++numAreaCells;
                    }
                }

                Debug.Log("CheckArea 8");

                bool error = false;
                var fillMode = numAreaCells == numPolyominoCells;
                var emptyMode = numPolyominoCells == 0;

                if (fillMode && emptyMode)
                {
                    Debug.Log("CheckArea 9");
                }
                else if (!fillMode && !emptyMode)
                {
                    Debug.Log("CheckArea 10");
                    error = true;
                }
                else if (fillMode || emptyMode)
                {
                    Debug.Log("CheckArea 11");

                    var patterns = new uint[numPolyominos];
                    var blues = new bool[numPolyominos];
                    var frees = new bool[numPolyominos];
                    int index = 0;

                    for (int i = 0; i < cells.Length; ++i)
                    {
                        if (cellAreas[i] == area)
                        {
                            var c = cells[i];
                            var type = GetCellType(c);

                            switch (type)
                            {
                                case CELL_TYPE_BLUE_POLYOMINO:
                                case CELL_TYPE_FREE_BLUE_POLYOMINO:
                                    patterns[index] = GetCellPattern(c);
                                    blues[index] = true;
                                    frees[index] = type == CELL_TYPE_FREE_BLUE_POLYOMINO;
                                    ++index;
                                    break;
                            }
                        }
                    }

                    Debug.Log("CheckArea 12");

                    for (int i = 0; i < cells.Length; ++i)
                    {
                        if (cellAreas[i] == area)
                        {
                            var c = cells[i];
                            var type = GetCellType(c);

                            switch (type)
                            {
                                case CELL_TYPE_POLYOMINO:
                                case CELL_TYPE_FREE_POLYOMINO:
                                    patterns[index] = GetCellPattern(c);
                                    blues[index] = false;
                                    frees[index] = type == CELL_TYPE_FREE_POLYOMINO;
                                    ++index;
                                    break;
                            }
                        }
                    }

                    Debug.Log("CheckArea 13");

                    if (emptyMode)
                    {
                        for (int i = 0; i < numPolyominos / 2; ++i)
                        {
                            {
                                var t = patterns[i];
                                patterns[i] = patterns[numPolyominos - i - 1];
                                patterns[numPolyominos - i - 1] = t;
                            }
                            {
                                var t = blues[i];
                                blues[i] = blues[numPolyominos - i - 1];
                                blues[numPolyominos - i - 1] = t;
                            }
                            {
                                var t = frees[i];
                                frees[i] = frees[numPolyominos - i - 1];
                                frees[numPolyominos - i - 1] = t;
                            }
                        }
                    }

                    Debug.Log("CheckArea 14");

                    var patternRows = new int[numPolyominos];
                    var patternCols = new int[numPolyominos];

                    for (int i = 0; i < numPolyominos; ++i)
                    {
                        var pattern = patterns[i];
                        patternRows[i] = GetPatternRows(pattern);
                        patternCols[i] = GetPatternCols(pattern);
                    }

                    Debug.Log("CheckArea 15");

                    var placeRows = new int[numPolyominos];
                    var placeCols = new int[numPolyominos];
                    var placeRotation = new int[numPolyominos];

                    var mapStack = new int[numPolyominos + 1][];
                    mapStack[0] = new int[numRows * numCols];

                    for (int i = 0; i < cells.Length; ++i)
                    {
                        if (cellAreas[i] != area)
                        {
                            mapStack[0][i] = fillMode ? 1 : 0;
                        }
                    }

                    Debug.Log("CheckArea 16");

                    int depth = 0;

                    while (depth != numPolyominos)
                    {
                        var map = (int[])mapStack[depth].Clone();

                        Debug.Log("PlaceOnMap " + depth + " " + placeRows[depth] + " " + placeCols[depth] + " " + placeRotation[depth]);

                        if (PlaceOnMap(map, patterns[depth], blues[depth], placeRows[depth], placeCols[depth], placeRotation[depth], fillMode, emptyMode))
                        {
                            Debug.Log("PlaceOnMap SUCCEEDED");

                            ++depth;
                            mapStack[depth] = map;
                        }
                        else
                        {
                            Debug.Log("PlaceOnMap FAILED");

                            while (true)
                            {
                                ++placeCols[depth];
                                Debug.Log("INC COL");

                                var currentPatternRows = placeRotation[depth] % 2 == 0 ? patternRows[depth] : patternCols[depth];
                                var currentPatternCols = placeRotation[depth] % 2 == 0 ? patternCols[depth] : patternRows[depth];

                                if (placeCols[depth] > numCols - currentPatternCols)
                                {
                                    placeCols[depth] = 0;
                                    ++placeRows[depth];
                                    Debug.Log("INC ROW");

                                    if (placeRows[depth] > numRows - currentPatternRows)
                                    {
                                        placeRows[depth] = 0;
                                        ++placeRotation[depth];
                                        Debug.Log("INC ROT");

                                        int numRotation = frees[depth] ? 4 : 1;

                                        if (placeRotation[depth] >= numRotation)
                                        {
                                            placeRotation[depth] = 0;
                                            --depth;
                                            Debug.Log("DEC DEPTH");

                                            if (depth < 0)
                                            {
                                                error = true;
                                                break;
                                            }

                                            continue;
                                        }
                                    }
                                }

                                break;
                            }

                            if (error)
                            {
                                break;
                            }
                        }
                    }
                }

                Debug.Log("CheckArea 16");

                if (error)
                {
                    Debug.Log("CheckArea 17");

                    for (int i = 0; i < cells.Length; ++i)
                    {
                        if (cellAreas[i] == area)
                        {
                            var c = cells[i];
                            var type = GetCellType(c);

                            switch (type)
                            {
                                case CELL_TYPE_POLYOMINO:
                                case CELL_TYPE_FREE_POLYOMINO:
                                case CELL_TYPE_BLUE_POLYOMINO:
                                case CELL_TYPE_FREE_BLUE_POLYOMINO:
                                    failedObjects[numFailedObjects++] = cellGameObjects[i];
                                    break;
                            }
                        }
                    }
                }
            }

            Debug.Log("CheckArea 18");

            {
                // Hexagon

                for (int i = 0; i < grids.Length; ++i)
                {
                    if (gridAreas[i] == area)
                    {
                        if (grids[i] == GRID_HEXAGON)
                        {
                            if (!gridStates[i])
                            {
                                failedObjects[numFailedObjects++] = gridGameObjects[i];
                            }
                        }
                    }
                }
            }

            Debug.Log("CheckArea 19");

            var result = new GameObject[numFailedObjects];

            for (int i = 0; i < numFailedObjects; ++i)
            {
                result[i] = failedObjects[i];
            }

            Debug.Log("CheckArea 20");

            return result;
        }

        private bool PlaceOnMap(int[] map, uint pattern, bool blue, int row, int col, int rotation, bool fillMode, bool emptyMode)
        {
            var patternRows = GetPatternRows(pattern);
            var patternCols = GetPatternCols(pattern);

            var rotatedPatternRows = rotation % 2 == 0 ? patternRows : patternCols;
            var rotatedPatternCols = rotation % 2 == 0 ? patternCols : patternRows;

            if (row < 0 || row + rotatedPatternRows > numRows || col < 0 || col + rotatedPatternCols > numCols)
            {
                return false;
            }

            for (int i = 0; i < rotatedPatternRows; ++i)
            {
                for (int j = 0; j < rotatedPatternCols; ++j)
                {
                    int rotatedI = 0;
                    int rotatedJ = 0;

                    switch (rotation)
                    {
                        case 0:
                            rotatedI = i;
                            rotatedJ = j;
                            break;
                        case 1:
                            rotatedI = patternRows - j - 1;
                            rotatedJ = i;
                            break;
                        case 2:
                            rotatedI = patternRows - i - 1;
                            rotatedJ = patternCols - j - 1;
                            break;
                        case 3:
                            rotatedI = j;
                            rotatedJ = patternCols - i - 1;
                            break;
                    }

                    if (GetPatternBit(pattern, rotatedI, rotatedJ))
                    {
                        var index = GetCellIndex(row + i, col + j);
                        map[index] += blue ? -1 : 1;

                        if (fillMode)
                        {
                            if (map[index] > 1)
                            {
                                return false;
                            }
                        }
                        else if (emptyMode)
                        {
                            if (map[index] < 0)
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return true;
        }

        private void ResetTest()
        {
            testState = TEST_STATE_INIT;
            testProgress = 0.0f;
            testWaypoints = null;

            if (testPaths != null)
            {
                for (int i = 0; i < testPaths.Length; ++i)
                {
                    if (testPaths[i] != null)
                    {
                        Destroy(testPaths[i]);
                    }
                }

                testPaths = null;
            }

            testMap = null;

            currentStartTransform.gameObject.SetActive(false);
            currentStartSymmetryTransform.gameObject.SetActive(false);
            currentPathTransform.gameObject.SetActive(false);
            currentPathSymmetryTransform.gameObject.SetActive(false);

            for (int i = 0; i < numFailedObjects; ++i)
            {
                Destroy(failedObjects[i]);
            }

            failedObjects = new GameObject[0];
            numFailedObjects = 0;

            RaiseStateChangedEvent();
        }

        private bool GetTestMap(int row, int col)
        {
            if (row < 0 || row > numRows * 4 || col < 0 || col > numCols * 4)
            {
                return false;
            }

            return testMap[GetWaypointIndex(row, col)];
        }

        private bool IsEnd(int row, int col)
        {
            if (row % 4 != 0 || col % 4 != 0)
            {
                return false;
            }

            return grids[GetGridIndex(row / 2, col / 2)] == GRID_END;
        }

        private bool IsDisjoint(int row, int col)
        {
            if (row % 2 != 0 || col % 2 != 0)
            {
                return false;
            }

            var index = GetGridIndex(row / 2, col / 2);
            return grids[index] == GRID_DISJOINT || grids[GetSymmetryGridIndex(index)] == GRID_DISJOINT;
        }

        private void AddWaypoint(int row, int col)
        {
            var lastWaypoint = testWaypoints[testWaypoints.Length - 1];
            var lastRow = GetWaypointRow(lastWaypoint);
            var lastCol = GetWaypointCol(lastWaypoint);

            var lastPos1 = GetWaypointPosition(lastRow, lastCol);
            var pos1 = GetWaypointPosition(row, col);

            var newWaypoints = new int[testWaypoints.Length + 1];
            testWaypoints.CopyTo(newWaypoints, 0);
            newWaypoints[testWaypoints.Length] = GetWaypointIndex(row, col);
            testWaypoints = newWaypoints;

            var newPath1 = VRCInstantiate(whitePathPrefab);
            var t1 = newPath1.transform;
            var meshRenderer1 = (SkinnedMeshRenderer)t1.GetChild(0).GetComponent(typeof(SkinnedMeshRenderer));
            meshRenderer1.SetBlendShapeWeight(0, 25.0f);
            meshRenderer1.SetBlendShapeWeight(1, radiusWeight);
            t1.SetParent(pathTransform, false);
            t1.localPosition = lastPos1;
            t1.localRotation = Quaternion.Euler(0.0f, -Mathf.Atan2(pos1.z - lastPos1.z, pos1.x - lastPos1.x) * Mathf.Rad2Deg, 0.0f);

            GameObject newPath2 = null;

            if (symmetry != SYMMETRY_NONE)
            {
                var lastPos2 = GetSymmetryWaypointPosition(lastRow, lastCol);
                var pos2 = GetSymmetryWaypointPosition(row, col);

                newPath2 = VRCInstantiate(whitePathPrefab);
                var t2 = newPath2.transform;
                var meshRenderer2 = (SkinnedMeshRenderer)t2.GetChild(0).GetComponent(typeof(SkinnedMeshRenderer));
                meshRenderer2.SetBlendShapeWeight(0, 25.0f);
                meshRenderer2.SetBlendShapeWeight(1, radiusWeight);
                t2.SetParent(pathTransform, false);
                t2.localPosition = lastPos2;
                t2.localRotation = Quaternion.Euler(0.0f, -Mathf.Atan2(pos2.z - lastPos2.z, pos2.x - lastPos2.x) * Mathf.Rad2Deg, 0.0f);
            }

            var newPaths = new GameObject[testPaths.Length + 2];
            testPaths.CopyTo(newPaths, 0);
            newPaths[testPaths.Length] = newPath1;
            newPaths[testPaths.Length + 1] = newPath2;

            testPaths = newPaths;

            var index = GetWaypointIndex(row, col);
            testMap[index] = true;
            testMap[GetSymmetryWaypointIndex(index)] = true;
        }

        private void DeleteWaypoint()
        {
            if (testWaypoints.Length < 2)
            {
                return;
            }

            var lastWaypoint = testWaypoints[testWaypoints.Length - 1];
            var lastRow = GetWaypointRow(lastWaypoint);
            var lastCol = GetWaypointCol(lastWaypoint);

            var newWaypoints = new int[testWaypoints.Length - 1];

            for (int i = 0; i < newWaypoints.Length; ++i)
            {
                newWaypoints[i] = testWaypoints[i];
            }

            testWaypoints = newWaypoints;

            for (int i = 0; i < 2; ++i)
            {
                var path = testPaths[testPaths.Length - i - 1];

                if (path != null)
                {
                    Destroy(path);
                }
            }

            var newPaths = new GameObject[testPaths.Length - 2];

            for (int i = 0; i < newPaths.Length; ++i)
            {
                newPaths[i] = testPaths[i];
            }

            testPaths = newPaths;

            var index = GetWaypointIndex(lastRow, lastCol);
            testMap[index] = false;
            testMap[GetSymmetryWaypointIndex(index)] = false;
        }

        public int GetWaypointIndex(int row, int col)
        {
            return row * (numCols * 4 + 1) + col;
        }

        private int GetSymmetryWaypointIndex(int index)
        {
            var row = GetWaypointRow(index);
            var col = GetWaypointCol(index);

            switch (symmetry)
            {
                case SYMMETRY_HORIZONTAL:
                    return GetWaypointIndex(row, numCols * 4 - col);
                case SYMMETRY_VERTICAL:
                    return GetWaypointIndex(numRows * 4 - row, col);
                case SYMMETRY_ROTATIONAL:
                    return GetWaypointIndex(numRows * 4 - row, numCols * 4 - col);
            }

            return index;
        }

        public int GetWaypointRow(int index)
        {
            return index / (numCols * 4 + 1);
        }

        public int GetWaypointCol(int index)
        {
            return index % (numCols * 4 + 1);
        }

        public int GetCellIndex(int row, int col)
        {
            return row * numCols + col;
        }

        public int GetGridIndex(int row, int col)
        {
            return row * (numCols * 2 + 1) + col;
        }

        private int GetSymmetryGridIndex(int index)
        {
            var row = GetGridRow(index);
            var col = GetGridCol(index);

            switch (symmetry)
            {
                case SYMMETRY_HORIZONTAL:
                    return GetGridIndex(row, numCols * 2 - col);
                case SYMMETRY_VERTICAL:
                    return GetGridIndex(numRows * 2 - row, col);
                case SYMMETRY_ROTATIONAL:
                    return GetGridIndex(numRows * 2 - row, numCols * 2 - col);
            }

            return index;
        }

        private int GetGridRow(int index)
        {
            return index / (numCols * 2 + 1);
        }

        private int GetGridCol(int index)
        {
            return index % (numCols * 2 + 1);
        }

        private void Sort(int[] a)
        {
            if (a.Length == 0)
            {
                return;
            }

            int[] stackLeft = new int[a.Length];
            int[] stackRight = new int[a.Length];
            int count = 0;

            stackLeft[0] = 0;
            stackRight[0] = a.Length - 1;
            ++count;

            while (count != 0)
            {
                var left = stackLeft[count - 1];
                var right = stackRight[count - 1];
                --count;

                if (left < right)
                {
                    var i = left;
                    var j = right;

                    var pivot = Med3(a[i], a[i + (j - i) / 2], a[j]);

                    while (true)
                    {
                        while (a[i] < pivot)
                        {
                            ++i;
                        }

                        while (pivot < a[j])
                        {
                            --j;
                        }

                        if (i >= j)
                        {
                            break;
                        }

                        var temp = a[i];
                        a[i] = a[j];
                        a[j] = temp;

                        ++i;
                        --j;
                    }

                    stackLeft[count] = left;
                    stackRight[count] = i - 1;
                    ++count;

                    stackLeft[count] = j + 1;
                    stackRight[count] = right;
                    ++count;
                }
            }
        }

        private int Med3(int x, int y, int z)
        {
            if (x < y)
            {
                if (y < z)
                {
                    return y;
                }
                else if (z < x)
                {
                    return x;
                }
                else
                {
                    return z;
                }
            }
            else
            {
                if (z < y)
                {
                    return y;
                }
                else if (x < z)
                {
                    return x;
                }
                else
                {
                    return z;
                }
            }
        }

        private int[] Unique(int[] a)
        {
            if (a.Length == 0)
            {
                return a;
            }

            int count = 1;

            for (int i = 1; i < a.Length; ++i)
            {
                if (a[i - 1] != a[i])
                {
                    ++count;
                }
            }

            var result = new int[count];
            int index = 0;

            result[index++] = a[0];

            for (int i = 1; i < a.Length; ++i)
            {
                if (a[i - 1] != a[i])
                {
                    result[index++] = a[i];
                }
            }

            return result;
        }
    }
}
