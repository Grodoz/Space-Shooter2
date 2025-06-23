using System.Collections;
using UnityEngine;

public class PickupRadar : MonoBehaviour
{
    [SerializeField] Enemy _parentBody;
    [SerializeField] float _detectionDelay = 2.5f;
    bool _canDetect = true;
    WaitForSeconds _detectionPlayWait;

    private void Start()
    {
        _detectionPlayWait = new WaitForSeconds(_detectionDelay);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Powerup>(out Powerup powerup) && _canDetect == true)
        {
            _parentBody.FireAtPowerup();
            StartCoroutine(DetectionCooldownRoutine());
        }
    }

    IEnumerator DetectionCooldownRoutine()
    {
        _canDetect = false;
        yield return _detectionPlayWait;
        _canDetect = true;
    }
}
