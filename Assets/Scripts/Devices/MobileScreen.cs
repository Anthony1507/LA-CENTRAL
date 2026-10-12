using UnityEngine;

public class MobileScreen : MonoBehaviour
{
    GameObject activePanel;

    void Start()
    {
        GameObject panel = transform.GetChild(0).gameObject;
        foreach (Transform child in panel.transform)
        {
            child.gameObject.SetActive(false);
        }
        gameObject.SetActive(false);
    }
    public void ChangePanel(GameObject newPanel)
    {
        if (activePanel != null)
        {
            activePanel.SetActive(false);
        }
        activePanel = newPanel;
        if (activePanel != null)
        {
            activePanel.SetActive(true);
        }
    }
}
