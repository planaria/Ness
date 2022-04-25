using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

namespace SuzuFactory.Ness
{
    [DefaultExecutionOrder(-3)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PanelBuilder : UdonSharpBehaviour
    {
        public bool twin = false;
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
                if (!string.IsNullOrWhiteSpace(lines[i]))
                {
                    lines[numLines++] = lines[i];
                }
            }

            this.lines = new string[numLines];

            for (int i = 0; i < numLines; ++i)
            {
                this.lines[i] = lines[i];
            }

            spawn = transform.Find("Spawn");
        }

        public string GetRandomData()
        {
            if (lines.Length == 0)
            {
                return null;
            }

            var index = Random.Range(0, lines.Length - 1);
            return lines[index];
        }

        public override void Interact()
        {
            var pool = transform.Find("/PanelPool").GetComponent<PanelPool>();

            if (twin)
            {
                var baseIndex = Random.Range(0, lines.Length / 2 - 1);
                var index1 = baseIndex * 2;
                var index2 = baseIndex * 2 + 1;

                var data1 = lines[index1];
                var data2 = lines[index2];

                Debug.Log("lines.Length=" + lines.Length);
                Debug.Log("index1=" + index1 + ", index2=" + index2);
                Debug.Log("data1=" + data1 + ", data2=" + data2);

                var obj1 = pool.TryToSpawn();
                var obj2 = pool.TryToSpawn();

                if (obj1 != null && obj2 != null)
                {
                    obj1.transform.SetPositionAndRotation(spawn.position + new Vector3(-0.15f, 0.15f, 0.0f), spawn.rotation);
                    obj2.transform.SetPositionAndRotation(spawn.position + new Vector3(0.15f, -0.15f, 0.0f), spawn.rotation);

                    var sync1 = (VRCObjectSync)obj1.GetComponent(typeof(VRCObjectSync));
                    sync1.FlagDiscontinuity();

                    var sync2 = (VRCObjectSync)obj2.GetComponent(typeof(VRCObjectSync));
                    sync2.FlagDiscontinuity();

                    var newStageData1 = obj1.transform.Find("StageData").GetComponent<StageData>();
                    newStageData1.Deserialize(data1);

                    var newStageData2 = obj2.transform.Find("StageData").GetComponent<StageData>();
                    newStageData2.Deserialize(data2);

                    Debug.Log("BUILD: " + newStageData1.Serialize());
                    Debug.Log("BUILD: " + newStageData2.Serialize());
                }
                else
                {
                    if (obj1 != null)
                    {
                        pool.Return(obj1);
                    }

                    if (obj2 != null)
                    {
                        pool.Return(obj2);
                    }
                }
            }
            else
            {
                var data = GetRandomData();

                var obj = pool.TryToSpawn();

                if (obj != null)
                {
                    obj.transform.SetPositionAndRotation(spawn.position, spawn.rotation);

                    var sync = (VRCObjectSync)obj.GetComponent(typeof(VRCObjectSync));
                    sync.FlagDiscontinuity();

                    var newStageData = obj.transform.Find("StageData").GetComponent<StageData>();
                    newStageData.Deserialize(data);

                    Debug.Log("BUILD: " + newStageData.Serialize());
                }
            }
        }
    }
}
