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
            if (_guideVisual != null)
                _guideVisual.SetActive(false);
            if (_trackedItem != null)
                _trackedItem.SetPlaced(true);
        }

        private void OnSnapExit(SelectExitEventArgs args)
        {
            if (_guideVisual != null)
                _guideVisual.SetActive(true);
            if (_trackedItem != null)
                _trackedItem.SetPlaced(false);
        }
    }
}
