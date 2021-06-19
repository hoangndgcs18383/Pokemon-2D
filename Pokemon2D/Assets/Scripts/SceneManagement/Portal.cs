using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Portal : MonoBehaviour, IPlayerTriggerable
{
    [SerializeField] int sceneToLoad = -1;
    [SerializeField] DestinationIdentifier destinationPortal;
    [SerializeField] Transform spawnPoint;

    PlayerController player;
    public void OnPlayerTriggered(PlayerController player)
    {
        player.Character.Animator.isMoving = false;
        this.player = player;
        StartCoroutine(SwichScene());
    }
    Fader fader;
    private void Start()
    {
        fader = FindObjectOfType<Fader>();
    }
    IEnumerator SwichScene()
    {
        DontDestroyOnLoad(gameObject);

        GameController.Instance.PauseGame(true);
        yield return fader.FadeIn(0.5f);

        yield return SceneManager.LoadSceneAsync(sceneToLoad);

        var destPortal = FindObjectsOfType<Portal>().First(x => x != this && x.destinationPortal == this.destinationPortal);
        player.Character.SetPositionAndSnapToTile(destPortal.SpawnPoint.position);

        yield return fader.FadeOut(0.5f);
        GameController.Instance.PauseGame(false);

        Destroy(gameObject);
    }
    public Transform SpawnPoint => spawnPoint;
}
public enum DestinationIdentifier { A, B, C, D, E}
