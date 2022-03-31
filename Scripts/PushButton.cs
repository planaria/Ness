using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SuzuFactory.Ness
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PushButton : UdonSharpBehaviour
    {
        public void Push()
        {
            var p = transform.parent;

            while (p != null)
            {
                var editorBoard = p.GetComponent<EditorBoard>();

                if (editorBoard != null)
                {
                    if (name.StartsWith("Cell") && name.EndsWith("Button") && name.Length == 12)
                    {
                        var row = name[4] - '0';
                        var col = name[5] - '0';
                        editorBoard.ToggleToolCell(row, col);
                    }

                    editorBoard.SendCustomEvent("On" + name);
                    break;
                }

                p = p.parent;
            }
        }
    }
}
