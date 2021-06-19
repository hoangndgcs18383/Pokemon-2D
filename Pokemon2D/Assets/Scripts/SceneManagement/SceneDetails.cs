using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneDetails : MonoBehaviour
{
    [SerializeField] List<SceneDetails> connectedScenes;
    public bool IsLoaded { get; private set; }
    List<SavableEntity> savableEntities;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "Player")
        {
            Debug.LogWarning($"Enter {gameObject.name}");

            LoadScene();
            GameController.Instance.SetCurrentScene(this);
            
            //Load all connected scenes
            foreach (var scene in connectedScenes)
            {
                scene.LoadScene();
            }
            //Unload the scenes that or no longer connected
            var prevScene = GameController.Instance.PreveScene;
            if (prevScene != null)
            {
                var previoslyLoadScenes = prevScene.connectedScenes;
                foreach (var scene in previoslyLoadScenes)
                {
                    if(!connectedScenes.Contains(scene) && scene != this)
                    {
                        scene.UnLoadScene();
                    }
                }

                if(!connectedScenes.Contains(prevScene))
                    prevScene.UnLoadScene();
            }
        }
    }
    public void LoadScene()
    {
        if (!IsLoaded)
        {
            var operation = SceneManager.LoadSceneAsync(gameObject.name, LoadSceneMode.Additive);
            IsLoaded = true;

            operation.completed += (AsyncOperation op) =>
            {
                savableEntities = GetSavableEntitiesInScene();
                SavingSystem.i.RestoreEntityStates(savableEntities);
            };
        }
    }
    public void UnLoadScene()
    {
        if (IsLoaded)
        {
            SavingSystem.i.CaptureEntityStates(savableEntities);

            SceneManager.UnloadSceneAsync(gameObject.name);
            IsLoaded = false;
        }
    }

    List<SavableEntity> GetSavableEntitiesInScene()
    {
        var currScene = SceneManager.GetSceneByName(gameObject.name);
        var savableEntities = FindObjectsOfType<SavableEntity>().Where(x => x.gameObject.scene == currScene).ToList();
        return savableEntities;
    }
}

