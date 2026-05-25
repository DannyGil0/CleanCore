using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CleanCore.VRMenu
{
    /// <summary>
    /// Anima el cierre/apertura de los dedos de una mano "Ghostly" sin depender de
    /// XR Hand Tracking real. Lee dos InputActions (grip / trigger) y rota los huesos
    /// entre la pose abierta (cacheada al inicio) y una pose cerrada (puño).
    ///
    /// Convenciones de nombres de huesos esperadas (XRI Hands sample):
    ///   L_/R_ + (Thumb|Index|Middle|Ring|Little) + (Metacarpal|Proximal|Intermediate|Distal|Tip)
    /// </summary>
    [DisallowMultipleComponent]
    public class VRHandGripAnimator : MonoBehaviour
    {
        [Header("Input (grip cierra la mano, trigger cierra el indice)")]
        public InputActionReference gripValueAction;
        public InputActionReference triggerValueAction;

        [Header("Animacion")]
        [Tooltip("Velocidad de interpolacion al cerrar / abrir la mano (1 = instantaneo).")]
        [Range(1f, 30f)] public float lerpSpeed = 14f;

        [Tooltip("Angulo de cierre por articulacion (grados). Aplicado en el eje X local.")]
        [Range(0f, 110f)] public float closeAngleProximal = 55f;
        [Range(0f, 110f)] public float closeAngleIntermediate = 80f;
        [Range(0f, 110f)] public float closeAngleDistal = 60f;
        [Range(0f, 90f)] public float closeAngleThumb = 35f;

        [Header("Ejes de Rotacion (Gleechi usa Z, XRI usa X)")]
        public Vector3 fingerCurlAxis = new Vector3(0f, 0f, 1f);
        public Vector3 thumbCurlAxis = new Vector3(0f, 1f, 0f);

        [Tooltip("Si esta marcado, fuerza la mano cerrada (debug).")]
        public bool debugForceClose = false;

        private struct Joint
        {
            public Transform bone;
            public Quaternion openLocal;
            public Quaternion closedLocal;
            public bool isThumb;
            public bool isIndex;
        }

        private readonly List<Joint> _joints = new List<Joint>();
        private float _currentGrip = 0f;
        private float _currentTrigger = 0f;

        private void Awake()
        {
            CacheJoints();
        }

        private void OnEnable()
        {
            gripValueAction?.action.Enable();
            triggerValueAction?.action.Enable();
        }

        private void OnDisable()
        {
            gripValueAction?.action.Disable();
            triggerValueAction?.action.Disable();
        }

        private void CacheJoints()
        {
            _joints.Clear();
            var all = GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
            {
                string n = t.name;
                if (string.IsNullOrEmpty(n)) continue;

                // Soporta XRI Hands (L_IndexProximal) y VirtualGrasp Hands (IndexA_L)
                bool isThumb = n.Contains("Thumb");
                bool isIndex = n.Contains("Index");
                bool isMiddle = n.Contains("Middle");
                bool isRing = n.Contains("Ring");
                bool isPinky = n.Contains("Pinky") || n.Contains("Little");

                if (!isThumb && !isIndex && !isMiddle && !isRing && !isPinky) continue;

                bool isProximal = n.Contains("Proximal") || n.Contains("A_L") || n.Contains("A_R");
                bool isIntermediate = n.Contains("Intermediate") || n.Contains("B_L") || n.Contains("B_R");
                bool isDistal = n.Contains("Distal") || n.Contains("C_L") || n.Contains("C_R");

                if (!isProximal && !isIntermediate && !isDistal) continue;

                float angle;
                if (isThumb)
                {
                    angle = closeAngleThumb;
                }
                else if (isProximal)
                {
                    angle = closeAngleProximal;
                }
                else if (isIntermediate)
                {
                    angle = closeAngleIntermediate;
                }
                else
                {
                    angle = closeAngleDistal;
                }

                // Las manos de VirtualGrasp (Gleechi) usan el eje Z para flexionar los dedos,
                // mientras que las de XRI usan el eje X. Detectamos por el nombre.
                bool isGleechi = n.EndsWith("_L") || n.EndsWith("_R");
                Vector3 axis = isGleechi ? fingerCurlAxis : new Vector3(1f, 0f, 0f);

                // El pulgar de Gleechi rota diferente
                if (isGleechi && isThumb)
                {
                    axis = thumbCurlAxis; // Aproximacion para el pulgar
                }

                var joint = new Joint
                {
                    bone = t,
                    openLocal = t.localRotation,
                    closedLocal = t.localRotation * Quaternion.AngleAxis(angle, axis),
                    isThumb = isThumb,
                    isIndex = isIndex,
                };
                _joints.Add(joint);
            }
        }

        private void Update()
        {
            float gripTarget = ReadValue(gripValueAction);
            float triggerTarget = ReadValue(triggerValueAction);

            if (debugForceClose)
            {
                gripTarget = 1f;
                triggerTarget = 1f;
            }

            float t = Mathf.Clamp01(Time.deltaTime * lerpSpeed);
            _currentGrip = Mathf.Lerp(_currentGrip, gripTarget, t);
            _currentTrigger = Mathf.Lerp(_currentTrigger, triggerTarget, t);

            for (int i = 0; i < _joints.Count; i++)
            {
                var j = _joints[i];
                if (j.bone == null) continue;

                float amount;
                if (j.isIndex)
                {
                    // GESTO DE APUNTAR: El indice SOLO se cierra con el trigger.
                    // Asi, si aprietas el grip (para agarrar el mando), el indice queda estirado apuntando.
                    amount = _currentTrigger;
                }
                else if (j.isThumb)
                {
                    // El pulgar se cierra un poco con el grip y un poco con el trigger
                    amount = Mathf.Max(_currentGrip * 0.85f, _currentTrigger * 0.4f);
                }
                else
                {
                    // Los demas dedos se cierran con el grip
                    amount = _currentGrip;
                }

                j.bone.localRotation = Quaternion.Slerp(j.openLocal, j.closedLocal, amount);
            }
        }

        private static float ReadValue(InputActionReference reference)
        {
            if (reference == null || reference.action == null) return 0f;
            try
            {
                return Mathf.Clamp01(reference.action.ReadValue<float>());
            }
            catch
            {
                return reference.action.IsPressed() ? 1f : 0f;
            }
        }
    }
}
