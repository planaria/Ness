using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SuzuFactory.Ness
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class StartCollider : UdonSharpBehaviour
    {
        [NonSerialized] public int index = 0;

        private Stage stage;
        private bool drag = false;

        public Stage GetStage()
        {
            if (stage == null)
            {
                var p = transform.parent;

                while (p != null)
                {
                    var s = p.GetComponent<Stage>();

                    if (s != null)
                    {
                        stage = s;
                        break;
                    }

                    p = p.parent;
                }
            }

            return stage;
        }

        public void Drag(Vector3 position)
        {
            var s = GetStage();

            if (!drag)
            {
                s.StartDrag(index);
            }

            s.Drag(position);

            drag = true;
        }

        public void EndDrag()
        {
            var s = GetStage();
            s.EndDrag();
            drag = false;
        }
    }
}
