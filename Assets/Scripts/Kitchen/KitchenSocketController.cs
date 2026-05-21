using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace CleanCore.Kitchen
{
    [RequireComponent(typeof(XRSocketInteractor))]
    public class KitchenSocketController : MonoBehaviour
    {
        [SerializeField] private KitchenItemInteractable _trackedItem;
        [SerializeField] private GameObject _guideVisual;
        [SerializeField] private string _objectName;

        public static event System.Action<string, bool> OnPlacementChanged;

        public string ObjectName => _objectName;
        public bool IsPlaced { get; private set; }

        private XRSocketInteractor _socket;

        private void Awake()
        {
            _socket = GetComponent<XRSocketInteractor>();
        }

        private void OnEnable()
        {
            _socket.selectEntered.AddListener(OnSnapEnter);
            _socket.selectExited.AddListener(OnSnapExit);
        }

        private void OnDisable()
        {
            _socket.selectEntered.RemoveListener(OnSnapEnter);
            _socket.selectExited.RemoveListener(OnSnapExit);
        }

        private void OnSnapEnter(SelectEnterEventArgs args)
        {
            IsPlaced = true;
            if (_guideVisual != null)
                _guideVisual.SetActive(false);
            if (_trackedItem != null)
                _trackedItem.SetPlaced(true);
            OnPlacementChanged?.Invoke(_objectName, true);
        }

        private void OnSnapExit(SelectExitEventArgs args)
        {
            IsPlaced = false;
            if (_guideVisual != null)
                _guideVisual.SetActive(true);
            if (_trackedItem != null)
                _trackedItem.SetPlaced(false);
            OnPlacementChanged?.Invoke(_objectName, false);
        }
    }
}
