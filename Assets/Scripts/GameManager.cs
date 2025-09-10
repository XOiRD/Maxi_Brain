using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;

public class GameManager : MonoBehaviour
{
    [Header("General")]
    [Space(5)]
    public Animator animator;
    public GameObject cardPrefab; // The card prefab
    public int timerLimit = 60;
    bool isGameFinished = false;

    [Header("Mainmenu page")]
    [Space(5)]
    public TMP_Dropdown columnsDropdown;
    public TMP_Dropdown rowsDropdown;

    [Header("Gameplay page")]
    [Space(5)]
    public GridLayoutGroup gridLayout;
    public RectTransform gameArea; // The grid container
    public Sprite[] cardImages; // An array of the front images of the cards
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;
    public int score;
    public GameObject endScene;

    [Header("Mechanics")]
    [Space(5)]
    [SerializeField] private int columns;
    [SerializeField] private int rows;
    private List<GameObject> cards = new List<GameObject>();
    [SerializeField] private List<Card> firstSelectedCard = null;
    [SerializeField] private List<Card> secondSelectedCard = null;
    private bool isCheckingMatch = false;
    [SerializeField]private int totalMatches;
    [SerializeField]private int currentMatches = 0;
    private List<Sprite> selectedImages = new List<Sprite>();

    [Header("Combos")]
    [Space(5)]
    public TextMeshProUGUI comboText;   
    public int basePairPoints = 5;      
    public int comboBonusPerStreak = 2; 
    private int currentStreak = 0;      
    private int maxStreak = 0;          

    [Header("Sound effects")]
    [Space(5)]
    AudioSource audioSource;
    public AudioClip flipcardAudio;
    public AudioClip matchAudio;
    public AudioClip mismatchAudio;
    public AudioClip gameOverAudio;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void StartGame()
    {
        UpdateGrid();
        animator.Play("Gameplay");
        StartCoroutine(CountdownTimer());
    }

    void LoadPreviousGame()
    {
        animator.Play("Gameplay");
        StartCoroutine(CountdownTimer());
    }

    public void UpdateGrid(bool isLoading = false)
    {
        foreach (Transform child in gridLayout.transform)
            Destroy(child.gameObject);
        cards.Clear();
        if (!isLoading)
        {
            columns = int.Parse(columnsDropdown.options[columnsDropdown.value].text); 
            rows = int.Parse(rowsDropdown.options[rowsDropdown.value].text);      
        }
        int totalCards = rows * columns;
        if ((totalCards % 2) != 0)
        {
      
            if (columns == 3) rows = 2;
            else columns = 2; 

            Debug.LogWarning($"Invalid grid requested; snapping to a valid one: {rows}x{columns}.");
            totalCards = rows * columns;
        }
        totalMatches = totalCards / 2;
        int availableUnique = cardImages.Length; 
        if (availableUnique < totalMatches)
        {
            Debug.LogError($"Not enough unique card images: need {totalMatches}, have {availableUnique}. " +
                           $"Add more images or choose a smaller grid.");
            return;
        }
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        gridLayout.constraintCount = rows;
        float cellWidth = gameArea.rect.width / columns - gridLayout.spacing.x;
        float cellHeight = gameArea.rect.height / rows - gridLayout.spacing.y;
        float aspectRatio = 150f / 210f;
        if (cellWidth / cellHeight > aspectRatio)
            cellWidth = cellHeight * aspectRatio;
        else
            cellHeight = cellWidth / aspectRatio;
        gridLayout.cellSize = new Vector2(cellWidth, cellHeight);
        selectedImages = new List<Sprite>(totalCards);
        for (int i = 0; i < totalMatches; i++)
        {
            selectedImages.Add(cardImages[i]);
            selectedImages.Add(cardImages[i]);
        }
        if (!isLoading)
            Shuffle(selectedImages);
        for (int i = 0; i < totalCards; i++)
        {
            GameObject cardGO = Instantiate(cardPrefab, gridLayout.transform);
            var card = cardGO.GetComponent<Card>();
            card.Initialize(selectedImages[i], this);
            cards.Add(cardGO);
        }
    }

