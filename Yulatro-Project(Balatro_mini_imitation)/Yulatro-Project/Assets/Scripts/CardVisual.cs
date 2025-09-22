using System.Collections;
using Attributes;
using DG.Tweening;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.U2D.Animation;
using UnityEngine.UI;
using Utility;

public struct CardSpriteLabels
{
    public CardSpriteLabels(string backgroundLabel)
    {
        BackgroundLabel = backgroundLabel;
        NumberLabel = "None";
    }
    public CardSpriteLabels(string backgroundLabel, string numberLabel)
    {
        BackgroundLabel = backgroundLabel;
        NumberLabel = numberLabel;
    }
    public readonly string BackgroundLabel;
    // 플레잉카드 용
    public readonly string NumberLabel;
}

public class CardVisual : MonoBehaviour
{
    private bool _initalize = false;
    
    private Canvas _canvas;
    private Camera _mainCamera;

    [SerializeField, ReadOnly] private CardType cardType;
    
    [Header("Card")] 
    [ReadOnly] public CardController targetCard;
    [SerializeField] private CardEdition cardEdition;
    private Transform _cardTransform;
    private Vector3 _rotationDelta;
    private int _savedIndex;
    private Vector3 _movementDelta;

    [field: Header("Main Visual Parameters")]
    public bool EnableAnimations { get; private set; } = true;
    
    [Header("Resource Data")] 
    [SerializeField] private CardSpriteResources cardSpriteResources;

    [Header("Shadow Parameters")] 
    [SerializeField] private float shadowOffset = 15;
    
    [Header("Follow Parameters")] 
    [SerializeField] private float followSpeed = 30;
    
    [Header("Rotation Parameters")]
    [SerializeField] private float rotationAmount = 50;
    [SerializeField] private float rotationSpeed = 10;
    [SerializeField] private float autoTiltAmount = 20;
    [SerializeField] private float manualTiltAmount = 20;
    [SerializeField] private float tiltSpeed = 20;
    
    [Header("Scale Parameters")]
    [SerializeField] private bool scaleAnimations = true;
    [SerializeField] private float scaleOnHover = 1.15f;
    [SerializeField] private float scaleOnSelect = 1.25f;
    [SerializeField] private float scaleTransition = .15f;
    [SerializeField] private Ease scaleEase = Ease.OutBack;
    
    [Header("Select Parameters")]
    [SerializeField] private float selectPunchAmount = 20;
    
    [Header("Hover Parameters")]
    [SerializeField] private float hoverPunchAngle = 5;
    [SerializeField] private float hoverTransition = .15f;
    
    [Header("Swap Parameters")]
    [SerializeField] private bool swapAnimations = true;
    [SerializeField] private float swapRotationAngle = 30;
    [SerializeField] private float swapTransition = .15f;
    [SerializeField] private int swapVibrato = 5;

    #region References

    [Header("SpriteLibrary References")]
    [SerializeField] private SpriteLibrary backgroundSpriteLibrary;
    [SerializeField] private SpriteLibrary numberSpriteLibrary;
    [SerializeField] private SpriteLibrary symbolSpriteLibrary;
    
    [Header("SpriteResolver References")]
    [SerializeField] private SpriteResolver backgroundSpriteResolver;
    [SerializeField] private SpriteResolver numberSpriteResolver;
    [SerializeField] private SpriteResolver symbolSpriteResolver;
    
    [Header("SpriteRenderer References")]
    [SerializeField] private SpriteRenderer backgroundSpriteRenderer;
    [SerializeField] private SpriteRenderer numberSpriteRenderer;
    [SerializeField] private SpriteRenderer symbolSpriteRenderer;
    
    [Header("Image References")] 
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image numberImage;
    [SerializeField] private Image symbolImage;
    [SerializeField] private Image shadowImage;
    
    [Header("Colider References")]
    [SerializeField] private Collider2D _collider;
    
    private Material backgroundMaterial;
    private Material numberMaterial;
    private Material symbolMaterial;

