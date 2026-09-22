using UnityEngine;

public class IsLookAt : MonoBehaviour
{
    [SerializeField]
    bool m_IsLookAt;
    Renderer m_Renderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_Renderer = GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (m_Renderer.isVisible)
        {
            Debug.Log("Object is visible");
            m_IsLookAt = true;
        } else
        {
            Debug.Log("Object is no longer visible");
            m_IsLookAt = false;
        }
         
    }


    private void OnBecameVisible()
    {
        m_IsLookAt = true;
        Debug.Log("see cube");
    }

    private void OnBecameInvisible()
    {
        m_IsLookAt= false;
        Debug.Log("no cube");
        
    }
}
