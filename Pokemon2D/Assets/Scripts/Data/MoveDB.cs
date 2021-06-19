using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveDB
{
    static Dictionary<string, MoveBase> moves;

    public static void Init()
    {
        moves = new Dictionary<string, MoveBase>();

        var moveArray = Resources.LoadAll<MoveBase>("");
        foreach (var move in moveArray)
        {
            if (moves.ContainsKey(move.Name))
            {
                Debug.LogError($"There is two pokemon with the same move {move.Name}");
                continue;
            }

            moves[move.Name] = move;
        }
    }
    public static MoveBase GetPokemonByName(string name)
    {
        if (!moves.ContainsKey(name))
        {
            Debug.LogError($"Mive with name {name} not found in the database");
            return null;
        }
        return moves[name];
    }
}
