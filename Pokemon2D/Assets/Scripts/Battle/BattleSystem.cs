using DG.Tweening;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum BattleState { Start, ActionSelection, MoveSelection, RunningTurn, Busy , AboutToUse , PartyScreen, MoveToForget , BattleOver}

public enum BattleAction { Move, SwitchPokemon, UseItem, Run}
public class BattleSystem : MonoBehaviour
{
    [SerializeField] BattleUnit playerUnit;
    [SerializeField] BattleUnit enemyUnit;
    [SerializeField] PartyScreen partyScreen;
    [SerializeField] Image playerImage;
    [SerializeField] Image trainerImage;
    [SerializeField] GameObject pokeballSprite;
    [SerializeField] MoveSelectionUI moveSelectionUI;

    [SerializeField] BattleDialogBox dialogBox;
    public event Action<bool> OnBattleOver;

    BattleState state;

    //Define
    int currentAction;
    int currentMove;
    bool aboutToUseChoice = true;

    PokemonParty playerParty;
    PokemonParty trainerParty;
    Pokemon wildPokemon;

    bool isTrainerBattle = false;
    PlayerController player;
    TrainerController trainer;

    int escapeAttempts;
    MoveBase moveToLearn;

    public void StartBattle(PokemonParty playerParty, Pokemon wildPokemon)
    {
        isTrainerBattle = false;
        this.playerParty = playerParty;
        this.wildPokemon = wildPokemon;
        player = playerParty.GetComponent<PlayerController>();

        StartCoroutine(SetupBattle());
    }
    public void StartTrainerBattle(PokemonParty playerParty, PokemonParty trainerPatty)
    {
        this.playerParty = playerParty;
        this.trainerParty = trainerPatty;

        isTrainerBattle = true;
        player = playerParty.GetComponent<PlayerController>();
        trainer = trainerParty.GetComponent<TrainerController>();

        StartCoroutine(SetupBattle());
    }
    //This function we handle the action( Action, Move, Party)
    public void HandleUpdate()
    {
        if (state == BattleState.ActionSelection)
        {
            HandleActionSelection();
        }
        else if (state == BattleState.MoveSelection)
        { 
            HandleMoveSelection();
        }
        else if (state == BattleState.PartyScreen)
        {
            HandlePartySelection();
        }
        else if(state == BattleState.AboutToUse)
        {
            HandleAboutToUse();
        }
        else if(state == BattleState.MoveToForget)
        {
            HandleMoveToForget();
        }
        
    }
    IEnumerator RunTurns(BattleAction playerAction)
    {
        state = BattleState.RunningTurn;

        if(playerAction == BattleAction.Move)
        {
            playerUnit.Pokemon.CurrenetMove = playerUnit.Pokemon.Moves[currentMove];
            enemyUnit.Pokemon.CurrenetMove = enemyUnit.Pokemon.GetRandomMove();

            int playerMovePriority = playerUnit.Pokemon.CurrenetMove.Base.Priority;
            int enemyMovePriority = enemyUnit.Pokemon.CurrenetMove.Base.Priority;

            //Check who goes first
            bool playerGoesFirst = true;
            if(enemyMovePriority > playerMovePriority)
            {
                playerGoesFirst = false;
            }
            else if( enemyMovePriority == playerMovePriority)
            {
                playerGoesFirst = playerUnit.Pokemon.Speed >= enemyUnit.Pokemon.Speed;
            }

            var firstUnit = (playerGoesFirst) ? playerUnit : enemyUnit;
            var secondUnit = (playerGoesFirst) ? enemyUnit : playerUnit;

            var secondPokemon = secondUnit.Pokemon;

            //First Turn
            yield return RunMove(firstUnit, secondUnit, firstUnit.Pokemon.CurrenetMove);
            //Statuses like burn or poison will hurt the pokemon after the turn
            yield return RunAfterTurn(firstUnit);
            if (state == BattleState.BattleOver) yield break;

            if (secondPokemon.HP > 0)
            {
                //Second Turn
                yield return RunMove(secondUnit, firstUnit, secondUnit.Pokemon.CurrenetMove);
                yield return RunAfterTurn(secondUnit);
                if (state == BattleState.BattleOver) yield break;
            }
        }
        else
        {
            //Handle Action/Pokemon
            if(playerAction == BattleAction.SwitchPokemon)
            {
                var selectedMember = partyScreen.SelectedMember;
                state = BattleState.Busy;
                yield return SwitchPokemon(selectedMember);
            }
            //Handle Action/Bag
            else if(playerAction == BattleAction.UseItem)
            {
                dialogBox.EnableActionSelector(false);
                yield return ThrowPokeball();
            }
            //Handle Action/Run
            else if (playerAction == BattleAction.Run)
            {
                yield return TryToEscape();
            }
            //Enemy Turn
            var enemyMove = enemyUnit.Pokemon.GetRandomMove();
            yield return RunMove(enemyUnit, playerUnit, enemyMove);
            yield return RunAfterTurn(enemyUnit);
            if (state == BattleState.BattleOver) yield break;
        }
        if(state != BattleState.BattleOver)
        {
            ActionSelection();
        }
    }

