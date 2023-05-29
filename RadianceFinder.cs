using UnityEngine;

namespace AbsRadAnyOrb {
    public class RadianceFinder : MonoBehaviour {
        private GameObject radiance;
        private bool found = false;

        private void Update() {
            if (radiance == null) {
                found = false;
                radiance = GameObject.Find("Absolute Radiance");
            }

            if (!found && radiance != null) {
                radiance.AddComponent<AnyOrb>();
                found = true;
            }
        }

        public void Unload() {
            if (radiance != null) {
                AnyOrb anyOrb = radiance.GetComponent<AnyOrb>();
                if (anyOrb != null) {
                    GameObject.Destroy(anyOrb);
                }
            }
        }
    }
}