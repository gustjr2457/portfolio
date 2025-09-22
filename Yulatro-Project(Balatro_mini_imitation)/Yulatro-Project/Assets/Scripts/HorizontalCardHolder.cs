using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using Attributes;
using DG.Tweening;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class HorizontalCardHolder : MonoBehaviour
{
    [SerializeField, ReadOnly] private CardController selectedCard;

    [SerializeField] private int maxCardLimit;
    
    [Header("Component References")] 
    [SerializeField] private PlayerInput playerInput;
    
    [Header("Curve")]
    [SerializeField] private CurveParameters curve;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private GameObject cardPrefab;
    private RectTransform _rectTransform;

    [Header("Debug Spawn Settings")] 
    [SerializeField] private Transform defaultSpawnPoint;
    [SerializeField] private int cardsToSpawn = 8;
    public List<CardController> cards;
    public List<CardController> playingCards;
    public HorizontalCardHolder drawCardSession;
    public resultManager rsM;

    private bool _isCrossing = false;
    private bool _isPlaying = false;
    [SerializeField] private bool tweenCardReturn = true;
    
    public Text scoreText;
    public Text plusText;
    public Text multiText;
    public Text JbText;
    public Text cardScoreText;
    public int nowScore = 0;

    public enum HandRanking
    {
        None,
        HighCard,
        OnePair,
        TwoPair,
        ThreeOfAKind,
        Straight,
        Flush,
        FullHouse,
        FourOfAKind,
        StraightFlush,
        RoyalFlush
    }

    public HandRanking CheckHandRanking(List<CardController> cards)
    {
        // 숫자별 그룹화 (족보 확인용)
        var rankGroups = cards.GroupBy(card => card.point).ToList(); // 숫자별 그룹
                                                                     // 문양별 그룹화 (플러쉬 확인용)
        var suitGroups = cards.GroupBy(card => card.Suit).ToList(); // 문양별 그룹

        // 플러쉬와 스트레이트를 미리 계산
        bool isFlush = suitGroups.Any(group => group.Count() >= 5);
        bool isStraight = IsStraight(cards);

        // 족보 확인 (우선순위: 높은 족보부터)
        if (isStraight && isFlush)
        {
            // 스트레이트 플러쉬인지 확인 (로열 플러쉬 포함)
            int maxPoint = cards.Max(card => card.point);
            if (maxPoint == 14 && cards.Select(card => card.point).Distinct().OrderBy(x => x).Take(5).SequenceEqual(new[] { 10, 11, 12, 13, 14 }))
            {
                return HandRanking.RoyalFlush; // 로열 플러쉬
            }
            return HandRanking.StraightFlush; // 스트레이트 플러쉬
        }

        if (rankGroups.Any(group => group.Count() == 4))
            return HandRanking.FourOfAKind; // 포카드

        if (rankGroups.Any(group => group.Count() == 3) && rankGroups.Any(group => group.Count() == 2))
            return HandRanking.FullHouse; // 풀하우스

        if (isFlush)
            return HandRanking.Flush; // 플러쉬

        if (isStraight)
            return HandRanking.Straight; // 스트레이트

        if (rankGroups.Any(group => group.Count() == 3))
            return HandRanking.ThreeOfAKind; // 쓰리카드

        if (rankGroups.Count(group => group.Count() == 2) == 2)
            return HandRanking.TwoPair; // 투페어

        if (rankGroups.Any(group => group.Count() == 2))
            return HandRanking.OnePair; // 원페어

        return HandRanking.HighCard; // 아무 족보도 없는 경우
    }


    private bool IsStraight(List<CardController> cards)
    {
        var sortedCards = cards.OrderBy(card => card.point).ToList();
        if (sortedCards.Count != 5) return false;
        bool isNormalStraight = sortedCards.Zip(sortedCards.Skip(1), (a, b) => b.point - a.point).All(diff => diff == 1);

        // A, 2, 3, 4, 5 처리
        bool isAceLowStraight = sortedCards[0].point == 2 &&
                                sortedCards[1].point == 3 &&
                                sortedCards[2].point == 4 &&
                                sortedCards[3].point == 5 &&
                                sortedCards[4].point == 14;

        return isNormalStraight || isAceLowStraight;
    }


    private void OnEnable()
    {
        if(playerInput)
            playerInput.actions["Deselect"].performed += OnDeselectInput;
        else
            Debug.LogWarning("HorizontalCardHolder: playerInput이 null입니다. 입력이 무시될 수 있습니다.");
    }

    private void OnDisable()
    {
        if(playerInput)
            playerInput.actions["Deselect"].performed -= OnDeselectInput;
    }

    private void Start()
    {
        for (int i = 0; i < cardsToSpawn; i++)
        {
            var slot = Instantiate(slotPrefab, transform);
            var card = Instantiate(cardPrefab, slot.transform);
        }

        scoreText.text = "";
        plusText.text = "";
        multiText.text = "";
        JbText.text = "";
        cardScoreText.text = "";

        _rectTransform = GetComponent<RectTransform>();
        cards = GetComponentsInChildren<CardController>().ToList();

        int cardCount = 0;

        foreach (var cardController in cards)
        {
            cardController.beginDragEvent.AddListener(OnCardBeginDrag);
            cardController.endDragEvent.AddListener(OnCardEndDrag);
            cardController.name = cardCount.ToString();
            cardController.curve = curve;
            cardCount++;
        }

        StartCoroutine(Frame());
        return;
        
        IEnumerator Frame()
        {
            yield return new WaitForEndOfFrame();
            foreach (var cardController in cards)
            {
                if (cardController.cardVisual)
                    cardController.cardVisual.UpdateSiblingIndex();
            }
        }
    }

    public void DeselectAllCard()
    {
        foreach (var cardController in cards)
        {
            cardController.Select(false);
        }
        foreach (var playCard in playingCards)
        {
            playingCards.Remove(playCard);
        }
    }

    
    public void AddCard(GameObject prefab)
    {
        // TODO: 덱에서 불러오는 걸로 바꾸기 
        var slot = Instantiate(slotPrefab, transform);
        var card = Instantiate(prefab, slot.transform);

        card.transform.position = defaultSpawnPoint.position;
        
        var cardController = card.GetComponent<CardController>();
        cardController.beginDragEvent.AddListener(OnCardBeginDrag);
        cardController.endDragEvent.AddListener(OnCardEndDrag);
        cardController.name = cards.Count.ToString();
        cardController.curve = curve;
        cards.Add(cardController);
        
        cardController.cardMovement.disableClampPosition = true;
        
        cardController.transform
            .DOLocalMove(
                cardController.IsSelected ? new Vector3(0, cardController.cardMovement.SelectionOffset, 0) : Vector3.zero,
                tweenCardReturn ? .35f : 0).SetEase(Ease.Linear).onComplete = () => cardController.cardMovement.disableClampPosition = false;
        
    }
    

    private void Update()
    {
        SortingCards();
        SelectedCardList();
    }

    private void SelectedCardList()
    {
        foreach (var card in cards)
        {
            if (card.IsSelected && !playingCards.Contains(card))
            {
                playingCards.Add(card);
            }
            else if (!card.IsSelected && playingCards.Contains(card))
            {
                playingCards.Remove(card);
            }
        }
    }

    // TODO
    // 중앙에 배치하는 함수
    public void DrawSelectedCard()
    {
        if (playingCards.Count == 0 || playingCards.Count > 5)
            return;
        _isPlaying = true;
        float a = 0;
        foreach (var card in playingCards)
        {
            a += 2;
            card.transform.position = new Vector2(-4.0f + a, 0.3f);
            card.IsOnPlay = true;
        }
        scoreCount();
    }

    // TODO
    // 총 점수 계산하는 함수 (족보까지)
    int plusPoint;
    float mltPoint;
    public int scoreCount()
    {
        // 초기화
        plusPoint = 0;
        mltPoint = 0f;
        StopAllCoroutines(); // 이전 점수 계산 Coroutine이 있다면 정지
        StartCoroutine(CountScoreWithDelay());
        return nowScore;
    }

    IEnumerator CountScoreWithDelay()
    {
        var handRanking = CheckHandRanking(playingCards);

        switch (handRanking)
        {
            case HandRanking.RoyalFlush:
                plusPoint = 100;
                mltPoint = 8;
                JbText.text = "Royal Flush";
                plusText.text = plusPoint.ToString();
                multiText.text = mltPoint.ToString();
                break;
            case HandRanking.StraightFlush:
                plusPoint = 100;
                mltPoint = 8;
                JbText.text = "Straight Flush";
                plusText.text = plusPoint.ToString();
                multiText.text = mltPoint.ToString();
                break;
            case HandRanking.FourOfAKind:
                plusPoint = 60;
                mltPoint = 7;
                JbText.text = "Four Of A Kind";
                plusText.text = plusPoint.ToString();
                multiText.text = mltPoint.ToString();
                break;
            case HandRanking.FullHouse:
                plusPoint = 40;
                mltPoint = 4;
                JbText.text = "Full House";
                plusText.text = plusPoint.ToString();
                multiText.text = mltPoint.ToString();
                break;
            case HandRanking.Flush:
                plusPoint = 35;
                mltPoint = 4;
                JbText.text = "Flush";
                plusText.text = plusPoint.ToString();
                multiText.text = mltPoint.ToString();
                break;
            case HandRanking.Straight:
                plusPoint = 30;
                mltPoint = 4;
                JbText.text = "Straight";
                plusText.text = plusPoint.ToString();
                multiText.text = mltPoint.ToString();
                break;
            case HandRanking.ThreeOfAKind:
                plusPoint = 30;
                mltPoint = 3;
                JbText.text = "Three Of A Kind";
                plusText.text = plusPoint.ToString();
                multiText.text = mltPoint.ToString();
                break;
            case HandRanking.TwoPair:
                plusPoint = 20;
                mltPoint = 2;
                JbText.text = "Two Pair";
                plusText.text = plusPoint.ToString();
                multiText.text = mltPoint.ToString();
                break;
            case HandRanking.OnePair:
                plusPoint = 10;
                mltPoint = 2;
                JbText.text = "One Pair";
                plusText.text = plusPoint.ToString();
                multiText.text = mltPoint.ToString();
                break;
            case HandRanking.HighCard:
                plusPoint = 10;
                mltPoint = 1;
                JbText.text = "High Card";
                plusText.text = plusPoint.ToString();
                multiText.text = mltPoint.ToString();
                break;
        }

        foreach (var card in playingCards)
        {
            yield return new WaitForSeconds(0.3f); // 0.3초 대기
            plusPoint += card.point; // 카드 기본 점수 플러스
            plusText.text = plusPoint.ToString();
            cardScoreText.text = "+" + card.point.ToString();
            cardScoreText.transform.position = card.transform.position + new Vector3(0, 1.5f);
        }

        foreach (var card in playingCards)
        {
            yield return new WaitForSeconds(0.3f);
            mltPoint += card.multipoint; // 카드 타입 곱하기
            multiText.text = mltPoint.ToString();
            cardScoreText.text = "x" + card.multipoint.ToString();
            cardScoreText.transform.position = card.transform.position + new Vector3(0, 1.5f);
        }

        yield return new WaitForSeconds(0.3f);
        cardScoreText.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.5f);

        

        nowScore = (int)(Math.Round(plusPoint * mltPoint));
        scoreText.text = nowScore.ToString(); // UI 업데이트
        

        // 점수 합산이 끝난 후 요소 제거
        foreach (var card in playingCards)
        {
            yield return new WaitForSeconds(0.15f); // 0.3초 대기
            Destroy(card);
        }
        
        _isPlaying = false;

        yield return new WaitForSeconds(0.6f);
        rsM.GameEnd();
    }




    private void SortingCards()
    {
        if (!selectedCard)
            return;
        if (_isCrossing)
            return;
        if (_isPlaying)
            return;
        for (int i = 0; i < cards.Count; i++)
        {
            if (selectedCard.transform.position.x > cards[i].transform.position.x)
            {
                if (selectedCard.ParentIndex() < cards[i].ParentIndex())
                {
                    Swap(i);
                    break;
                }
            }
            
            if (selectedCard.transform.position.x < cards[i].transform.position.x)
            {
                if (selectedCard.ParentIndex() > cards[i].ParentIndex())
                {
                    Swap(i);
                    break;
                }
            }
        }
    }

    private void Swap(int index)
    {
        _isCrossing = true;

        Transform focusedParent = selectedCard.transform.parent;
        Transform crossedParent = cards[index].transform.parent;
        
        cards[index].transform.SetParent(focusedParent);
        cards[index].transform.localPosition = cards[index].IsSelected
            ? new Vector3(0, cards[index].cardMovement.SelectionOffset, 0)
            : Vector3.zero;
        selectedCard.transform.SetParent(crossedParent);

        _isCrossing = false;
        
        if (!cards[index].cardVisual)
            return;

        bool swapIsRight = cards[index].ParentIndex() > selectedCard.ParentIndex();
        cards[index].cardVisual.Swap(swapIsRight ? -1 : 1);
        
        // 구조 분해 스왑
        var selectedCardIndex = cards.FindIndex(x => x.name == selectedCard.name);
        (cards[selectedCardIndex], cards[index]) = (cards[index], cards[selectedCardIndex]);

        //Updated Visual Indexes
        foreach (var card in cards)
        {
            card.cardVisual.UpdateSiblingIndex();
        }
    }
    
    
    // Input Events -----
    
    private void OnDeselectInput(InputAction.CallbackContext context)
    {
        DeselectAllCard();
    }
    
    // Card Interaction Events -----
    
    private void OnCardBeginDrag(CardController cardController)
    {
        if (selectedCard)
            return;
        selectedCard = cardController;
    }

    private void OnCardEndDrag(CardController cardController)
    {
        if (!selectedCard)
            return;

        selectedCard.transform
            .DOLocalMove(
                selectedCard.IsSelected ? new Vector3(0, selectedCard.cardMovement.SelectionOffset, 0) : Vector3.zero,
                tweenCardReturn ? .15f : 0).SetEase(Ease.OutBack);

        selectedCard = null;
    }
}
