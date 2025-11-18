// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;

// public class DifficultySelectionUI : MonoBehaviour
// {
//     public TMP_Dropdown difficultyDropdown;
//     public Button continueButton;
//     public GameObject roomPanel;

//     public GameObject Difficulty;

//     public static RoomSettings.Difficulty SelectedDifficulty;

//     void Start()
//     {
//         continueButton.onClick.AddListener(OnContinue);
//     }

//     void OnContinue()
//     {
//         SelectedDifficulty = (RoomSettings.Difficulty)difficultyDropdown.value;

//         // Move to room screen
//         roomPanel.SetActive(true);

//         Difficulty.SetActive(false);
//     }
// }
