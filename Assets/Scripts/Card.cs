using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    public Image cardImage;    
    private Sprite frontSprite;
    private Sprite backSprite; 
    private GameManager gameManager;
    private bool isFlipped = false;
    private bool isMatched = false;

    private void Awake()
    {
        cardImage = GetComponent<Image>();
    }

    public void Initialize(Sprite frontImage, GameManager manager)
    {
        frontSprite = frontImage;
        gameManager = manager;
        backSprite = cardImage.sprite;
    }

    public void FlipCard(bool showFront)
    {
        if (isMatched) return;

        isFlipped = showFront;
        cardImage.sprite = showFront ? frontSprite : backSprite;
    }

    public void OnCardClicked()
    {
        if (isFlipped) return;

        FlipCard(true);
        gameManager.OnCardSelected(this);
    }

    public void SetMatched()
    {
        isMatched = true;
        FlipCard(true);
        gameManager.score += 5;
        gameManager.UpdateScore();
    }

    public void Unmatch()
    {
        isFlipped = false;
        FlipCard(false); 
    }

    public string GetCardImageName()
    {
        return frontSprite.name;
    }

    public bool IsFlipped()
    {
        return isFlipped;
    }

    public bool IsMatched()
    {
        return isMatched;
    }

}
