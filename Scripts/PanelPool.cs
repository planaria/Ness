using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SuzuFactory.Ness
{
    [DefaultExecutionOrder(-5)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class PanelPool : UdonSharpBehaviour
    {
        private GameObject[] children;
        [UdonSynced] private bool[] used = new bool[0];
        private float startTime = 0.0f;

        void Start()
        {
            children = new GameObject[transform.childCount];

            for (int i = 0; i < transform.childCount; ++i)
            {
                children[i] = transform.GetChild(i).gameObject;
            }

            used = new bool[children.Length];
            startTime = Time.time;
        }

        private int index = 0;

        void Update()
        {
            if (Time.time < startTime + 3.0f)
            {
                return;
            }

            if (children.Length == 0)
            {
                return;
            }

            if (index >= children.Length)
            {
                index = 0;
            }

            children[index].gameObject.SetActive(used[index]);

            ++index;
        }

        public GameObject TryToSpawn()
        {
            Debug.Log(nameof(TryToSpawn));

            Networking.SetOwner(Networking.LocalPlayer, gameObject);

            for (int i = 0; i < children.Length; ++i)
            {
                if (!used[i])
                {
                    used[i] = true;
                    RequestSerialization();

                    var obj = children[i];
                    Networking.SetOwner(Networking.LocalPlayer, obj.gameObject);
                    obj.gameObject.SetActive(true);

                    return obj;
                }
            }

            return null;
        }

        public void Return(GameObject obj)
        {
            Debug.Log(nameof(Return));

            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            obj.SetActive(false);

            for (int i = 0; i < children.Length; ++i)
            {
                if (children[i] == obj)
                {
                    used[i] = false;
                    break;
                }
            }

            RequestSerialization();
        }
    }
}
