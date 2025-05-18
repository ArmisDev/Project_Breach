namespace NL
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class GrenadeBase : MonoBehaviour
    {
        [SerializeField] private float explosionDelay = 3;
        public MeshRenderer rend;
        public Collider coll;
        public Rigidbody rb;
        [HideInInspector] public bool isExploded = false;
        [HideInInspector] public float timeSinceThrow = 0;

        private WaitForSeconds waitForExplosion => new WaitForSeconds(explosionDelay);

        private IEnumerator Start()
        {
            yield return waitForExplosion;

            Destroy(gameObject);
        }
    }
}
