using UnityEngine;

namespace ExpandedChestUI.Scripts.Components
{
    public class ChangeRuntimeMaterial : MonoBehaviour
    {
        public bool Apply(Material material)
        {
            if(material != null)
            {
                if (gameObject.TryGetComponent(out SpriteRenderer spriteRenderer))
                {
                    spriteRenderer.sharedMaterial = material;
                }

                if (gameObject.TryGetComponent(out ParticleSystemRenderer particleSystemRenderer))
                {
                    particleSystemRenderer.sharedMaterial = material;
                }
            }
            else
            {
                ExpandedChestUI.Log.LogInfo($"Error applying null material!");
            }

            return true;
        }
    }
}