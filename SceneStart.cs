using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneStart : MonoBehaviour
{
    // for handling the main menu and loading the game's scene
    // the main menu and the actual game are split into two different scenes, as the main menu loads the game's scene

    public GameObject screenText;
    public GameObject nameText;
    public GameObject startGameButton;
    public GameObject displayTutorialToggleButton;
    public GameObject tutorialObjectsCarrier;
    bool isTutorialToggled = false;
    
    void Start()
    {
        StartCoroutine(GrowText()); // asynchronously grows the text displaying the name of the game in real time
    }

    IEnumerator GrowText()
    {
        float t = 1;
        int initialFontSize = screenText.GetComponent<Text>().fontSize;

        while (t <= 5) //  
        {
            screenText.GetComponent<Text>().fontSize = Mathf.RoundToInt(initialFontSize * t);
            t += Time.deltaTime * 4; // Adds to the time counter. Time.deltaTime represents the time since the last frame, so that the asynchronous growText function doesn't happen quicker on computers running higher framerates.
            // Essentially keeps the time constant between different computers running the game.
            yield return null;
        }

        StartCoroutine(IntroduceName()); // When the text saying 'Chess AI' has finished growing, now grows the text saying my name
    }

    IEnumerator IntroduceName()
    {
        nameText.SetActive(true); // makes the name text visible to the player
        float t = 1;
        int initialFontSize = nameText.GetComponent<Text>().fontSize; // gets the initial font size so that the growth in the font size is linear, as opposed to multiplying the current font size by the time, which would be exponential.

        while (t <= 5)
        {
            nameText.GetComponent<Text>().fontSize = Mathf.RoundToInt(initialFontSize * t); // grows the font size of the name text
            t += Time.deltaTime * 4;
            yield return null;
        }
        while (t <= 6) // waits for one more second after the name text has finished growing
        {
            t += Time.deltaTime * 4;
            yield return null;
        }
        startGameButton.SetActive(true); // now displays the start game and UI tutorial buttons
        displayTutorialToggleButton.SetActive(true);
    }

    public void ToggleTutorialWindow() // when the 'toggle tutorial button' is pressed, it will toggle whether or not the UI tutorial is on display.
    {
        // this procedure is both called by the UI tutorial button and the return button that is present when the UI tutorial is open, in order to open a way back.
        if (isTutorialToggled) // if the display was previously on, remove both the UI tutorial and the Return button.
        {
            tutorialObjectsCarrier.SetActive(false);
        }
        else // display the UI tutorial and activate the Return button, to allow the user to go back out of the UI tutorial.
        {
            tutorialObjectsCarrier.SetActive(true);
        }
        isTutorialToggled = !isTutorialToggled; // toggle the toggle variable
    }

    public void LoadGameScene()
    {
        SceneManager.LoadScene("Chess Project"); // when the user presses the start game button, load the actual game scene.
    }

}
