using UnityEngine;
using UnityEngine.U2D.Animation;
using UnityEngine.UI;

namespace ScriptableObjects
{
    [CreateAssetMenu(fileName = "CardSpriteResource", menuName = "CardData", order = 0)]
    public class CardSpriteResources : ScriptableObject
    {
        [field: SerializeField] public SpriteLibraryAsset PlayingCardSpriteLibrary { get; private set;}
        [field: SerializeField] public SpriteLibraryAsset JokerCardSpriteLibrary { get; private set;}
        [field: SerializeField] public SpriteLibraryAsset TarotCardSpriteLibrary { get; private set;}
        [field: SerializeField] public SpriteLibraryAsset PlanetCardSpriteLibrary { get; private set;}
        [field: SerializeField] public SpriteLibraryAsset GhostCardSpriteLibrary { get; private set;}
    }
}