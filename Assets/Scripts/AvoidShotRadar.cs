using System.Collections;
using UnityEngine;

public class AvoidShotRadar : MonoBehaviour
{
    [SerializeField] Enemy _parentBody;
    [SerializeField] Sides _moveDirection;
    [SerializeField] float _detectionDelay = 2.5f;
    bool _canDetect = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Projectile") && _canDetect)
        {
            Projectile laser = other.GetComponent<Projectile>();
            if (laser != null && !laser.IsEnemyProjectile())
            {
                _parentBody.DodgeFire(_moveDirection);
                StartCoroutine(DetectionCooldownRoutine());
            }
        }
    }

    IEnumerator DetectionCooldownRoutine()
    {
        _canDetect = false;
        yield return new WaitForSeconds(_detectionDelay);
        _canDetect = true;
    }
}
