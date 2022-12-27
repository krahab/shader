using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    void Update(){
        if (Input.GetKeyDown("q")){
            SceneManager.LoadScene("Menu", LoadSceneMode.Single);
        } 
        if(Input.GetKeyDown("r")){
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        if (Input.GetKeyDown("escape"))
        {
            Application.Quit();
        }
    }

    public void ChangeScene(string sceneName){
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