    [Header("Transform References")] 
    private Vector2 _shadowDistance;
    private Canvas _shadowCanvas;
    [SerializeField] private Transform visualShadow;
    [SerializeField] private Transform shakeParent;
    [SerializeField] private Transform tiltParent;

    #endregion

    [Header("Curve")]
    private CurveParameters _curve;
    private float _curveYOffset;
    private float _curveRotationOffset;
    private Coroutine _pressCoroutine;
    private static readonly int Rotation = Shader.PropertyToID("_Rotation");

    private const string BackgroundCategoryName = "Background";
    private const string NumberCategoryName = "Number";
    private const string SymbolCategoryName = "Symbol";

    private const string RegularEditionEnumEntryName = "REGULAR";
    private const string FoilEditionEnumEntryName = "FOIL";
    private const string HolographicEditionEnumEntryName = "HOLOGRAPHIC";
    private const string PolychromeEditionEnumEntryName = "POLYCHROME";
    private const string NegativeEditionEnumEntryName = "NEGATIVE";

    public bool disableFollowTarget = false;

    private void Start()
    {
        _shadowDistance = visualShadow.localPosition;
    }

    // 플레잉 카드 제외
    public void Initialize(CardController target, CurveParameters curve, CardType newCardType, CardEdition edition, CardSpriteLabels cardSpriteLabels)
    {
        targetCard = target;
        _cardTransform = target.transform;
        _curve = curve;

        transform.position = _cardTransform.position;
        
        _mainCamera = Camera.main;
        _canvas = GetComponent<Canvas>();
        _shadowCanvas = visualShadow.GetComponent<Canvas>();
        
        // Add Event Listeners
        targetCard.selectEvent.AddListener(OnSelect);
        targetCard.beginDragEvent.AddListener(OnBeginDrag);
        targetCard.endDragEvent.AddListener(OnEndDrag);
        targetCard.pointerEnterEvent.AddListener(OnPointerEnter);
        targetCard.pointerExitEvent.AddListener(OnPointerExit);
        targetCard.pointerUpEvent.AddListener(OnPointerUp);
        targetCard.pointerDownEvent.AddListener(OnPointerDown);

        backgroundSpriteRenderer.enabled = false;
        numberSpriteRenderer.enabled = false;
        symbolSpriteRenderer.enabled = false;
        
        cardType = newCardType;
        
        // 이미지 컴포넌트 초기화
        backgroundImage.enabled = false;
        numberImage.enabled = false;
        symbolImage.enabled = false;
        shadowImage.enabled = false;
        
        backgroundImage.sprite = null;
        numberImage.sprite = null;
        symbolImage.sprite = null;
        
        SetSpriteLibrary();
        SetSprites(cardSpriteLabels);
        
        // Shader
        cardEdition = edition;
        SetupMaterialShader();
        SetEditionEnumKeywordEntry();
        
        _initalize = true;
    }

    private void Update()
    {
        if (!_initalize || !targetCard) return;
        
        HandPositioning();
        SmoothFollow();
        FollowRotation();
        
        if (EnableAnimations)
        {
            CardTilt();
        }

        // Shader
        SetMaterialRotationProperty();
        SetEditionEnumKeywordEntry();
    }
    
    float ClampAngle(float angle, float min, float max)
    {
        if (angle < -180f)
            angle += 360f;
        if (angle > 180f)
            angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }

    private void SetupMaterialShader()
    {
        backgroundMaterial = new Material(backgroundImage.material);
        backgroundImage.material = backgroundMaterial;
        
        numberMaterial = new Material(numberImage.material);
        numberImage.material = numberMaterial;
        
        symbolMaterial = new Material(symbolImage.material);
        symbolImage.material = symbolMaterial;
    }

