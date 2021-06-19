using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Teleports the player to a different position without swithcing scenes
public class LocationPortal : MonoBehaviour, IPlayerTriggerable
{
    [SerializeField] DestinationIdentifier destinationPortal;
    [SerializeField] Transform spawnPoint;

    PlayerController player;
    public void OnPlayerTriggered(PlayerController player)
    {
        player.Character.Animator.isMoving = false;
        this.player = player;
        StartCoroutine(Teleport());
    }
    Fader fader;
    private void Start()
    {
        fader = FindObjectOfType<Fader>();
    }
    IEnumerator Teleport()
    {
        GameController.Instance.PauseGame(true);
        yield return fader.FadeIn(0.5f);

        var destPortal = FindObjectsOfType<LocationPortal>().First(x => x != this && x.destinationPortal == this.destinationPortal);
        player.Character.SetPositionAndSnapToTile(destPortal.SpawnPoint.position);

        yield return fader.FadeOut(0.5f);
        GameController.Instance.PauseGame(false);
    }
    public Transform SpawnPoint => spawnPoint;
}
