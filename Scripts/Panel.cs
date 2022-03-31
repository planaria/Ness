using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace SuzuFactory.Ness
{
    [DefaultExecutionOrder(-6)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class Panel : UdonSharpBehaviour
    {
        public bool isFixed = false;
        public string serializedData;

        void Start()
        {
            if (!string.IsNullOrWhiteSpace(serializedData))
            {
                var stageData = transform.Find("StageData").GetComponent<StageData>();
                stageData.Deserialize(serializedData);
            }
        }
    }
}
