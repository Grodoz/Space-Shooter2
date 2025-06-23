using UnityEngine;

public class Explosion : MonoBehaviour
{
    [SerializeField] private float _animationTime;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void DestroyObject()
    {
        Destroy(this.gameObject);
    }
}
