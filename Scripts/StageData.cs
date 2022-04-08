using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SuzuFactory.Ness
{
    [DefaultExecutionOrder(-8)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class StageData : UdonSharpBehaviour
    {
        [UdonSynced] private int numRows;
        [UdonSynced] private int numCols;
        [UdonSynced] private ulong[] cells;
        [UdonSynced] private ulong[] grids;
        [UdonSynced] private int symmetry;
        [UdonSynced] private bool solved;
        [UdonSynced] private int creator;
        [UdonSynced] private int version = 0;
        private int lastVersion = 0;

        private Stage stage;

        void Start()
        {
            stage = transform.parent.Find("Stage").GetComponent<Stage>();
        }

        void Update()
        {
            if (version != lastVersion)
            {
                var solvedByMyself = solved && creator == Networking.LocalPlayer.playerId;
                stage.Set(numRows, numCols, cells, grids, symmetry, solved, solvedByMyself);
                lastVersion = version;
            }
        }

        public void Set(Stage stage)
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);

            this.numRows = stage.NumRows;
            this.numCols = stage.NumCols;
            this.cells = stage.ClonedCells;
            this.grids = stage.ClonedGrids;
            this.symmetry = stage.Symmetry;
            this.solved = stage.IsSolved;
            this.creator = Networking.LocalPlayer.playerId;
            ++version;

            RequestSerialization();
        }

        public string Serialize()
        {
            return stage.Serialize();
        }

        public void Deserialize(string str)
        {
            stage.Deserialize(str);

            this.numRows = stage.NumRows;
            this.numCols = stage.NumCols;
            this.cells = stage.ClonedCells;
            this.grids = stage.ClonedGrids;
            this.symmetry = stage.Symmetry;
            this.solved = false;
            this.creator = -1;
            ++version;
        }
    }
}
