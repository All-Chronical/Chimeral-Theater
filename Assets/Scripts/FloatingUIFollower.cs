using UnityEngine;

namespace ChimeralTheater.UI
{
    /// <summary>
    /// Makes a floating panel follow an anchor point around: the panel eases toward the anchor
    /// plus an offset expressed in the viewer's space, then turns to face the viewer. Any panel
    /// can borrow this; the dashboard follows the left hand and a recording HUD would follow
    /// the head. The anchor and viewer are supplied by the caller each frame.
    /// </summary>
    public class FloatingUIFollower : MonoBehaviour
    {
        [SerializeField] private Transform viewer;
        [SerializeField] private Vector3 anchorOffset = new Vector3(0f, 0.15f, 0.25f);
        [SerializeField] private float followSharpness = 8f;

        public Transform Viewer { get => viewer; set => viewer = value; }
        public Vector3 AnchorOffset { get => anchorOffset; set => anchorOffset = value; }
        public float FollowSharpness { get => followSharpness; set => followSharpness = value; }

        /// <summary>Eases the panel toward the anchor plus its offset and faces it to the viewer.</summary>
        public void Follow(Vector3 anchor, float dt)
        {
            Vector3 target = CalculateTargetPosition(anchor);
            transform.position = Vector3.Lerp(transform.position, target, dt * followSharpness);
            FaceViewer();
        }

        /// <summary>Follows a transform's position using the current frame's delta time.</summary>
        public void Follow(Transform anchor)
        {
            if (anchor == null)
                return;

            Follow(anchor.position, Time.deltaTime);
        }

        /// <summary>Places the panel at the anchor immediately, skipping the ease.</summary>
        public void Snap(Vector3 anchor)
        {
            transform.position = CalculateTargetPosition(anchor);
            FaceViewer();
        }

        private Vector3 CalculateTargetPosition(Vector3 anchor)
        {
            if (viewer == null)
                return anchor + anchorOffset;

            Vector3 forwardOffset = viewer.forward * anchorOffset.z;
            Vector3 upwardOffset = Vector3.up * anchorOffset.y;
            Vector3 rightOffset = viewer.right * anchorOffset.x;
            return anchor + forwardOffset + upwardOffset + rightOffset;
        }

        private void FaceViewer()
        {
            if (viewer == null)
                return;

            Vector3 toPanel = transform.position - viewer.position;
            if (toPanel.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(toPanel, Vector3.up);
        }
    }
}
