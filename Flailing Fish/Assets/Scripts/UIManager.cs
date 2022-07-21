using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    #region Variables

    //SERIALIZED
    [SerializeField] private GameObject startText;
    [SerializeField] private GameObject scoreText;
    [SerializeField] private GameObject resetButton;

    #endregion

    #region Functions

    public void PreGameUI()
    {
        startText.SetActive(false);
        scoreText.SetActive(true);
    }

    public void EndGameUI()
    {
        resetButton.SetActive(true);
    }

    public void ResetButton()
    {
        Debug.Log("I have Reloaded Scene");
        SceneManager.LoadScene("MainScene");         
    }

    #endregion
}
