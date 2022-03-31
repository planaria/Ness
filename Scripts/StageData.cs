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

        void Update()
        {
            if (version != lastVersion)
            {
                var stage = transform.parent.Find("Stage").GetComponent<Stage>();
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

        private const string START_SIGNATURE = "NESS_START_";
        private const string END_SIGNATURE = "_END_NESS";
        private const uint CURRENT_VERSION = 1;

        public string Serialize()
        {
            var data = new byte[4 + 1 + 1 + cells.Length * 8 + grids.Length * 1];
            int index = 0;

            for (int j = 0; j < 4; ++j)
            {
                data[index++] = (byte)((CURRENT_VERSION >> (j * 8)) & 0xffu);
            }

            data[index++] = (byte)numRows;
            data[index++] = (byte)numCols;

            for (int i = 0; i < cells.Length; ++i)
            {
                var c = cells[i];

                for (int j = 0; j < 8; ++j)
                {
                    data[index++] = (byte)((c >> (j * 8)) & 0xffu);
                }
            }

            for (int i = 0; i < grids.Length; ++i)
            {
                var g = grids[i];

                for (int j = 0; j < 1; ++j)
                {
                    data[index++] = (byte)((g >> (j * 8)) & 0xffu);
                }
            }

            return START_SIGNATURE + System.Convert.ToBase64String(data) + END_SIGNATURE;
        }

        public void Deserialize(string str)
        {
            if (!str.StartsWith(START_SIGNATURE))
            {
                return;
            }

            if (!str.EndsWith(END_SIGNATURE))
            {
                return;
            }

            str = str.Substring(START_SIGNATURE.Length, str.Length - START_SIGNATURE.Length - END_SIGNATURE.Length);

            var data = System.Convert.FromBase64String(str);
            int index = 0;

            if (data.Length < 4)
            {
                return;
            }

            uint version = 0;

            for (int j = 0; j < 4; ++j)
            {
                version |= (uint)data[index++] << (j * 8);
            }

            if (version > CURRENT_VERSION)
            {
                return;
            }

            if (data.Length < 6)
            {
                return;
            }

            var numRows = (int)data[index++];
            var numCols = (int)data[index++];

            if (numRows > 10 || numCols > 10)
            {
                return;
            }

            var cells = new ulong[numRows * numCols];
            var grids = new ulong[(numRows * 2 + 1) * (numCols * 2 + 1)];

            if (data.Length != 4 + 1 + 1 + cells.Length * 8 + grids.Length * 1)
            {
                return;
            }

            for (int i = 0; i < cells.Length; ++i)
            {
                ulong c = 0;

                for (int j = 0; j < 8; ++j)
                {
                    c |= (ulong)data[index++] << (j * 8);
                }

                cells[i] = c;
            }

            for (int i = 0; i < grids.Length; ++i)
            {
                ulong g = 0;

                for (int j = 0; j < 1; ++j)
                {
                    g |= (ulong)data[index++] << (j * 8);
                }

                grids[i] = g;
            }

            this.numRows = numRows;
            this.numCols = numCols;
            this.cells = cells;
            this.grids = grids;
            ++this.version;
        }
    }
}
