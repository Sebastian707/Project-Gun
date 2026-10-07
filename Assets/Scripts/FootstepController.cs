using System;
using FMOD.Studio;
using FMODUnity;
using Unity.VisualScripting;
using UnityEngine;

namespace StarterAssets
{
    public class FootstepController : MonoBehaviour
    {
        [Serializable]
        public struct MaterialSoundEntry
        {
            [Tooltip("Must exactly match the PhysicMaterial asset name (case-sensitive).")]
            public string MaterialName;

            [Tooltip("FMOD event to fire on this surface.")]
            public EventReference FootstepEvent;
        }

        [Header("References")]
        [Tooltip("The transform that moves horizontally. For XR, this is usually the XR Origin.")]
        public Transform PlayerBody;

        [Header("Footstep Distance")]
        [Tooltip("Horizontal distance between footstep sounds.")]
        public float FootstepStride = 2.2f;

        [Header("Raycast")]
        [Tooltip("How far below the player origin to cast when sampling the surface.")]
        public float RayLength = 1.5f;

        [Tooltip("Layers that can be detected as footsteps surfaces.")]
        public LayerMask GroundLayers;

        [Header("FMOD Sounds")]
        [Tooltip("Fallback event when no material entry matches.")]
        public EventReference DefaultFootstep;

        [Tooltip("Per-material overrides. Entries are checked in order; first match wins.")]
        public MaterialSoundEntry[] MaterialSounds;

        private CharacterController _controller;

        private Vector3 _lastPosition;
        private float _strideAccumulator;

        private void Start()
        {
            _controller = GetComponent<CharacterController>();

            if (PlayerBody == null)
                PlayerBody = transform;

            _lastPosition = PlayerBody.position;
        }

        private void Update()
        {
            HandleFootsteps();
        }

        private void HandleFootsteps()
        {
            // Don't play footsteps while airborne.
            if (_controller != null && !_controller.isGrounded)
            {
                _lastPosition = PlayerBody.position;
                return;
            }

            Vector3 currentPosition = PlayerBody.position;

            // Only measure horizontal movement.
            Vector3 delta = currentPosition - _lastPosition;
            delta.y = 0f;

            float moved = delta.magnitude;

            _lastPosition = currentPosition;

            if (moved < 0.001f)
                return;

            _strideAccumulator += moved;

            if (_strideAccumulator >= FootstepStride)
            {
                _strideAccumulator -= FootstepStride;
                PlayFootstep();
            }
        }

        private void PlayFootstep()
        {
            string materialName;
            EventReference eventRef = ResolveSurfaceEvent(out materialName);

            if (eventRef.IsNull)
                return;

            EventInstance instance = RuntimeManager.CreateInstance(eventRef);

            instance.set3DAttributes(
                RuntimeUtils.To3DAttributes(PlayerBody.position)
            );

            instance.start();
            instance.release();
        }

        private EventReference ResolveSurfaceEvent(out string matchedMaterialName)
        {
            matchedMaterialName = string.Empty;

            Vector3 origin = PlayerBody.position + Vector3.up * 0.1f;

            if (Physics.Raycast(
                    origin,
                    Vector3.down,
                    out RaycastHit hit,
                    RayLength,
                    GroundLayers,
                    QueryTriggerInteraction.Ignore))
            {
                PhysicsMaterial mat = hit.collider.sharedMaterial;

                if (mat != null)
                {
                    matchedMaterialName = mat.name;
                    return FindEventForMaterial(mat.name);
                }
            }

            return DefaultFootstep;
        }

        private EventReference FindEventForMaterial(string matName)
        {
            if (MaterialSounds == null)
                return DefaultFootstep;

            foreach (MaterialSoundEntry entry in MaterialSounds)
            {
                if (string.Equals(
                        entry.MaterialName,
                        matName,
                        StringComparison.Ordinal))
                {
                    return entry.FootstepEvent;
                }
            }

            return DefaultFootstep;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;

            Transform body = PlayerBody != null
                ? PlayerBody
                : transform;

            Vector3 origin = body.position + Vector3.up * 0.1f;

            Gizmos.DrawLine(
                origin,
                origin + Vector3.down * RayLength
            );
        }
    }
}