    private void Shuffle(List<Sprite> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            Sprite temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    public void OnCardSelected(Card selectedCard)
    {
        if(firstSelectedCard.Count == secondSelectedCard.Count)
        {
            firstSelectedCard.Add(selectedCard);
            audioSource.clip = flipcardAudio;
            audioSource.Play();
        }
        else
        {
            secondSelectedCard.Add(selectedCard);
            audioSource.clip = flipcardAudio;
            audioSource.Play();
        }
        if (firstSelectedCard.Count == 1 && secondSelectedCard.Count == 1)
        {
            StartCoroutine(CheckMatch());
        }
    }

    private IEnumerator CheckMatch()
    {
        isCheckingMatch = true;

        yield return new WaitForSeconds(0.5f);

        if (firstSelectedCard[0].GetCardImageName() == secondSelectedCard[0].GetCardImageName())
        {
            firstSelectedCard[0].SetMatched();
            secondSelectedCard[0].SetMatched();
            OnPairMatched();
            audioSource.clip = matchAudio;
            audioSource.Play();
            currentMatches++;
            if(currentMatches == totalMatches)
            {
                isGameFinished = true;
                audioSource.clip = gameOverAudio;
                audioSource.Play();
                endScene.SetActive(true);
                endScene.transform.GetChild(0).gameObject.SetActive(true);
            }
        }
        else
        {
            firstSelectedCard[0].Unmatch();
            secondSelectedCard[0].Unmatch();
            OnPairMismatched();
            audioSource.clip = mismatchAudio;
            audioSource.Play();
        }
        firstSelectedCard.RemoveAt(0);
        secondSelectedCard.RemoveAt(0);
        isCheckingMatch = false;
        if(firstSelectedCard.Count > 0 && secondSelectedCard.Count > 0)
        {
            StartCoroutine(CheckMatch());
        }
    }

    public bool IsCheckingMatch()
    {
        return isCheckingMatch;
    }

    public void UpdateScore()
    {
        scoreText.text = score.ToString();
    }

    public void OnPairMatched()
    {
        currentStreak++;
        if (currentStreak > maxStreak) maxStreak = currentStreak;
        int bonus = (currentStreak - 1) * comboBonusPerStreak;
        score += basePairPoints + Mathf.Max(0, bonus);
        UpdateScore();
        UpdateComboUI();
    }

    public void OnPairMismatched()
    {
        currentStreak = 0;
        UpdateComboUI();
    }

    private void UpdateComboUI()
    {
        if (comboText == null) return;
        if (currentStreak < 1) comboText.text = "";
        else comboText.text = "Combo x" + currentStreak + "!";
    }

    IEnumerator CountdownTimer()
    {
        yield return new WaitForSeconds (0.5f);
        timerText.text = timerLimit.ToString();
        while(timerLimit > 0 && !isGameFinished)
        {
            timerLimit--;
            timerText.text = timerLimit.ToString();
            yield return new WaitForSeconds(1f);
        }
        if (!isGameFinished)
        {
            audioSource.clip = gameOverAudio;
            audioSource.Play();
            endScene.SetActive(true);
            endScene.transform.GetChild(1).gameObject.SetActive(true);
        }
    }

    public void SaveGame()
    {
        GameData gameData = new GameData
        {
            columns = columns,
            rows = rows,
            score = score,
            timerLimit = timerLimit,
            currentMatches = currentMatches,
            isGameFinished = isGameFinished,
            cards = new List<CardData>()
        };
        if (currentMatches < totalMatches)
        {
            foreach (GameObject card in cards)
            {
                gameData.cards.Add(new CardData
                {
                    imageName = card.GetComponent<Card>().GetCardImageName(),
                    isFlipped = card.GetComponent<Card>().IsFlipped(),
                    isMatched = card.GetComponent<Card>().IsMatched()
                });
            }

            BinaryFormatter formatter = new BinaryFormatter();
            FileStream file = File.Create(Application.persistentDataPath + "/gameData.dat");
            formatter.Serialize(file, gameData);
            file.Close();
            SceneManager.LoadScene(0, LoadSceneMode.Single);
        }
    }

    public void LoadGame()
    {
        if (File.Exists(Application.persistentDataPath + "/gameData.dat"))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream file = File.Open(Application.persistentDataPath + "/gameData.dat", FileMode.Open);
            GameData gameData = (GameData)formatter.Deserialize(file);
            file.Close();

            columns = gameData.columns;
            rows = gameData.rows;
            score = gameData.score;
            timerLimit = gameData.timerLimit;
            currentMatches = gameData.currentMatches;
            isGameFinished = gameData.isGameFinished;

            // Set dropdowns to match loaded state
            columnsDropdown.value = columnsDropdown.options.FindIndex(option => option.text == columns.ToString());
            rowsDropdown.value = rowsDropdown.options.FindIndex(option => option.text == rows.ToString());

            UpdateGrid(true);

            for (int i = 0; i < cards.Count; i++)
            {
                Card card = cards[i].GetComponent<Card>();
                CardData cardData = gameData.cards[i];

                card.Initialize(cardImages[int.Parse(cardData.imageName)-1], this); // Reinitialize the card with the saved image
                card.FlipCard(cardData.isFlipped);
                if (cardData.isMatched)
                {
                    card.SetMatched();
                }
            }
            LoadPreviousGame();
        }
    }

    public void RestartScene()
    {
        DeleteSaveFile();
        SceneManager.LoadScene(0, LoadSceneMode.Single);
    }

    private void DeleteSaveFile()
    {
        if (File.Exists(Application.persistentDataPath + "/gameData.dat"))
        {
            File.Delete(Application.persistentDataPath + "/gameData.dat");
            Debug.Log("Saved game file deleted.");
        }
    }
}

// Supporting Classes

[System.Serializable]
public class GameData
{
    public int columns;
    public int rows;
    public int score;
    public int timerLimit;
    public int currentMatches;
    public bool isGameFinished;
    public List<CardData> cards;
}

[System.Serializable]
public class CardData
{
    public string imageName;
    public bool isFlipped;
    public bool isMatched;
}
