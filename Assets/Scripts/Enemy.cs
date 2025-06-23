using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] float _speed = 5;
    [SerializeField] EnemyScreenBounds _screenBounds;
    private bool _canRespawn = true;
    private Player _player;
    [SerializeField] int _pointValue = 100;
    [SerializeField] GameObject _explosionPrefab;
    [SerializeField] float _explosionCoverupDelay = .33f;
    [SerializeField] GameObject _enemyLaserPrefab;
    [SerializeField] GameObject _shieldVisual;
    bool _isShieldActive = false;
    Sides _dodgeDirection;
    bool _isDodging;
    WaitForSeconds _dodgeWait;
    [SerializeField] float _dodgeTime;

    private void Start()
    {
        _player = GameManager.Instance.Player;
        _screenBounds = GameManager.Instance.Bounds;
        StartCoroutine(FireLaserRoutine());
        float rndShield = Random.value;
        if (rndShield > 0.5f)
        {
            _isShieldActive = true;
            _shieldVisual.SetActive(true);
        }
        else
        {
            _isShieldActive = false;
                                        
        }
    }

    private void Update()
    {
        CalculateMovement();
    }

    private void CalculateMovement()
    {
        if (_isDodging)
            transform.Translate(Vector3.right * ((int)_dodgeDirection * _speed * Time.deltaTime));
        else
            transform.Translate(Vector3.down * _speed * Time.deltaTime);
        if (transform.position.y < _screenBounds.bottom && _canRespawn)
        {
            Respawn();
        }
        else if (transform.position.y < _screenBounds.bottom && _canRespawn == false)
        {
            Destroy(this.gameObject);
        }
    }

    private void Respawn()
    {
        float rng = Random.Range(_screenBounds.left, _screenBounds.right);
        transform.position = new Vector3(rng, _screenBounds.top, 0);
    }

    IEnumerator FireLaserRoutine()
    {
        while (_player != null)
        {
            Instantiate(_enemyLaserPrefab, transform.position, Quaternion.identity);
            float randomRefireTime = Random.Range(2, 5);
            yield return new WaitForSeconds(randomRefireTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _player.Damage();
            if (!_isShieldActive)
            {
                OnEnemyDeath();
            }
            else
            {
                _isShieldActive = false;
                _shieldVisual.SetActive(false);
            }
        }
        if (other.CompareTag("Projectile"))
        {
            
            if (other.TryGetComponent<IProjectile>(out IProjectile projectile))
            {
                //Debug.Log("Found projectile.");
                if (projectile.IsEnemyProjectile() == false)
                {
                    //Debug.Log("Is not enemy projectile!");
                    Destroy(other.gameObject);
                    if (!_isShieldActive)
                    {
                        OnEnemyDeath();
                    }
                    else
                    {
                        _isShieldActive = false;
                        _shieldVisual.SetActive(false);
                    }
                }
            }
            else
                Debug.LogError("No IProjectile script found!");
        }
    }

    private void OnEnemyDeath()
    {
        SpawnManager.Instance.OnEnemyDeath();
        _player.AddScore(_pointValue);
        Instantiate(_explosionPrefab, transform.position, Quaternion.identity);
        _speed = 0;
        GetComponent<Collider>().enabled = false;
        Destroy(this.gameObject, _explosionCoverupDelay);
    }

    public void OnPlayerDeath()
    {
        _canRespawn = false;
    }

    public void FireAtPowerup()
    {
        Instantiate(_enemyLaserPrefab, transform.position, Quaternion.identity);
    }

    public void DodgeFire(Sides dodgeDirection)
    {
        if (_isDodging == false)
        {
            _dodgeDirection = dodgeDirection;
            _isDodging = true;
            StartCoroutine(DodgeTimeRoutine());
        }
    }

    IEnumerator DodgeTimeRoutine()
    {
        yield return _dodgeWait;
        _isDodging = false;
    }
}