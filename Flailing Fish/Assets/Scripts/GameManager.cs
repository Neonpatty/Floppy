using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class GameManager : MonoBehaviour
{
    #region Variables

    //ENUMS

    public enum GameState
    {
        MainMenu,
        PreGame,
        Gameplay,
        EndGame
    }
    
    //NON_SERIALIZED
    private GameState CurrentState;
    private int Score = 0;

    //SERIALIZED



    //UNITY EVENTS

    [Space]
    [Header("Events")]

    [SerializeField] private UnityEvent OnPreGameTrigger;
    [SerializeField] private UnityEvent OnGameplayTrigger;
    [SerializeField] private UnityEvent OnEndGameTrigger;
    [SerializeField] private UnityEvent OnMainMenuTrigger;

    #endregion

    #region Functions

    #region GameState Functions
    public void SaveScore()
    {
        if (CurrentState == GameState.EndGame)
        {
            
        }
    }

    public void PreGame()
    {
        if(CurrentState == GameState.MainMenu)
        {
            CurrentState = GameState.PreGame;
            OnPreGameTrigger.Invoke();
            Gameplay();
        }
    }

    public void Gameplay()
    {
        if (CurrentState == GameState.PreGame)
        {
            CurrentState = GameState.Gameplay;
            OnGameplayTrigger.Invoke();
        }
    }

    public void EndGame()
    {
        if (CurrentState == GameState.Gameplay)
        {
            CurrentState = GameState.EndGame;
            OnEndGameTrigger.Invoke();
            Debug.Log("I have entered endgame state");
        }
    }

    public void MainMenu()
    {
        if (CurrentState == GameState.EndGame)
        {
            CurrentState = GameState.MainMenu;
            OnMainMenuTrigger.Invoke();
        }
    }
    #endregion

    #region Value Functions

    public GameState GetCurrentGameState()
    {
        return CurrentState;
    }

    public int GetCurrentScore()
    {
        return Score;
    }

    #endregion

    #endregion

}