    private void SetMaterialRotationProperty()
    {
        // Rotation
        Vector3 eulerAngles = tiltParent.localRotation.eulerAngles;
        float xAngle = eulerAngles.x;
        float yAngle = eulerAngles.y;
        
        // X축 각도가 -90~90도 범위 내에 있는지 확인합니다.
        xAngle = ClampAngle(xAngle, -90f, 90f);
        yAngle = ClampAngle(yAngle, -90f, 90);

        backgroundMaterial.SetVector(Rotation,
            new Vector2(xAngle.Remap(-20, 20, -.5f, .5f), yAngle.Remap(-20, 20, -.5f, .5f)));
        numberMaterial.SetVector(Rotation,
            new Vector2(xAngle.Remap(-20, 20, -.5f, .5f), yAngle.Remap(-20, 20, -.5f, .5f)));
        symbolMaterial.SetVector(Rotation,
            new Vector2(xAngle.Remap(-20, 20, -.5f, .5f), yAngle.Remap(-20, 20, -.5f, .5f)));
    }

    private void SetEditionEnumKeywordEntry()
    {
        // Edition Enum Keyword
        for (int i = 0; i < backgroundImage.material.enabledKeywords.Length; i++)
        {
            backgroundMaterial.DisableKeyword(backgroundImage.material.enabledKeywords[i]);
        }
        for (int i = 0; i < numberImage.material.enabledKeywords.Length; i++)
        {
            numberMaterial.DisableKeyword(numberImage.material.enabledKeywords[i]);
        }
        for (int i = 0; i < symbolImage.material.enabledKeywords.Length; i++)
        {
            symbolMaterial.DisableKeyword(symbolImage.material.enabledKeywords[i]);
        }
        
        switch (cardEdition)
        {
            case CardEdition.Regular:
                backgroundMaterial.EnableKeyword("_EDITION_" + RegularEditionEnumEntryName);
                numberImage.material.EnableKeyword("_EDITION_" + RegularEditionEnumEntryName);
                symbolImage.material.EnableKeyword("_EDITION_" + RegularEditionEnumEntryName);
                break;
            case CardEdition.Foil:
                backgroundMaterial.EnableKeyword("_EDITION_" + FoilEditionEnumEntryName);
                numberImage.material.EnableKeyword("_EDITION_" + FoilEditionEnumEntryName);
                symbolImage.material.EnableKeyword("_EDITION_" + FoilEditionEnumEntryName);
                break;
            case CardEdition.Holographic:
                backgroundMaterial.EnableKeyword("_EDITION_" + HolographicEditionEnumEntryName);
                numberImage.material.EnableKeyword("_EDITION_" + HolographicEditionEnumEntryName);
                symbolImage.material.EnableKeyword("_EDITION_" + HolographicEditionEnumEntryName);
                break;
            case CardEdition.Polychrome:
                backgroundMaterial.EnableKeyword("_EDITION_" + PolychromeEditionEnumEntryName);
                numberImage.material.EnableKeyword("_EDITION_" + PolychromeEditionEnumEntryName);
                symbolImage.material.EnableKeyword("_EDITION_" + PolychromeEditionEnumEntryName);
                break;
            case CardEdition.Negative:
                backgroundMaterial.EnableKeyword("_EDITION_" + NegativeEditionEnumEntryName);
                numberImage.material.EnableKeyword("_EDITION_" + NegativeEditionEnumEntryName);
                symbolImage.material.EnableKeyword("_EDITION_" + NegativeEditionEnumEntryName);
                break;
        }
    }

    private void SmoothFollow()
    {
        if (disableFollowTarget) return;
        
        Vector3 verticalOffset = (Vector3.up * (targetCard.IsDragging ? 0 : _curveYOffset));
        transform.position = Vector3.Lerp(transform.position, _cardTransform.position + verticalOffset,
            followSpeed * Time.deltaTime);
    }
    
    private void HandPositioning()
    {
        _curveYOffset = (_curve.positioning.Evaluate(targetCard.NormalizedPosition()) * _curve.positioningInfluence) * targetCard.SiblingAmount();
        // _curveYOffset = targetCard.SiblingAmount() < 5 ? 0 : _curveYOffset;
        _curveRotationOffset = _curve.rotation.Evaluate(targetCard.NormalizedPosition());
    }

