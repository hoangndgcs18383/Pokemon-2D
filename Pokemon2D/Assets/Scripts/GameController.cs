using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState { FreeRoam, Battle, Dialog, Menu, PartyScreen, Bag ,CutScene, Pause}
public class GameController : MonoBehaviour
{
    [SerializeField] PlayerController playerController;
    [SerializeField] BattleSystem battleSystem;
    [SerializeField] Camera worldCamera;
    [SerializeField] PartyScreen partyScreen;
    [SerializeField]InventoryUI inventoryUI;

    GameState state;
    GameState stateBeforePause;

    public SceneDetails CurrentScene { get; private set; }
    public SceneDetails PreveScene { get; private set; }

    MenuController menuController;

    public static GameController Instance { get; private set; }
    private void Awake()
    {
        Instance = this;

        menuController = GetComponent<MenuController>();
        /* Disable mouse on playing game
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        */

        PokemonDB.Init();
        MoveDB.Init();
        ConditionDB.Init();
    }
    private void Start()
    {
        battleSystem.OnBattleOver += EndBattle;

        partyScreen.Init();

        DialogManager.Instance.OnShowDialog += () =>
        {
            state = GameState.Dialog;
        };
        DialogManager.Instance.OnCloseDialog += () =>
        {
            if (state == GameState.Dialog)
            state = GameState.FreeRoam;
        };

        menuController.onBack += () =>
        {
            state = GameState.FreeRoam;
        };

        menuController.onMenuSelected += OnMenuSelected;
    }
    public void PauseGame(bool pause)
    {
        if (pause)
        {
            stateBeforePause = state;
            state = GameState.Pause;
        }
        else
        {
            state =  stateBeforePause;
        }
    }
    public void StartBattle()
    {
        state = GameState.Battle;
        battleSystem.gameObject.SetActive(true);
        worldCamera.gameObject.SetActive(false);

        var playerParty = playerController.GetComponent<PokemonParty>();
        var wildPokemon = CurrentScene.GetComponent<MapArea>().GetRandomWildPokemon();
        //Fix issue 
        var wildPokemonCopy = new Pokemon(wildPokemon.Base, wildPokemon.Level);

        battleSystem.StartBattle(playerParty, wildPokemonCopy);
    }
    TrainerController trainer;
    public void StartTrainerBattle(TrainerController trainer)
    {
        state = GameState.Battle;
        battleSystem.gameObject.SetActive(true);
        worldCamera.gameObject.SetActive(false);

        this.trainer = trainer;
        var playerParty = playerController.GetComponent<PokemonParty>();
        var trainerParty = trainer.GetComponent<PokemonParty>();


        battleSystem.StartTrainerBattle(playerParty, trainerParty);
    }
    public void OnEnterTrainerView(TrainerController trainer)
    {
        state = GameState.CutScene;
        StartCoroutine(trainer.TriggerTrainerBattle(playerController));
    }
    void EndBattle(bool won)
    {
        if(trainer != null && won == true)
        {
            trainer.BattleLost();
            trainer = null;
        }
        state = GameState.FreeRoam;
        battleSystem.gameObject.SetActive(false);
        worldCamera.gameObject.SetActive(true);
    }
    void Update()
    {
        if(state == GameState.FreeRoam)
        {
            playerController.HandleUpdate();

            if (Input.GetKeyDown(KeyCode.Return))
            {
                menuController.OpenMenu();
                state = GameState.Menu;
            }
        }
        else if (state == GameState.Battle)
        {
            battleSystem.HandleUpdate();
        }
        else if (state == GameState.Dialog)
        {
            DialogManager.Instance.HandleUpdate();
        }
        else if(state == GameState.Menu)
        {
            menuController.HandleUpdate();
        }
        else if(state == GameState.PartyScreen)
        {
            Action onSelected = () =>
            {
                //Go to Summary Screen
            };
            Action onBack = () =>
            {
                partyScreen.gameObject.SetActive(false);
                state = GameState.FreeRoam;
            };

            partyScreen.HandleUpdate(onSelected, onBack);
        }
        else if(state == GameState.Bag)
        {
            Action onBack = () =>
            {
                inventoryUI.gameObject.SetActive(false);
                state = GameState.FreeRoam;
            };

            inventoryUI.HandleUpdate(onBack);
        }
    }
    public void SetCurrentScene(SceneDetails currScene)
    {
        PreveScene = CurrentScene;
        CurrentScene = currScene;
    }
    void OnMenuSelected(int selectedItem)
    {
        if(selectedItem == 0)
        {
            //Pokemon
            partyScreen.gameObject.SetActive(true);
            partyScreen.SetPartyData(playerController.GetComponent<PokemonParty>().Pokemons);
            state = GameState.PartyScreen;
        }
        else if(selectedItem == 1)
        {
            //Bag
            inventoryUI.gameObject.SetActive(true);
            state = GameState.Bag;
        }
        else if(selectedItem == 2)
        {
            //Save
            SavingSystem.i.Save("saveSlot1");
            state = GameState.FreeRoam;
        }
        else if(selectedItem == 3)
        {
            //Load
            SavingSystem.i.Load("saveSlot1");
            state = GameState.FreeRoam;
        }
    }
}
