using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainerFov : MonoBehaviour, IPlayerTriggerable
{
    public void OnPlayerTriggered(PlayerController player)
    {
        player.Character.Animator.isMoving = false;
        GameController.Instance.OnEnterTrainerView(GetComponentInParent<TrainerController>());
    }
}