    private void FollowRotation()
    {
        Vector3 movement = (transform.position - _cardTransform.position);
        _movementDelta = Vector3.Lerp(_movementDelta, movement, 25 * Time.deltaTime);
        Vector3 movementRotation = (targetCard.IsDragging ? _movementDelta : movement) * rotationAmount;
        _rotationDelta = Vector3.Lerp(_rotationDelta, movementRotation, rotationSpeed * Time.deltaTime);
        
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y,
            Mathf.Clamp(_rotationDelta.x, -60, 60));
    }

    private void CardTilt()
    {
        _savedIndex = targetCard.IsDragging ? _savedIndex : targetCard.ParentIndex();
        float sine = Mathf.Sin(Time.time + _savedIndex) * (targetCard.IsHovering ? .2f : 1);
        float cosine = Mathf.Cos(Time.time + _savedIndex) * (targetCard.IsHovering ? .2f : 1);


        var point = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        var offset = transform.position - point;
        
        float tiltX = targetCard.IsHovering ? ((offset.y * -1) * manualTiltAmount) : 0;
        float tiltY = targetCard.IsHovering ? ((offset.x) * manualTiltAmount) : 0;
        float tiltZ = targetCard.IsDragging
            ? tiltParent.eulerAngles.z
            : (_curveRotationOffset * Mathf.Min(_curve.maxRotation, _curve.rotationInfluence * targetCard.SiblingAmount()));

        float lerpX = Mathf.LerpAngle(tiltParent.eulerAngles.x, tiltX + (sine * autoTiltAmount),
            tiltSpeed * Time.deltaTime);
        float lerpY = Mathf.LerpAngle(tiltParent.eulerAngles.y, tiltY + (cosine * autoTiltAmount),
            tiltSpeed * Time.deltaTime);
        float lerpZ = Mathf.LerpAngle(tiltParent.eulerAngles.z, tiltZ, tiltSpeed / 2 * Time.deltaTime);

        tiltParent.eulerAngles = new Vector3(lerpX, lerpY, lerpZ);
    }
    
    public void Swap(float dir = 1)
    {
        if (!swapAnimations)
            return;

        DOTween.Kill(2, true);
        shakeParent.DOPunchRotation(Vector3.forward * (swapRotationAngle * dir), swapTransition, swapVibrato, 1).SetId(3);
    }

    private void UpdateShadowPosition()
    {
        visualShadow.localPosition += (-Vector3.up * shadowOffset);
        visualShadow.localPosition = _shadowDistance;
    }

    private void SetSpriteLibrary()
    {
        // Add Sprite Library
        switch (cardType)
        {
            case CardType.PlayingCard:
                backgroundImage.enabled = true;
                numberImage.enabled = true;
                symbolImage.enabled = true;
                SetSpriteLibrary(cardSpriteResources.PlayingCardSpriteLibrary);
                break;
            case CardType.JokerCard:
                backgroundImage.enabled = true;
                numberImage.enabled = false;
                symbolImage.enabled = false;
                SetSpriteLibrary(cardSpriteResources.JokerCardSpriteLibrary);
                break;
            case CardType.TarotCard:
                backgroundImage.enabled = true;
                numberImage.enabled = false;
                symbolImage.enabled = false;
                SetSpriteLibrary(cardSpriteResources.TarotCardSpriteLibrary);
                break;
            case CardType.PlanetCard:
                backgroundImage.enabled = true;
                numberImage.enabled = false;
                symbolImage.enabled = false;
                SetSpriteLibrary(cardSpriteResources.PlanetCardSpriteLibrary);
                break;
            default:
                break;
        }
    }

    private void SetSpriteLibrary(SpriteLibraryAsset spriteLibraryAsset)
    {
        backgroundSpriteLibrary.spriteLibraryAsset = spriteLibraryAsset;
        numberSpriteLibrary.spriteLibraryAsset = spriteLibraryAsset;
        symbolSpriteLibrary.spriteLibraryAsset = spriteLibraryAsset;
    }

    public void SetSprites(CardSpriteLabels cardSpriteLabels)
    {
        switch (cardType)
        {
            case CardType.PlayingCard:
                backgroundSpriteResolver.SetCategoryAndLabel(BackgroundCategoryName, cardSpriteLabels.BackgroundLabel);
                if (cardSpriteLabels.NumberLabel == "None")
                {
                    numberImage.enabled = false;
                    symbolImage.enabled = false;
                }
                else
                {
                    numberSpriteResolver.SetCategoryAndLabel(NumberCategoryName, cardSpriteLabels.NumberLabel);
                    symbolSpriteResolver.SetCategoryAndLabel(SymbolCategoryName, cardSpriteLabels.NumberLabel);
                }
                break;
            case CardType.JokerCard:
                backgroundSpriteResolver.SetCategoryAndLabel(BackgroundCategoryName, cardSpriteLabels.BackgroundLabel);
                break;
            case CardType.TarotCard:
                backgroundSpriteResolver.SetCategoryAndLabel(BackgroundCategoryName, cardSpriteLabels.BackgroundLabel);
                break;
            case CardType.PlanetCard:
                backgroundSpriteResolver.SetCategoryAndLabel(BackgroundCategoryName, cardSpriteLabels.BackgroundLabel);
                break;
            default:
                break;
        }

        SetImageSprites();
    }

    private void SetImageSprites()
    {
        if (cardType == CardType.PlayingCard)
        {
            backgroundImage.sprite  = backgroundSpriteRenderer.sprite;
            numberImage.sprite      = numberSpriteRenderer.sprite;
            symbolImage.sprite      = symbolSpriteRenderer.sprite;
        }
        else
        {
            backgroundImage.sprite  = backgroundSpriteRenderer.sprite;
            numberImage.sprite      = null;
            symbolImage.sprite      = null;
        }
    }

    public void UpdateSiblingIndex()
    {
        transform.SetSiblingIndex(targetCard.transform.parent.GetSiblingIndex());
    }
    
    // Events -----
    
    private void OnSelect(CardController card, bool state)
    {
        DOTween.Kill(2, true);
        float dir = state ? 1 : 0;
        shakeParent.DOPunchPosition(shakeParent.up * selectPunchAmount * dir, scaleTransition, 10, 1);
        shakeParent.DOPunchRotation(Vector3.forward * (hoverPunchAngle/2), hoverTransition, 20, 1).SetId(2);

        if(scaleAnimations)
            transform.DOScale(scaleOnHover, scaleTransition).SetEase(scaleEase);

    }
    
    private void OnBeginDrag(CardController cardController)
    {
        if(scaleAnimations)
            transform.DOScale(scaleOnSelect, scaleTransition).SetEase(scaleEase);

        _canvas.overrideSorting = true;
    }

    private void OnEndDrag(CardController cardController)
    {
        _canvas.overrideSorting = false;
        transform.DOScale(1, scaleTransition).SetEase(scaleEase);
    }

    private void OnPointerEnter(CardController cardController)
    {
        if(scaleAnimations)
            transform.DOScale(scaleOnHover, scaleTransition).SetEase(scaleEase);

        DOTween.Kill(2, true);
        shakeParent.DOPunchRotation(Vector3.forward * hoverPunchAngle, hoverTransition, 20, 1).SetId(2);
    }

    private void OnPointerExit(CardController cardController)
    {
        if (!targetCard.WasDragged)
            transform.DOScale(1, scaleTransition).SetEase(scaleEase);
    }

    private void OnPointerUp(CardController cardController, bool isLongPress)
    {
        if(scaleAnimations)
            transform.DOScale(isLongPress ? scaleOnHover : scaleOnSelect, scaleTransition).SetEase(scaleEase);
        _canvas.overrideSorting = false;
        
        _shadowCanvas.overrideSorting = true;
        _shadowCanvas.sortingOrder = -20;
    }

    private void OnPointerDown(CardController cardController)
    {
        if(scaleAnimations)
            transform.DOScale(scaleOnSelect, scaleTransition).SetEase(scaleEase);
        
        _canvas.overrideSorting = true;
        _canvas.sortingOrder = 2;
        
        _shadowCanvas.overrideSorting = true;
        _shadowCanvas.sortingOrder = 1;
    }
}
