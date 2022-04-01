using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace SuzuFactory.Ness
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LaserPointer : UdonSharpBehaviour
    {
#if UNITY_EDITOR
        public bool trigger = false;
        private bool lastTrigger = false;
#endif

        private Transform pointer;
        private Transform laser;
        private GameObject handle;
        private VRC_Pickup pickup;
        private GameObject lastTarget = null;
        private StartCollider dragTarget = null;

        private const int DEFAULT_LAYER = 1 << 0;
        private const int LASER_BUTTON_LAYER_INDEX = 26;
        private const int LASER_BUTTON_LAYER = 1 << LASER_BUTTON_LAYER_INDEX;

        private const float MAX_DISTANCE = 3.0f;

        void Start()
        {
            pointer = transform.Find("Pointer");
            laser = transform.Find("Laser");
            handle = transform.Find("Handle").gameObject;
            pickup = (VRC_Pickup)GetComponent(typeof(VRC_Pickup));
        }

        void Update()
        {
#if UNITY_EDITOR
            if (trigger != lastTrigger)
            {
                if (trigger)
                {
                    OnPickupUseDown();
                }
                else
                {
                    OnPickupUseUp();
                }

                lastTrigger = trigger;
            }
#endif

#if UNITY_EDITOR
            var held = true;
#else
            var held = pickup.IsHeld;
#endif

            if (held)
            {
                GameObject target = null;

                var org = transform.position;
                var dir = transform.rotation * Vector3.up;

                Vector3 point = org + dir * MAX_DISTANCE;
                float distance = MAX_DISTANCE;

                RaycastHit hitInfo;

                if (Physics.Raycast(org, dir, out hitInfo, MAX_DISTANCE, DEFAULT_LAYER | LASER_BUTTON_LAYER, QueryTriggerInteraction.Collide))
                {
                    var collider = hitInfo.collider;

                    if (collider != null)
                    {
                        var go = collider.gameObject;

                        if (go != null)
                        {
                            if (go.layer == LASER_BUTTON_LAYER_INDEX)
                            {
                                target = go;
                            }
                        }
                    }

                    point = hitInfo.point;
                    distance = hitInfo.distance;
                }

                if (dragTarget == null)
                {
                    if (target != null)
                    {
                        var targetDir = (target.transform.position - org).normalized;

                        if (Physics.Raycast(org, targetDir, out hitInfo, MAX_DISTANCE, LASER_BUTTON_LAYER, QueryTriggerInteraction.Collide))
                        {
                            point = hitInfo.point;
                        }
                    }

                    distance = Mathf.Max(0.0f, distance - 0.01f);

                    pointer.position = point;
                    laser.localScale = new Vector3(1.0f, distance, 1.0f);

                    if (target != lastTarget)
                    {
                        pickup.PlayHaptics();
                        lastTarget = target;
                    }
                }
                else
                {
                    distance = Mathf.Max(0.0f, distance - 0.01f);

                    pointer.position = point;
                    laser.localScale = new Vector3(1.0f, distance, 1.0f);

                    dragTarget.Drag(point);
                }

                pointer.gameObject.SetActive(true);
                laser.gameObject.SetActive(true);
            }
            else
            {
                var localPlayer = Networking.LocalPlayer;

                if (localPlayer != null)
                {
                    var vr = localPlayer.IsUserInVR();

                    handle.SetActive(!vr);

                    if (vr)
                    {
                        transform.position = localPlayer.GetBonePosition(HumanBodyBones.Head);
                    }
                }

                pointer.gameObject.SetActive(false);
                laser.gameObject.SetActive(false);

                OnPickupUseUp();
            }
        }

        public override void OnPickupUseDown()
        {
            if (lastTarget != null)
            {
                var pushButton = lastTarget.GetComponent<PushButton>();

                if (pushButton != null)
                {
                    pushButton.Push();
                }

                var indexedCollider = lastTarget.GetComponent<IndexedCollider>();

                if (indexedCollider != null)
                {
                    indexedCollider.Push();
                }

                var startCollider = lastTarget.GetComponent<StartCollider>();

                if (startCollider != null)
                {
                    dragTarget = startCollider;
                }
            }
        }

        public override void OnPickupUseUp()
        {
            if (dragTarget != null)
            {
                dragTarget.EndDrag();
                dragTarget = null;
            }
        }
    }
}
