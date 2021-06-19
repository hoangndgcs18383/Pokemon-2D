using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class BattleDialogBox : MonoBehaviour
{
    [SerializeField] int letterPerSecond;

    [SerializeField] Text dialogText;
    [SerializeField] Text ppText;
    [SerializeField] Text typeText;

    [SerializeField] GameObject actionSelector;
    [SerializeField] GameObject moveSelector;
    [SerializeField] GameObject moveDetails;
    [SerializeField] GameObject choiceBox;

    [SerializeField] List<Text> actionTexts;
    [SerializeField] List<Text> moveTexts;

    [SerializeField] Text yesText;
    [SerializeField] Text noText;

    Color hightlightedColor;

    private void Start()
    {
        hightlightedColor = GlobalSettings.i.HighlightedColor;    
    }
    public void SetDialog(string dialog)
    {
        dialogText.text = dialog;
    }
    public IEnumerator TypeDialog(string dialog)
    {
        dialogText.text = "";
        foreach (var letter in dialog.ToCharArray())
        {
            dialogText.text += letter;
            yield return new WaitForSeconds(1f / letterPerSecond);
        }
        yield return new WaitForSeconds(1f);
    }

    public void EnableDialogText(bool enable)
    {
        dialogText.enabled = enable;
    }
    // Action Selector
    public void EnableActionSelector(bool enable)
    {
        actionSelector.SetActive(enable);
    }
    public void UpdateActionSelector(int selectedAction)
    {
        for(int i = 0; i < actionTexts.Count; ++i)
        {
            if (i == selectedAction)
            {
                actionTexts[i].color = hightlightedColor;
            }
            else
            {
                actionTexts[i].color = Color.black;
            }
        }
    }
    public void UpdateMoveSelector(int selectedMove, Move move)
    {
        for (int i = 0; i < moveTexts.Count; ++i)
        {
            if (i == selectedMove)
            {
                moveTexts[i].color = hightlightedColor;
            }
            else
            {
                moveTexts[i].color = Color.black;
            }
        }

        ppText.text = $"PP { move.PP}/{ move.Base.PP}";
        typeText.text = move.Base.Type.ToString();

        if(move.PP == 0)
        {
            ppText.color = Color.red;

        }
        else if(move.PP <= 5)
        {
            ppText.color = Color.yellow;
        }
        else
        {
            ppText.color = Color.black;
        }
    }
    // Move Selector
    public void EnableMoveSelector(bool enable)
    {
        moveSelector.SetActive(enable);
        moveDetails.SetActive(enable);
    }

    public void SetMoveNames(List<Move> moves)
    {
        for (int i = 0; i<moveTexts.Count; ++i)
        {
            if(i < moves.Count)
            {
                moveTexts[i].text = moves[i].Base.Name;
            }
            else
            {
                moveTexts[i].text = "-";
            }
        }
    }
    //Choice Box
    public void EnableChoiceBox(bool enable)
    {
        choiceBox.SetActive(enable);
    }
    public void UpdateChoiceBox(bool yesSelected)
    {
        if (yesSelected)
        {
            yesText.color = hightlightedColor;
            noText.color = Color.black;
        }
        else
        {
            yesText.color = Color.black;
            noText.color = hightlightedColor;
        }
    }
}