    IEnumerator RunMove(BattleUnit sourceUnit, BattleUnit targetUnit, Move move)
    {
        bool canRunMove = sourceUnit.Pokemon.OnBeforeMove();
        if (!canRunMove)
        {
            yield return ShowStatusChanges(sourceUnit.Pokemon);
            yield return sourceUnit.Hud.UpdateHD();
            yield break;
        }
        yield return ShowStatusChanges(sourceUnit.Pokemon);
        move.PP--; // reduce PP by 1
        yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name} used {move.Base.Name}.");

        if (CheckIfMoveHits(move, sourceUnit.Pokemon, targetUnit.Pokemon))
        {
            sourceUnit.PlayAttackAnimation();
            yield return new WaitForSeconds(1f);
            targetUnit.PlayHitAnimation();

            if (move.Base.Category == MoveCategory.Status)
            {
                yield return RunMoveEffects(move.Base.Effects, sourceUnit.Pokemon, targetUnit.Pokemon, move.Base.Target);
            }
            else
            {
                //Take Damage
                var damageDetails = targetUnit.Pokemon.TakeDamage(move, sourceUnit.Pokemon);
                yield return targetUnit.Hud.UpdateHD();
                yield return ShowDamageDetails(damageDetails);
            }
            if(move.Base.Secondaries != null && move.Base.Secondaries.Count > 0 && targetUnit.Pokemon.HP > 0)
            {
                foreach (var secondary in move.Base.Secondaries)
                {
                    var rnd = UnityEngine.Random.Range(1, 101);
                    if(rnd <= secondary.Change)
                    {
                        yield return RunMoveEffects(secondary, sourceUnit.Pokemon, targetUnit.Pokemon, secondary.Target);
                    }
                }
            }
            //Unit fainted
            if (targetUnit.Pokemon.HP <= 0)
            {
                yield return HandlePokemonFainted(targetUnit);
            }
        }
        else
        {
            yield return dialogBox.TypeDialog($"{sourceUnit.Pokemon.Base.Name}'s attack missed.");
        }
    }
    IEnumerator RunMoveEffects(MoveEffects effects, Pokemon source, Pokemon target, MoveTarget moveTarget)
    {
        
        //Stat Boosting
        if (effects.Boosts != null)
        {
            if (moveTarget == MoveTarget.Self)
            {
                source.ApplyBoosts(effects.Boosts);
            }
            else
            {
                target.ApplyBoosts(effects.Boosts);
            }
        }
        //Status condition
        if(effects.Status != ConditionID.none)
        {
            target.SetStatus(effects.Status);
        }
        //Volatile Status condition
        if (effects.VolatileStatus != ConditionID.none)
        {
            target.SetVolatileStatus(effects.VolatileStatus);
        }
        yield return ShowStatusChanges(source);
        yield return ShowStatusChanges(target);
    }
    IEnumerator RunAfterTurn(BattleUnit sourceUnit)
    {
        if (state == BattleState.BattleOver) yield break;
        yield return new WaitUntil(()=> state == BattleState.RunningTurn);
        //Statuses like burn or poison will hurt the pokemon after the turn
        sourceUnit.Pokemon.OnAfterTurn();
        yield return ShowStatusChanges(sourceUnit.Pokemon);
        yield return sourceUnit.Hud.UpdateHD();
        if (sourceUnit.Pokemon.HP <= 0)
        {
            yield return HandlePokemonFainted(sourceUnit);

            yield return new WaitUntil(() => state == BattleState.RunningTurn);
        }
    }
    bool CheckIfMoveHits(Move move, Pokemon source, Pokemon target)
    {
        if (move.Base.AlwaysHits)
            return true;

        float moveAccurary = move.Base.Accuracy;

        int accurary = source.StatBoosts[Stat.Accuracy];
        int evasion = source.StatBoosts[Stat.Evasion];

        var boostValues = new float[] { 1f, 4f / 3f, 5f / 3f, 2f, 7f / 3f, 8f / 3f, 3f };

        if(accurary > 0)
        {
            moveAccurary *= boostValues[accurary];
        }
        else
        {
            moveAccurary /= boostValues[-accurary];
        }

        if (evasion > 0)
        {
            moveAccurary /= boostValues[evasion];
        }
        else
        {
            moveAccurary *= boostValues[-evasion];
        }

        return UnityEngine.Random.Range(1, 101) <= moveAccurary;
    }
    IEnumerator ShowStatusChanges(Pokemon pokemon)
    {
        while (pokemon.StatusChanges.Count > 0)
        {
            var message = pokemon.StatusChanges.Dequeue();
            yield return dialogBox.TypeDialog(message);
        }
    }
    IEnumerator HandlePokemonFainted(BattleUnit faitedUnit)
    {
        yield return dialogBox.TypeDialog($"{faitedUnit.Pokemon.Base.Name} fainted.");
        faitedUnit.PlayFaintAnimation();
        yield return new WaitForSeconds(2f);

        if (!faitedUnit.IsPlayerUnit)
        {
            //Exp Gain
            int expYield = faitedUnit.Pokemon.Base.ExpYield;
            int enemyLevel = faitedUnit.Pokemon.Level;
            float trainerBonus = (isTrainerBattle) ? 1.5f : 1f;

            int expGain = Mathf.FloorToInt((expYield * enemyLevel * trainerBonus) / 7);
            playerUnit.Pokemon.Exp += expGain;
            yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} gained {expGain} exp.");
            yield return playerUnit.Hud.SetExpSmooth();

            //Check Level Up
            //if we use if statle for this case, it will check one time, instead of solving issue we use (while)
            while (playerUnit.Pokemon.CheckForLevelUp())
            {
                playerUnit.Hud.SetLevel();
                yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} grew to level {playerUnit.Pokemon.Level}.");

                //Try to learn a new Move
                var newMove = playerUnit.Pokemon.GetLearnableMoveAtCurrLevel();
                if (newMove != null)
                {
                    if(playerUnit.Pokemon.Moves.Count < PokemonBase.MaxNumofMoves)
                    {
                        playerUnit.Pokemon.LearnMove(newMove);
                        yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} learned {newMove.Base.Name}.");
                        dialogBox.SetMoveNames(playerUnit.Pokemon.Moves);
                    }
                    else
                    {
                        yield return dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} trying to learn {newMove.Base.Name}.");
                        yield return dialogBox.TypeDialog($"But it cannot learn more than {PokemonBase.MaxNumofMoves} moves.");
                        yield return ChooseMoveToForget(playerUnit.Pokemon, newMove.Base);
                        yield return new WaitUntil(() => state != BattleState.MoveToForget);
                        yield return new WaitForSeconds(2f);
                    }
                }
                yield return playerUnit.Hud.SetExpSmooth(true);
            }
            yield return new WaitForSeconds(1f);
        }
        CheckForBattleOver(faitedUnit);
    }
    void CheckForBattleOver(BattleUnit faintedUnit)
    {
        if (faintedUnit.IsPlayerUnit)
        {
            var nextPokemon = playerParty.GetHealthyPokemon();
            if (nextPokemon != null)
            {
                OpenPartyScreen();
            }
            else
            {
                BattleOver(false);
            }
        }
        else
        {
            if (!isTrainerBattle)
            {
                BattleOver(true);
            }
            else
            {
                var nextPokemon = trainerParty.GetHealthyPokemon();
                if(nextPokemon != null)
                {
                    StartCoroutine(AboutToUse(nextPokemon));
                }
                else
                {
                    BattleOver(true);
                }
            }
        }
    }
    IEnumerator ShowDamageDetails(DamageDetails damageDetails)
    {
        if(damageDetails.Critical > 1f)
        {
            yield return dialogBox.TypeDialog("A critical hit!");
        }
        if(damageDetails.TypeEffectiveness > 1f)
        {
            yield return dialogBox.TypeDialog("It's super effective!");
        }
        else if(damageDetails.TypeEffectiveness < 1f)
        {
            yield return dialogBox.TypeDialog("It's not very effective!");
        }
    }

    public IEnumerator SetupBattle()
    {
        playerUnit.Clear();
        enemyUnit.Clear();

        if (!isTrainerBattle)
        {
            //Wild Pokemon Battle
            playerUnit.SetUp(playerParty.GetHealthyPokemon());
            enemyUnit.SetUp(wildPokemon);

            dialogBox.SetMoveNames(playerUnit.Pokemon.Moves);

            yield return dialogBox.TypeDialog($"A wild {enemyUnit.Pokemon.Base.Name} appeared.");
        }
        else
        {
            //Trainer Battle

            //Show trainer and player sprites
            playerUnit.gameObject.SetActive(false);
            enemyUnit.gameObject.SetActive(false);

            playerImage.gameObject.SetActive(true);
            trainerImage.gameObject.SetActive(true);
            playerImage.sprite = player.Sprite;
            trainerImage.sprite = trainer.Sprite;

            yield return dialogBox.TypeDialog($"{trainer.Name} want to battle.");
            //Send out first pokemon of the trainer
            trainerImage.gameObject.SetActive(false);
            enemyUnit.gameObject.SetActive(true);
            var ememyPokemon = trainerParty.GetHealthyPokemon();
            enemyUnit.SetUp(ememyPokemon);
            yield return dialogBox.TypeDialog($"{trainer.Name} send out {ememyPokemon.Base.Name}.");

            //Send out first pokemon of the player
            playerImage.gameObject.SetActive(false);
            playerUnit.gameObject.SetActive(true);
            var playerPokemon = playerParty.GetHealthyPokemon();
            playerUnit.SetUp(playerPokemon);
            yield return dialogBox.TypeDialog($"Go {ememyPokemon.Base.Name}!");
            dialogBox.SetMoveNames(playerUnit.Pokemon.Moves);
        }

        escapeAttempts = 0;
        partyScreen.Init();
        ActionSelection();
    }
    void BattleOver(bool won)
    {
        state = BattleState.BattleOver;
        playerParty.Pokemons.ForEach(p => p.OnBattleOver());
        OnBattleOver(won);
    }

    void ActionSelection()
    {
        state = BattleState.ActionSelection;
        dialogBox.SetDialog("Choose a action.");
        dialogBox.EnableActionSelector(true);
    }
    // Open Party Screen
    void OpenPartyScreen()
    {
        //state PartyScreen
        partyScreen.CalledFrom = state;
        state = BattleState.PartyScreen;

        partyScreen.SetPartyData(playerParty.Pokemons);
        partyScreen.gameObject.SetActive(true);
    }
    void MoveSelection()
    {
        state = BattleState.MoveSelection;
        dialogBox.EnableActionSelector(false);
        dialogBox.EnableDialogText(false);
        dialogBox.EnableMoveSelector(true);
    }
    IEnumerator AboutToUse(Pokemon newPokemon)
    {
        state = BattleState.Busy;
        yield return dialogBox.TypeDialog($"{trainer.Name} is about to use {newPokemon.Base.Name}. Do you want to change pokemon?");

        state = BattleState.AboutToUse;
        dialogBox.EnableChoiceBox(true);
    }
    IEnumerator ChooseMoveToForget(Pokemon pokemon,MoveBase newMove)
    {
        state = BattleState.Busy;
        yield return dialogBox.TypeDialog($"Choose a move you wan't to forget.");
        moveSelectionUI.gameObject.SetActive(true);
        moveSelectionUI.SetMoveData(pokemon.Moves.Select(x => x.Base).ToList(), newMove);
        moveToLearn = newMove;

        state = BattleState.MoveToForget;
    }
    //Handle Action
    void HandleActionSelection()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            ++currentAction;
        } else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            --currentAction;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentAction += 2;
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentAction -= 2;
        }
        currentAction = Mathf.Clamp(currentAction, 0, 3);

        dialogBox.UpdateActionSelector(currentAction);
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if(currentAction == 0)
            {
                //Fight
                MoveSelection();
            }
            else if(currentAction == 1)
            {
                //Bag
                StartCoroutine(RunTurns(BattleAction.UseItem));
            }
            else if (currentAction == 2)
            {
                //Pokemon
                OpenPartyScreen();
            }
            else if (currentAction == 3)
            {
                // Run
                StartCoroutine(RunTurns(BattleAction.Run));
            }
        }

    }

    //Handle Move
    void HandleMoveSelection()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            ++currentMove;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            --currentMove;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentMove += 2;
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentMove -= 2;
        }
        currentMove = Mathf.Clamp(currentMove, 0, playerUnit.Pokemon.Moves.Count - 1);
        dialogBox.UpdateMoveSelector(currentMove, playerUnit.Pokemon.Moves[currentMove]);

        if (Input.GetKeyDown(KeyCode.Z))
        {
            var move = playerUnit.Pokemon.Moves[currentAction];
            if(move.PP == 0) return;

            dialogBox.EnableMoveSelector(false);
            dialogBox.EnableDialogText(true);
            StartCoroutine(RunTurns(BattleAction.Move));
        }

        else if (Input.GetKeyDown(KeyCode.X))
        {
            dialogBox.EnableMoveSelector(false);
            dialogBox.EnableDialogText(true);
            ActionSelection();
        }
    }
    //Handle Party Screen
    void HandlePartySelection()
    {
        Action onSelected = () =>
        {
            var selectedMember = partyScreen.SelectedMember;
            if (selectedMember.HP <= 0)
            {
                partyScreen.SetMassageText("You can't sent out a fainted pokemon.");
                return;
            }
            if (selectedMember == playerUnit.Pokemon)
            {
                partyScreen.SetMassageText("You can't switch with the same pokemon.");
                return;
            }
            partyScreen.gameObject.SetActive(false);

            if (partyScreen.CalledFrom == BattleState.ActionSelection)
            {
                StartCoroutine(RunTurns(BattleAction.SwitchPokemon));
            }
            else
            {
                state = BattleState.Busy;
                bool isTrainerAboutToUse = partyScreen.CalledFrom == BattleState.AboutToUse;
                StartCoroutine(SwitchPokemon(selectedMember, isTrainerAboutToUse));
            }
            partyScreen.CalledFrom = null;
        };

        Action onBack = () =>
        {
            if (playerUnit.Pokemon.HP <= 0)
            {
                partyScreen.SendMessage("You have to choose a pokemon to continue.");
                return;
            }

            partyScreen.gameObject.SetActive(false);
            if (partyScreen.CalledFrom == BattleState.AboutToUse)
            {
                StartCoroutine(SendNextTrainerPokemon());
            }
            else
            {
                ActionSelection();
            }
            partyScreen.CalledFrom = null;
        };
        partyScreen.HandleUpdate(onSelected, onBack);
   
    }
    //Handle Choice Yes/No AboutToUse
    void HandleAboutToUse()
    {
        if(Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            aboutToUseChoice = !aboutToUseChoice;
        }

        dialogBox.UpdateChoiceBox(aboutToUseChoice);

        if (Input.GetKeyDown(KeyCode.Z))
        {
            dialogBox.EnableChoiceBox(false);
            if(aboutToUseChoice == true)
            {
                //Yes Option
                OpenPartyScreen();
            }
            else
            {
                //No Option
                StartCoroutine(SendNextTrainerPokemon());
            }
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            dialogBox.EnableChoiceBox(false);
            StartCoroutine(SendNextTrainerPokemon());
        }
    }
    //Handle Move To Forget
    void HandleMoveToForget()
    {
        Action<int> onMoveSelected = (moveIndex) =>
        {
            moveSelectionUI.gameObject.SetActive(false);
            if (moveIndex == PokemonBase.MaxNumofMoves)
            {
                //Don't learn the new move
                StartCoroutine(dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} did not learn {moveToLearn.Name}."));
            }
            else
            {
                //Forget the selected move and learn new move
                var selectedMove = playerUnit.Pokemon.Moves[moveIndex].Base;
                StartCoroutine(dialogBox.TypeDialog($"{playerUnit.Pokemon.Base.Name} forgot {selectedMove.Name} and learned {moveToLearn.Name}."));

                playerUnit.Pokemon.Moves[moveIndex] = new Move(moveToLearn);
            }
            moveToLearn = null;
            state = BattleState.RunningTurn;
        };

        moveSelectionUI.HandleMoveSelection(onMoveSelected);
    }
    IEnumerator SwitchPokemon(Pokemon newPokemon, bool isTrainerAboutToUse = false)
    {   
        if (playerUnit.Pokemon.HP > 0)
        {
            yield return dialogBox.TypeDialog($"Come back {playerUnit.Pokemon.Base.Name}.");
            playerUnit.PlayFaintAnimation();
            yield return new WaitForSeconds(2f);
        }
        //Set Up the next pokemon
        playerUnit.SetUp(newPokemon);
        dialogBox.SetMoveNames(newPokemon.Moves);
        yield return dialogBox.TypeDialog($"Go {newPokemon.Base.Name}!");

        if (isTrainerAboutToUse)
            StartCoroutine(SendNextTrainerPokemon());
        else
            state = BattleState.RunningTurn;
    }
    IEnumerator SendNextTrainerPokemon()
    {
        state = BattleState.Busy;

        var nextPokemon = trainerParty.GetHealthyPokemon();
        enemyUnit.SetUp(nextPokemon);
        yield return dialogBox.TypeDialog($"{trainer.Name} send out {nextPokemon.Base.Name}!");

        state = BattleState.RunningTurn;
    }
    IEnumerator ThrowPokeball()
    {
        state = BattleState.Busy;

        yield return dialogBox.TypeDialog($"{player.Name} used Pokeball <3.");

        var pokeballObj = Instantiate(pokeballSprite, playerUnit.transform.position - new Vector3(2, 0), Quaternion.identity);
        var pokeball = pokeballObj.GetComponent<SpriteRenderer>();

        //Animations
        yield return pokeball.transform.DOJump(enemyUnit.transform.position 
            + new Vector3(0, 2), 2f, 1, 1f).WaitForCompletion();
        yield return enemyUnit.PlayCaptureAnimation();
        pokeball.transform.DOMoveY(enemyUnit.transform.position.y - 1.2f, 0.5f).WaitForCompletion();
        //Check if is Trainer can't caught and break out Animation then return Battle with enemy turn.
        if (isTrainerBattle)
        {
            yield return dialogBox.TypeDialog($"You can't steal the trainer pokemon!");
            yield return new WaitForSeconds(1f);
            pokeball.DOFade(0, 0.2f);
            yield return enemyUnit.PlayBreakOutAnimation();
            Destroy(pokeball);
            state = BattleState.RunningTurn;
            yield break;
        }

        int shakeCount = TryToCatchPokemon(enemyUnit.Pokemon);

        for(int i=0; i<Mathf.Min(shakeCount, 3); i++)
        {
           yield return new WaitForSeconds(0.5f);
           yield return pokeball.transform.DOPunchRotation(new Vector3(0, 0, 10f), 0.8f).WaitForCompletion();
        }
        if(shakeCount == 4)
        {
            //Pokemon is caught
            yield return dialogBox.TypeDialog($"{enemyUnit.Pokemon.Base.Name} was caught.");
            yield return pokeball.DOFade(0, 1.5f).WaitForCompletion();

            playerParty.AddPokemon(enemyUnit.Pokemon);
            yield return dialogBox.TypeDialog($"{enemyUnit.Pokemon.Base.Name} has been added to your party.");

            Destroy(pokeball);
            BattleOver(true);
        }
        else
        {
            // Pokemon broke out
            yield return new WaitForSeconds(1f);
            pokeball.DOFade(0, 0.2f);
            yield return enemyUnit.PlayBreakOutAnimation();

            if(shakeCount < 2)
                yield return dialogBox.TypeDialog($"{enemyUnit.Pokemon.Base.Name} broke free.");
            else
                yield return dialogBox.TypeDialog($"{enemyUnit.Pokemon.Base.Name} almost count it.");

            Destroy(pokeball);
            state = BattleState.RunningTurn;
        }
    }
    int TryToCatchPokemon(Pokemon pokemon)
    {
        float a = (3 * pokemon.MaxHP - 2 * pokemon.HP) 
            * pokemon.Base.CatchRate 
            * ConditionDB.GetStaticBonus(pokemon.Status) / (3 * pokemon.MaxHP);

        if(a >= 255)
        {
            return 4;
        }
        float b = 1048560 / Mathf.Sqrt(Mathf.Sqrt(16711680) / a);

        int shakeCount = 0;
        while(shakeCount < 4)
        {
            if (UnityEngine.Random.Range(0, 65535) >= b)
            {
                break;
            }
            ++shakeCount;
        }
        return shakeCount;
    }
    IEnumerator TryToEscape()
    {
        state = BattleState.Busy;
        if (isTrainerBattle)
        {
            yield return dialogBox.TypeDialog($"You can't run from trainer battles.");
            state = BattleState.RunningTurn;
            yield break;
        }
        ++escapeAttempts;

        int playerSpeed = playerUnit.Pokemon.Speed;
        int enemySpeed = enemyUnit.Pokemon.Speed;

        if(playerSpeed > enemySpeed)
        {
            yield return dialogBox.TypeDialog($"Ran away safely!");
            BattleOver(true);
        }
        else
        {
            float f = (playerSpeed * 128) / enemySpeed + 30 * escapeAttempts;
            f = f % 256;

            if(UnityEngine.Random.Range(0, 256) < f)
            {
                yield return dialogBox.TypeDialog($"Ran away safely!");
                BattleOver(true);
            }
            else
            {
                yield return dialogBox.TypeDialog($"Can't escape!");
                state = BattleState.RunningTurn;
            }
        }
    }
}//class
