using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Item/Create new recovery item")]
public class RecoveryItem : ItemBase
{
    //HP
    [Header("HP")]
    [SerializeField] int hpAmount;
    [SerializeField] bool restoreMaxHP;
    //PP
    [Header("PP")]
    [SerializeField] int ppAmout;
    [SerializeField] bool restoreMAXPP;
    //Status condition
    [Header("Status condition")]
    [SerializeField] ConditionID status;
    [SerializeField] bool recoverAllStatus;
    //Revive
    [Header("Revive")]
    [SerializeField] bool revive;
    [SerializeField] bool maxRevive;

    public override bool Use(Pokemon pokemon)
    {
        if(hpAmount > 0)
        {
            if(pokemon.HP == pokemon.MaxHP)
            {
                return false;
            }
            pokemon.IncreaseHP(hpAmount);
        }
        return true;
    }
}
