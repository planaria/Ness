using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace SuzuFactory.Ness
{
    [DefaultExecutionOrder(-1)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PanelBuilder : UdonSharpBehaviour
    {
        public TextAsset data;
        private Transform spawn;
        private string[] lines;

        void Start()
        {
            var lines = data.text.Split('\n');

            for (int i = 0; i < lines.Length; ++i)
            {
                var line = lines[i];

                var commentBegin = line.IndexOf("//");

                if (commentBegin != -1)
                {
                    line = line.Substring(0, commentBegin);
                }

                line = line.Trim();
                lines[i] = line;
            }

            int numLines = 0;

            for (int i = 0; i < lines.Length; ++i)
            {
                if (lines[i] != "")
                {
                    if (i != numLines)
                    {
                        lines[numLines++] = lines[i];
                    }
                }
            }

            this.lines = new string[numLines];

            for (int i = 0; i < numLines; ++i)
            {
                this.lines[i] = lines[i];
            }

            spawn = transform.Find("Spawn");
        }

        public override void Interact()
        {
            if (lines.Length == 0)
            {
                return;
            }

            var index = Random.Range(0, lines.Length - 1);

            var pool = transform.Find("/PanelPool").GetComponent<PanelPool>();
            var obj = pool.TryToSpawn();

            if (obj != null)
            {
                obj.transform.SetPositionAndRotation(spawn.position, spawn.rotation);

                var sync = (VRCObjectSync)obj.GetComponent(typeof(VRCObjectSync));
                sync.FlagDiscontinuity();

                var newStageData = obj.transform.Find("StageData").GetComponent<StageData>();
                newStageData.Deserialize(lines[index]);

                Debug.Log("BUILD: " + newStageData.Serialize());
            }
        }
    }
}
