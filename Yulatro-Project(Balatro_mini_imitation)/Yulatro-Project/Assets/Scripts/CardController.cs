using System.Collections;
using Attributes;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Utility;

public class CardController : MonoBehaviour,
    IDragHandler, IBeginDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler,IPointerUpHandler, IPointerDownHandler
{

    public string[] cardNamesList = { "Spade_2", "Spade_3", "Spade_4", "Spade_5", "Spade_6", "Spade_7", "Spade_8", "Spade_9", "Spade_10", "Spade_J", "Spade_Q", "Spade_K" , "Spade_A", 
                                  "Heart_2", "Heart_3", "Heart_4", "Heart_5", "Heart_6", "Heart_7", "Heart_8", "Heart_9", "Heart_10", "Heart_J", "Heart_Q", "Heart_K" , "Heart_A",
                                  "Club_2", "Club_3", "Club_4", "Club_5", "Club_6", "Club_7", "Club_8", "Club_9", "Club_10", "Club_J", "Club_Q", "Club_K" , "Club_A",
                                  "Diamond_2", "Diamond_3", "Diamond_4", "Diamond_5", "Diamond_6", "Diamond_7", "Diamond_8", "Diamond_9", "Diamond_10", "Diamond_J", "Diamond_Q", "Diamond_K", "Diamond_A" };

    private Canvas  _canvas;
    private Camera  _mainCamera;
    private Image   _imageComponent;
    private VisualCardsHandler _visualCardsHandler;
    public int point;
    public float multipoint;
    public bool IsOnPlay = false;
    public string Suit;

    [Header("Sub Component References")] 
    public CardMovement cardMovement;
    
    [Header("Visuals")] 
    public CurveParameters curve;
    [SerializeField] private bool instantiateVisual = false;
    [SerializeField] private GameObject cardVisualPrefab;
    public CardVisual cardVisual; 

    [field: Header("Selection")]
    [field: SerializeField] public bool ActiveDrag { get; private set; } = true;
    [field: SerializeField] public bool ActiveSelect { get; private set; } = true;
    [field: SerializeField, ReadOnly] public bool IsSelected { get; private set; } = false;
    private float _pointerDownTime;
    private float _pointerUpTime;
    
    [field: Header("States")]
    [field: SerializeField, ReadOnly] public bool IsHovering { get; private set; } = false;
    [field: SerializeField, ReadOnly] public bool IsDragging { get; private set; } = false;
    public bool WasDragged { get; private set; } = false;

    [Header("Events")] 
    [HideInInspector] public UnityEvent<CardController>         pointerEnterEvent;
    [HideInInspector] public UnityEvent<CardController>         pointerExitEvent;
    [HideInInspector] public UnityEvent<CardController, bool>   pointerUpEvent;
    [HideInInspector] public UnityEvent<CardController>         pointerDownEvent;
    [HideInInspector] public UnityEvent<CardController>         beginDragEvent;
    [HideInInspector] public UnityEvent<CardController>         endDragEvent;
    [HideInInspector] public UnityEvent<CardController, bool>   selectEvent;


    private void Start()
    {
        _canvas = GetComponentInParent<Canvas>();
        _mainCamera = Camera.main;
        _imageComponent = GetComponent<Image>();
        
        cardMovement.Initialize(this, _mainCamera);

        if (instantiateVisual == false)
            return;

        initCard();
    }
    
    private void initCard()
    {
        // CardVisual Setup

        _visualCardsHandler = FindObjectOfType<VisualCardsHandler>();
        cardVisual =
            Instantiate(cardVisualPrefab, _visualCardsHandler ? _visualCardsHandler.transform : _canvas.transform)
                .GetComponent<CardVisual>();

        // 랜덤 카드 선택
        int randomIndex = Random.Range(0, cardNamesList.Length);
        string randomCard = cardNamesList[randomIndex];

        // 문양과 숫자 분리
        string[] parts = randomCard.Split('_');
        if (parts.Length < 2)
        {
            Debug.LogError($"Invalid card format: {randomCard}");
            return;
        }

        Suit = parts[0]; // 문양 설정 (Spade, Heart, Club, Diamond)
        string rank = parts[1]; // 숫자 또는 그림 카드 (2~10, J, Q, K, A)

        // 점수(point) 계산
        if (int.TryParse(rank, out int number))
        {
            point = number; // 숫자 카드의 점수는 숫자 그대로
        }
        else
        {
            switch (rank)
            {
                case "J": point = 11; break;
                case "Q": point = 12; break;
                case "K": point = 13; break;
                case "A": point = 14; break;
                default:
                    Debug.LogError($"Invalid rank: {rank}");
                    return;
            }
        }

        // 카드 타입 (CardEdition) 설정
        CardEdition randomEdition = (CardEdition)Random.Range(0, 5);
        switch (randomEdition)
        {
            case CardEdition.Regular:
                multipoint = 1.0f;
                break;
            case CardEdition.Foil:
                multipoint = 1.5f;
                break;
            case CardEdition.Holographic:
                multipoint = 2.0f;
                break;
            case CardEdition.Polychrome:
                multipoint = 2.5f;
                break;
            case CardEdition.Negative:
                multipoint = 3.0f;
                break;
            default:
                Debug.LogWarning($"Unknown CardEdition: {randomEdition}");
                multipoint = 1.0f;
                break;
        }

        // 카드 시각적 정보 초기화
        cardVisual.Initialize(
            this,
            curve,
            CardType.PlayingCard,
            randomEdition, // 랜덤으로 결정된 카드 타입 전달
            new CardSpriteLabels("BaseCard", randomCard)
        );
    }

    // Events -----
    
    // 쓰지 않는데 왜 구현했나 의문이 든다면 나도 모른다.
    // 왠진 모르겠지만 IDragHandler 인터페이스를 포함하지 않으면 BeginDrag, EndDrag 또한 호출되지 않는다.
    public void OnDrag(PointerEventData eventData)
    {
        
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (ActiveDrag == false)
            return;
        if (IsOnPlay)
            return;

        beginDragEvent.Invoke(this);
        IsDragging = true;
        
        // 드래그 중에는 사용자 입력을 받지 않는다.
        _canvas.GetComponent<GraphicRaycaster>().enabled = false;
        _imageComponent.raycastTarget = false;

        WasDragged = true;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ActiveDrag == false)
            return;
        if (IsOnPlay)
            return;

        endDragEvent.Invoke(this);
        IsDragging = false;
        
        // 사용자 입력을 다시 받는다.
        _canvas.GetComponent<GraphicRaycaster>().enabled = true;
        _imageComponent.raycastTarget = true;
        
        StartCoroutine(FrameWait());

        IEnumerator FrameWait()
        {
            yield return new WaitForEndOfFrame();
            WasDragged = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsOnPlay)
            return;
        pointerEnterEvent.Invoke(this);
        IsHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (IsOnPlay)
            return;
        pointerExitEvent.Invoke(this);
        IsHovering = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (IsOnPlay)
            return;
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        _pointerUpTime = Time.time;

        bool isLongPress = _pointerUpTime - _pointerDownTime > .2f;
        pointerUpEvent.Invoke(this, isLongPress);
        
        // 길게 누르기 이거나 이 프레임에 드래그가 끝났으면 Select 하지 않는다.
        if (isLongPress)
            return;
        if (WasDragged)
            return;

        Select();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsOnPlay)
            return;
        if (eventData.button != PointerEventData.InputButton.Left)
            return;
        
        pointerDownEvent.Invoke(this);
        _pointerDownTime = Time.time;
    }

    public bool Select(bool flag)
    {
        if (IsOnPlay)
            return false;
        if (ActiveSelect == false)
            return IsSelected;
        if (IsSelected == flag)
            return IsSelected;

        IsSelected = flag;
        selectEvent.Invoke(this, IsSelected);
        return IsSelected;
    }

    public bool Select()
    {
        if (IsOnPlay)
            return false;
        if (ActiveSelect == false)
            return IsSelected;

        IsSelected = !IsSelected;

        // 선택 모션인듯
        selectEvent.Invoke(this, IsSelected);
        return IsSelected;
    }
    
    public int SiblingAmount()
    {
        return transform.parent.CompareTag("Slot") ? transform.parent.parent.childCount - 1 : 0;
    }
    
    public int ParentIndex()
    {
        return transform.parent.CompareTag("Slot") ? transform.parent.GetSiblingIndex() : 0;
    }
    
    public float NormalizedPosition()
    {
        return transform.parent.CompareTag("Slot")
            ? ((float)ParentIndex()).Remap(0, (float)(transform.parent.parent.childCount - 1), 0, 1)
            : 0;
    }
    
    private void OnDestroy()
    {
        if(cardVisual != null)
            Destroy(cardVisual.gameObject);
    }
}




