using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SuzuFactory.Ness
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class IndexedCollider : UdonSharpBehaviour
    {
        [NonSerialized] public int type = 0;
        [NonSerialized] public int index = 0;

        public void Push()
        {
            var p = transform.parent;

            while (p != null)
            {
                var editorBoard = p.GetComponent<EditorBoard>();

                if (editorBoard != null)
                {
                    editorBoard.OnIndexedCollider(type, index);
                    break;
                }

                p = p.parent;
            }
        }
    }
}
