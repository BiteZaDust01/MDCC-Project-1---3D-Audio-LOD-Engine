using UnityEngine;
using UnityEngine.UI;
using System.Collections; // Required for Coroutines

public class StatsWindowToggle : MonoBehaviour
{
    public GameObject statsPanel;
    public Text resultText; // We need to control the text from here now

    void Start()
    {
        if (statsPanel != null && resultText != null)
        {
            // 1. Show the window and set the intro text when the game starts
            statsPanel.SetActive(true);
            resultText.text = "Press 'E' to interact with objects";

            // 2. Start the 3-second countdown
            StartCoroutine(HideIntroRoutine());
        }
    }

    IEnumerator HideIntroRoutine()
    {
        // Wait for exactly 3 seconds
        yield return new WaitForSeconds(1f);

        // Hide the window and clear the text so it is blank by default
        if (statsPanel != null) statsPanel.SetActive(false);
        if (resultText != null) resultText.text = "Interact with an object to view stats.";
    }

    void Update()
    {
        // The player can still press Tab to manually check the window at any time
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (statsPanel != null)
            {
                statsPanel.SetActive(!statsPanel.activeSelf);
            }
        }
    }
}