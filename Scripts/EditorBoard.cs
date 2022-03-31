using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Components;

namespace SuzuFactory.Ness
{
    [DefaultExecutionOrder(-6)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class EditorBoard : UdonSharpBehaviour
    {
        public Material buttonMaterial;
        public Material activeButtonMaterial;
        public Material disabledButtonMaterial;
        public Material[] colorMaterials;

        private Stage stage;
        private MeshRenderer squareButtonRenderer;
        private MeshRenderer sunStarButtonRenderer;
        private MeshRenderer polyominoButtonRenderer;
        private MeshRenderer bluePolyominoButtonRenderer;
        private MeshRenderer freePolyominoButtonRenderer;
        private MeshRenderer freeBluePolyominoButtonRenderer;
        private MeshRenderer yButtonRenderer;
        private MeshRenderer triangleButtonRenderer;
        private GameObject triangleIcon1;
        private GameObject triangleIcon2;
        private GameObject triangleIcon3;
        private MeshRenderer startButtonRenderer;
        private MeshRenderer endButtonRenderer;
        private MeshRenderer hexagonButtonRenderer;
        private MeshRenderer disjointButtonRenderer;
        private MeshRenderer eraserButtonRenderer;
        private MeshRenderer testButtonRenderer;
        private GameObject toolCells;
        private GameObject colorButtons;
        private MeshRenderer noSymmetryButton;
        private MeshRenderer horizontalSymmetryButton;
        private MeshRenderer verticalSymmetryButton;
        private MeshRenderer rotationalSymmetryButton;
        private MeshRenderer[] toolCellMeshRenderers;

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

        private int triangleMode = TOOL_TRIANGLE_1;

        private const int MAX_NUM_POLYOMINO_ROWS = 5;
        private const int MAX_NUM_POLYOMINO_COLS = 5;

        private const uint COLOR_BLACK = 0;
        private const uint COLOR_WHITE = 1;
        private const uint COLOR_CYAN = 2;
        private const uint COLOR_MAGENTA = 3;
        private const uint COLOR_YELLOW = 4;
        private const uint COLOR_RED = 5;
        private const uint COLOR_GREEN = 6;
        private const uint COLOR_BLUE = 7;
        private const uint COLOR_ORANGE = 8;

        private const int SYMMETRY_NONE = 0;
        private const int SYMMETRY_HORIZONTAL = 1;
        private const int SYMMETRY_VERTICAL = 2;
        private const int SYMMETRY_ROTATIONAL = 3;

        void Start()
        {
            var model = transform.Find("EditorBoard");
            stage = model.Find("Center/Stage").GetComponent<Stage>();
            squareButtonRenderer = (MeshRenderer)model.Find("SquareButton").GetComponent(typeof(MeshRenderer));
            sunStarButtonRenderer = (MeshRenderer)model.Find("SunStarButton").GetComponent(typeof(MeshRenderer));
            polyominoButtonRenderer = (MeshRenderer)model.Find("PolyominoButton").GetComponent(typeof(MeshRenderer));
            bluePolyominoButtonRenderer = (MeshRenderer)model.Find("BluePolyominoButton").GetComponent(typeof(MeshRenderer));
            freePolyominoButtonRenderer = (MeshRenderer)model.Find("FreePolyominoButton").GetComponent(typeof(MeshRenderer));
            freeBluePolyominoButtonRenderer = (MeshRenderer)model.Find("FreeBluePolyominoButton").GetComponent(typeof(MeshRenderer));
            yButtonRenderer = (MeshRenderer)model.Find("YButton").GetComponent(typeof(MeshRenderer));
            triangleButtonRenderer = (MeshRenderer)model.Find("TriangleButton").GetComponent(typeof(MeshRenderer));
            triangleIcon1 = model.Find("TriangleButton/Triangle1").gameObject;
            triangleIcon2 = model.Find("TriangleButton/Triangle2").gameObject;
            triangleIcon3 = model.Find("TriangleButton/Triangle3").gameObject;
            startButtonRenderer = (MeshRenderer)model.Find("StartButton").GetComponent(typeof(MeshRenderer));
            endButtonRenderer = (MeshRenderer)model.Find("EndButton").GetComponent(typeof(MeshRenderer));
            hexagonButtonRenderer = (MeshRenderer)model.Find("HexagonButton").GetComponent(typeof(MeshRenderer));
            disjointButtonRenderer = (MeshRenderer)model.Find("DisjointButton").GetComponent(typeof(MeshRenderer));
            eraserButtonRenderer = (MeshRenderer)model.Find("EraserButton").GetComponent(typeof(MeshRenderer));
            testButtonRenderer = (MeshRenderer)model.Find("TestButton").GetComponent(typeof(MeshRenderer));
            toolCells = model.Find("ToolCells").gameObject;
            colorButtons = model.Find("ColorButtons").gameObject;
            noSymmetryButton = (MeshRenderer)model.Find("NoSymmetryButton").GetComponent(typeof(MeshRenderer));
            horizontalSymmetryButton = (MeshRenderer)model.Find("HorizontalSymmetryButton").GetComponent(typeof(MeshRenderer));
            verticalSymmetryButton = (MeshRenderer)model.Find("VerticalSymmetryButton").GetComponent(typeof(MeshRenderer));
            rotationalSymmetryButton = (MeshRenderer)model.Find("RotationalSymmetryButton").GetComponent(typeof(MeshRenderer));

            toolCellMeshRenderers = new MeshRenderer[MAX_NUM_POLYOMINO_ROWS * MAX_NUM_POLYOMINO_COLS];

            for (int i = 0; i < toolCellMeshRenderers.Length; ++i)
            {
                int row = i / MAX_NUM_POLYOMINO_COLS;
                int col = i % MAX_NUM_POLYOMINO_COLS;
                var n = "ToolCells/Cell" + row + col + "Button";
                toolCellMeshRenderers[i] = (MeshRenderer)model.Find(n).GetComponent(typeof(MeshRenderer));
            }

            SendCustomEventDelayedFrames(nameof(UpdateObjects), 1);
        }

        public void OnSquareButton()
        {
            stage.Tool = TOOL_SQUARE;
            UpdateObjects();
        }

        public void OnSunStarButton()
        {
            stage.Tool = TOOL_SUN_STAR;
            UpdateObjects();
        }

        public void OnPolyominoButton()
        {
            stage.Tool = TOOL_POLYOMINO;
            UpdateObjects();
        }

        public void OnBluePolyominoButton()
        {
            stage.Tool = TOOL_BLUE_POLYOMINO;
            UpdateObjects();
        }

        public void OnFreePolyominoButton()
        {
            stage.Tool = TOOL_FREE_POLYOMINO;
            UpdateObjects();
        }

        public void OnFreeBluePolyominoButton()
        {
            stage.Tool = TOOL_FREE_BLUE_POLYOMINO;
            UpdateObjects();
        }

        public void OnYButton()
        {
            stage.Tool = TOOL_Y;
            UpdateObjects();
        }

        public void OnTriangleButton()
        {
            switch (stage.Tool)
            {
                case TOOL_TRIANGLE_1:
                    triangleMode = TOOL_TRIANGLE_2;
                    break;
                case TOOL_TRIANGLE_2:
                    triangleMode = TOOL_TRIANGLE_3;
                    break;
                case TOOL_TRIANGLE_3:
                    triangleMode = TOOL_TRIANGLE_1;
                    break;
            }

            stage.Tool = triangleMode;
            UpdateObjects();
        }

        public void OnStartButton()
        {
            stage.Tool = TOOL_START;
            UpdateObjects();
        }

        public void OnEndButton()
        {
            stage.Tool = TOOL_END;
            UpdateObjects();
        }

        public void OnHexagonButton()
        {
            stage.Tool = TOOL_HEXAGON;
            UpdateObjects();
        }

        public void OnDisjointButton()
        {
            stage.Tool = TOOL_DISJONT;
            UpdateObjects();
        }

        public void OnEraserButton()
        {
            stage.Tool = TOOL_ERASER;
            UpdateObjects();
        }

        public void OnClearButton()
        {
            stage.ClearAll();
            UpdateObjects();
        }

        public void OnTestButton()
        {
            stage.Tool = TOOL_TEST;
            UpdateObjects();
        }

        public void OnBuildButton()
        {
            var pool = transform.Find("/PanelPool").GetComponent<PanelPool>();
            var obj = pool.TryToSpawn();

            if (obj != null)
            {
                obj.transform.position = transform.position + transform.rotation * new Vector3(0.0f, 0.02f, 0.0f);
                obj.transform.rotation = transform.rotation;

                var sync = (VRCObjectSync)obj.GetComponent(typeof(VRCObjectSync));
                sync.FlagDiscontinuity();

                var newStageData = obj.transform.Find("StageData").GetComponent<StageData>();
                newStageData.Set(stage);

                Debug.Log("BUILD: " + newStageData.Serialize());
            }
        }

        public void OnNoSymmetryButton()
        {
            stage.Symmetry = SYMMETRY_NONE;
            UpdateObjects();
        }

        public void OnHorizontalSymmetryButton()
        {
            stage.Symmetry = SYMMETRY_HORIZONTAL;
            UpdateObjects();
        }

        public void OnVerticalSymmetryButton()
        {
            stage.Symmetry = SYMMETRY_VERTICAL;
            UpdateObjects();
        }

        public void OnRotationalSymmetryButton()
        {
            stage.Symmetry = SYMMETRY_ROTATIONAL;
            UpdateObjects();
        }

        public void OnIndexedCollider(int type, int index)
        {
            stage.OnIndexedCollider(type, index);
        }

        public void UpdateObjects()
        {
            triangleIcon1.SetActive(false);
            triangleIcon2.SetActive(false);
            triangleIcon3.SetActive(false);

            switch (triangleMode)
            {
                case TOOL_TRIANGLE_1:
                    triangleIcon1.SetActive(true);
                    break;
                case TOOL_TRIANGLE_2:
                    triangleIcon2.SetActive(true);
                    break;
                case TOOL_TRIANGLE_3:
                    triangleIcon3.SetActive(true);
                    break;
            }

            squareButtonRenderer.sharedMaterial = buttonMaterial;
            sunStarButtonRenderer.sharedMaterial = buttonMaterial;
            polyominoButtonRenderer.sharedMaterial = buttonMaterial;
            bluePolyominoButtonRenderer.sharedMaterial = buttonMaterial;
            freePolyominoButtonRenderer.sharedMaterial = buttonMaterial;
            freeBluePolyominoButtonRenderer.sharedMaterial = buttonMaterial;
            yButtonRenderer.sharedMaterial = buttonMaterial;
            triangleButtonRenderer.sharedMaterial = buttonMaterial;
            startButtonRenderer.sharedMaterial = buttonMaterial;
            endButtonRenderer.sharedMaterial = buttonMaterial;
            hexagonButtonRenderer.sharedMaterial = buttonMaterial;
            disjointButtonRenderer.sharedMaterial = buttonMaterial;
            eraserButtonRenderer.sharedMaterial = buttonMaterial;
            testButtonRenderer.sharedMaterial = buttonMaterial;

            switch (stage.Tool)
            {
                case TOOL_SQUARE:
                    squareButtonRenderer.sharedMaterial = activeButtonMaterial;
                    break;
                case TOOL_SUN_STAR:
                    sunStarButtonRenderer.sharedMaterial = activeButtonMaterial;
                    break;
                case TOOL_POLYOMINO:
                    polyominoButtonRenderer.sharedMaterial = activeButtonMaterial;
                    break;
                case TOOL_BLUE_POLYOMINO:
                    bluePolyominoButtonRenderer.sharedMaterial = activeButtonMaterial;
                    break;
                case TOOL_FREE_POLYOMINO:
                    freePolyominoButtonRenderer.sharedMaterial = activeButtonMaterial;
                    break;
                case TOOL_FREE_BLUE_POLYOMINO:
                    freeBluePolyominoButtonRenderer.sharedMaterial = activeButtonMaterial;
                    break;
                case TOOL_Y:
                    yButtonRenderer.sharedMaterial = activeButtonMaterial;
                    break;
                case TOOL_TRIANGLE_1:
                case TOOL_TRIANGLE_2:
                case TOOL_TRIANGLE_3:
                    triangleButtonRenderer.sharedMaterial = activeButtonMaterial;
                    break;
                case TOOL_START:
                    startButtonRenderer.sharedMaterial = activeButtonMaterial;
                    break;
                case TOOL_END:
                    endButtonRenderer.sharedMaterial = activeButtonMaterial;
                    break;
                case TOOL_HEXAGON:
                    hexagonButtonRenderer.sharedMaterial = activeButtonMaterial;
                    break;
                case TOOL_DISJONT:
                    disjointButtonRenderer.sharedMaterial = activeButtonMaterial;
                    break;
                case TOOL_ERASER:
                    eraserButtonRenderer.sharedMaterial = activeButtonMaterial;
                    break;
                case TOOL_TEST:
                    testButtonRenderer.sharedMaterial = activeButtonMaterial;
                    break;
            }

            Material colorMaterial = colorMaterials[stage.Color];
            SetIconMaterial(squareButtonRenderer, colorMaterial);
            SetIconMaterial(sunStarButtonRenderer, colorMaterial);
            SetIconMaterial(polyominoButtonRenderer, colorMaterial);
            SetIconMaterial(bluePolyominoButtonRenderer, colorMaterial);
            SetIconMaterial(freePolyominoButtonRenderer, colorMaterial);
            SetIconMaterial(freeBluePolyominoButtonRenderer, colorMaterial);
            SetIconMaterial(yButtonRenderer, colorMaterial);
            SetIconMaterial(triangleButtonRenderer, colorMaterial);

            var pattern = stage.Pattern;

            for (int i = 0; i < MAX_NUM_POLYOMINO_ROWS; ++i)
            {
                for (int j = 0; j < MAX_NUM_POLYOMINO_COLS; ++j)
                {
                    var index = stage.GetPatternIndex(i, j);
                    toolCellMeshRenderers[index].sharedMaterial = stage.GetPatternBit(pattern, i, j) ? colorMaterials[1] : colorMaterials[0];
                }
            }

            noSymmetryButton.sharedMaterial = buttonMaterial;
            horizontalSymmetryButton.sharedMaterial = stage.CanSetSymmetryHorizontal ? buttonMaterial : disabledButtonMaterial;
            verticalSymmetryButton.sharedMaterial = stage.CanSetSymmetryVertical ? buttonMaterial : disabledButtonMaterial;
            rotationalSymmetryButton.sharedMaterial = stage.CanSetSymmetryRotational ? buttonMaterial : disabledButtonMaterial;

            switch (stage.Symmetry)
            {
                case SYMMETRY_NONE:
                    noSymmetryButton.sharedMaterial = activeButtonMaterial;
                    break;
                case SYMMETRY_HORIZONTAL:
                    horizontalSymmetryButton.sharedMaterial = activeButtonMaterial;
                    break;
                case SYMMETRY_VERTICAL:
                    verticalSymmetryButton.sharedMaterial = activeButtonMaterial;
                    break;
                case SYMMETRY_ROTATIONAL:
                    rotationalSymmetryButton.sharedMaterial = activeButtonMaterial;
                    break;
            }
        }

        private void SetIconMaterial(MeshRenderer mr, Material m)
        {
            var mrs = (MeshRenderer[])mr.gameObject.GetComponentsInChildren(typeof(MeshRenderer));

            for (int i = 0; i < mrs.Length; ++i)
            {
                if (mrs[i] == mr)
                {
                    continue;
                }

                mrs[i].sharedMaterial = m;
            }
        }

        public void ToggleToolCell(int row, int col)
        {
            stage.Pattern = stage.SetPatternBit(stage.Pattern, row, col, !stage.GetPatternBit(stage.Pattern, row, col));
            UpdateObjects();
        }

        public void OnBlackButton()
        {
            stage.Color = COLOR_BLACK;
            UpdateObjects();
        }

        public void OnWhiteButton()
        {
            stage.Color = COLOR_WHITE;
            UpdateObjects();
        }

        public void OnCyanButton()
        {
            stage.Color = COLOR_CYAN;
            UpdateObjects();
        }

        public void OnMagentaButton()
        {
            stage.Color = COLOR_MAGENTA;
            UpdateObjects();
        }

        public void OnYellowButton()
        {
            stage.Color = COLOR_YELLOW;
            UpdateObjects();
        }

        public void OnRedButton()
        {
            stage.Color = COLOR_RED;
            UpdateObjects();
        }

        public void OnGreenButton()
        {
            stage.Color = COLOR_GREEN;
            UpdateObjects();
        }

        public void OnBlueButton()
        {
            stage.Color = COLOR_BLUE;
            UpdateObjects();
        }

        public void OnOrangeButton()
        {
            stage.Color = COLOR_ORANGE;
            UpdateObjects();
        }

        public void OnAddColumnLeftButton()
        {
            stage.Resize(0, 1, 0, -1);
            UpdateObjects();
        }

        public void OnAddColumnRightButton()
        {
            stage.Resize(0, 1, 0, 0);
            UpdateObjects();
        }

        public void OnAddRowTopButton()
        {
            stage.Resize(1, 0, 0, 0);
            UpdateObjects();
        }

        public void OnAddRowBottomButton()
        {
            stage.Resize(1, 0, -1, 0);
            UpdateObjects();
        }

        public void OnDeleteColumnLeftButton()
        {
            stage.Resize(0, -1, 0, 1);
            UpdateObjects();
        }

        public void OnDeleteColumnRightButton()
        {
            stage.Resize(0, -1, 0, 0);
            UpdateObjects();
        }

        public void OnDeleteRowTopButton()
        {
            stage.Resize(-1, 0, 0, 0);
            UpdateObjects();
        }

        public void OnDeleteRowBottomButton()
        {
            stage.Resize(-1, 0, 1, 0);
            UpdateObjects();
        }
    }
}
