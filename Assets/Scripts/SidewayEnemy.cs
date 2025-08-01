using UnityEngine;

public class SidewayEnemy : MonoBehaviour
{
    [SerializeField] float _speed = 5f;
    [Header("Screen Bounds")]
    [SerializeField] EnemyScreenBounds _screenBounds;
    int _directionMultiplier = 1;
    [SerializeField] GameObject _firingPoint;
    [SerializeField] GameObject _laserPrefab;
    [SerializeField] Vector3 _laserOffset;
    [SerializeField] float _fireRate = 2.5f;
    float _whenCanFire;
    Transform _laserContainer;
    GameObject _player;
    [SerializeField] int _pointValue;
    [SerializeField] GameObject _explosionPrefab;
    [SerializeField] float _explosionCoverDelay;
    float _rndY = 0;
    Vector2 _newPOS = Vector2.zero;
    [SerializeField] float _spawnOffsetX = 0.05f;

    private void Start()
    {
        ResetSpawn();
        _laserContainer = GameObject.Find("LaserContainer")?.transform;
        _player = GameManager.Instance.Player.gameObject;
    }

    private void ResetSpawn()
    {
        float rndY = Random.Range(_screenBounds.bottom, _screenBounds.top);
        _newPOS.y = _rndY;
        _newPOS.x = _screenBounds.left + _spawnOffsetX;
        transform.position = _newPOS;
       
    }

    private void Update()
    {
        CalculateMovement();
        LookAtTarget();
        if (_whenCanFire< Time.time)
        {
            FireLaser();
        }
    }

    private void CalculateMovement()
    {
        transform.Translate(Vector3.right * (_directionMultiplier * _speed * Time.deltaTime));
        if (transform.position.x < _screenBounds.left || transform.position.x > _screenBounds.right)
        {
            _directionMultiplier *= -1;
            transform.localScale = new Vector3(_directionMultiplier, 1, 1);
            _rndY = Random.Range(_screenBounds.bottom, _screenBounds.top);
            _newPOS.y = _rndY;
            _newPOS.x = transform.position.x;
            transform.position = _newPOS;
        }
    }

    private void FireLaser()
    {
        GameObject go = Instantiate(_laserPrefab, _firingPoint.transform.position + _laserOffset, _firingPoint.transform.rotation);
        go.GetComponent<Projectile>()?.AssignEnemyLaser();
        go.transform.parent = _laserContainer;
        _whenCanFire = Time.time + _fireRate;
    }

    private void LookAtTarget()
    {
        Vector3 diff = _player.transform.position - _firingPoint.transform.position;
        diff.Normalize();
        float rot_z = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        _firingPoint.transform.rotation = Quaternion.Euler(0f, 0f, rot_z - 90);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _player.GetComponent<Player>()?.Damage();
            OnEnemyDeath();
        }
        if (other.CompareTag("Projectile"))
        {
            if (!other.GetComponent<IProjectile>().IsEnemyProjectile())
            {
                Destroy(other.gameObject);
                OnEnemyDeath();
            }
        }
    }

    private void OnEnemyDeath()
    {
        SpawnManager.Instance.OnEnemyDeath();
        _player.GetComponent<Player>()?.AddScore(_pointValue);
        Instantiate(_explosionPrefab, transform.position, Quaternion.identity);
        _speed = 0;
        GetComponent<Collider>().enabled = false;
        Destroy(this.gameObject, _explosionCoverDelay);
    }
}

