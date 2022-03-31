using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace SuzuFactory.Ness
{
    [DefaultExecutionOrder(-6)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class Eraser : UdonSharpBehaviour
    {
        private bool active = false;

        public override void OnPickupUseDown()
        {
            active = true;
        }

        public override void OnPickupUseUp()
        {
            active = false;
        }

        void OnTriggerStay(Collider collider)
        {
            if (!active)
            {
                return;
            }

            var go = collider.gameObject;

            if (go == null)
            {
                return;
            }

            var panel = go.GetComponent<Panel>();

            if (panel == null)
            {
                return;
            }

            if (panel.isFixed)
            {
                return;
            }

            Networking.SetOwner(Networking.LocalPlayer, go);

            var pool = transform.Find("/PanelPool").GetComponent<PanelPool>();
            pool.Return(go);
        }
    }
}
