using UnityEngine;

public class LocationManager : MonoBehaviour
{
    public static LocationManager Instance;
    public Transform kitchen, bedroom, livingroom;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }
}