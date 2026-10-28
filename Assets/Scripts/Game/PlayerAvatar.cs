using UnityEngine;

namespace Deliverable3
{
    public class PlayerAvatar : MonoBehaviour
    {
        [SerializeField] Renderer body;
        [SerializeField] Renderer gun;
        [SerializeField] float crouchGunHeight = 0.6f;

        public byte LastShots { get; set; }
        public Vector3 GunPosition { get { return gun.transform.position; } }

        Vector3 m_bodyPosition;
        Vector3 m_bodyScale;
        Vector3 m_gunPosition;
        bool m_crouching;

        void Awake()
        {
            m_bodyPosition = body.transform.localPosition;
            m_bodyScale = body.transform.localScale;
            m_gunPosition = gun.transform.localPosition;
        }

        public void SetColors(Color bodyColor, Color gunColor)
        {
            body.material.color = bodyColor;
            gun.material.color = gunColor;
        }

        public void SetCrouching(bool crouching)
        {
            if (m_crouching == crouching) return;
            m_crouching = crouching;
            body.transform.localScale = crouching ? new Vector3(m_bodyScale.x, m_bodyScale.y * 0.5f, m_bodyScale.z) : m_bodyScale;
            body.transform.localPosition = crouching ? m_bodyPosition * 0.5f : m_bodyPosition;
            gun.transform.localPosition = crouching ? new Vector3(m_gunPosition.x, crouchGunHeight, m_gunPosition.z) : m_gunPosition;
        }
    }
}
