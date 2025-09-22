using UnityEngine;

public class CardMovement : MonoBehaviour
{
    private Camera _mainCamera;
    // 드래그를 시작 했을때 마우스를 기준으로 움직이기 위한 보정 값. (마우스가 항상 중앙에 위치하지 않도록.)
    private Vector3 _targetDirectionOffset;
    private CardController _cardController;

    [Header("Movement")]
    public bool disableClampPosition;
    [SerializeField] private float moveSpeedLimit = 50.0f;
    [Tooltip("선택 되었을 때 위로 움직일 거리 (강조를 위해)")]
    [field: SerializeField] public float SelectionOffset { get; private set; } = 50.0f;

    public void Initialize(CardController cardController, Camera mainCamera)
    {
        _cardController = cardController;
        _mainCamera = mainCamera;
        
        RegisterEvents(cardController);
    }

    private void RegisterEvents(CardController cardController)
    {
        cardController.beginDragEvent.AddListener(OnBeginDrag);
        cardController.selectEvent.AddListener(OnSelect);
    }

    private void Update()
    {
        if(disableClampPosition == false)
            ClampPosition();
        UpdateCardDragPosition();
    }

    private void UpdateCardDragPosition()
    {
        if (!_cardController.IsDragging) return;
        
        Vector2 targetPosition = _mainCamera.ScreenToWorldPoint(Input.mousePosition) - _targetDirectionOffset;
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
        Vector2 velocity = direction * Mathf.Min(moveSpeedLimit,
            Vector2.Distance(transform.position, targetPosition) / Time.deltaTime);
        transform.Translate(velocity * Time.deltaTime);
    }
    
    // 카드가 스크린 밖으로 나가지 않도록 한다.
    private void ClampPosition()
    {
        Vector2 screenBounds =
            _mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height,
                _mainCamera.transform.position.z));
        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, -screenBounds.x, screenBounds.x);
        clampedPosition.y = Mathf.Clamp(clampedPosition.y, -screenBounds.y, screenBounds.y);

        transform.position = new Vector3(clampedPosition.x, clampedPosition.y, 0);
    }

    private void SelectMovement(bool isSelected)
    {
        if (isSelected)
            transform.localPosition += (transform.up * SelectionOffset);
        else
            transform.localPosition = Vector3.zero;
    }

    // Events -----

    private void OnBeginDrag(CardController cardController)
    {
        Vector2 mousePosition = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        _targetDirectionOffset = mousePosition - (Vector2)transform.position;
    }

    private void OnSelect(CardController cardController, bool isSelected)
    {
        SelectMovement(isSelected);
    }
}
