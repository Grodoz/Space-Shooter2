using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Player Stats")]
    [SerializeField] private float _speed = 5f;
    private Vector3 _position;
    private float _horizontalInput;
    private float _verticalInput;
    private Vector3 _direction;
    private int _score;
    [Header("Screenplay Bounds")]
    [SerializeField] private float _topBounds;
    [SerializeField] private float _bottomBounds;
    [SerializeField] private float _leftBounds;
    [SerializeField] private float _rightBounds;
    [Header("Laser Settings")]
    [SerializeField] private GameObject _laserPrefab;
    [SerializeField] private Vector3 _laserOffset;
    [SerializeField] private float _fireRate = 2.5f;
    private float _whenCanFire = -1;
    [SerializeField] private Transform _laserContainer;
    [SerializeField] private AudioClip _laserAudioClip;
    [SerializeField] private int _maxAmmoCount = 30;
    int _ammoCount = 50;
    [SerializeField] FireType _fireType = FireType.Regular;
    [Header("Quad Shot Settings")]
    [SerializeField] GameObject _quadShotPrefab;
    bool _isQuadShotActive;
    [Header("Gatling Gun Settings")]
    [SerializeField] Transform[] _gatlingPoints;
    bool _isGatlingActive;
    int _gatlingPointCounter;
    int _currentPoint = 0;
    [SerializeField] float _gatlingFireRate = .1f;
    [SerializeField] GameObject _starBursterPrefab;
    [SerializeField] float _starBursterFireRate = 5f;
    bool _isStarBursterActive;
    [Header("Homing Missile")]
    [SerializeField] GameObject _homingMissilePrefab;
    int _homingMissileCount = 0;
    [SerializeField] float _missileFireRate = 5;
    private SpawnManager _spawnManager;
    private Coroutine _QuadPowerTimerRoutine;
    private Coroutine _SpeedBoostTimerRoutine;
    private int _health = 3;
    [SerializeField] private Transform _shieldVisuals;
    [SerializeField] private bool _isShieldActive;
    private int _currentShieldStrength = 0;
    [SerializeField] private int _maxShieldStrength = 3;
    [SerializeField] private ShieldVisualization _shieldVisualization;
    [SerializeField] private GameObject[] _damageVisualization;
    [SerializeField] private float _speedBoostMultiplier = 2;
    private float _boostedMultiplier = 1;
    [SerializeField] private float _thrustMultiplier = 2;
    private float _thrustingMultiplier = 1;
    private float _jammedControlsMultiplier = 1;
    private bool _isThrustActive = false;
    private float _engineHeat = 0;
    [SerializeField] private float _heatingRate = 2;
    [SerializeField] private float _coolingRate = 1.5f;
    [SerializeField] private float _maxEngineHeat = 100;
    private bool _isEngineOverheated = false;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip[] _audioClips;
    private bool _wasHitThisFrame = false;
    private CameraManager _cameraManager;
    [SerializeField] float _cameraShakeDuration = .5f;
    [SerializeField] float _cameraShakeIntensity = .5f;
    WaitForSeconds _shortWait;
    WaitForSeconds _midWait;
    WaitForSeconds _longWait;
    [SerializeField] float _shortWaitTime = 1f;
    [SerializeField] float _midWaitTime = 5f;
    [SerializeField] float _longWaitTime = 10f;
    GameObject _missile;
    GameObject[] _missileTargets;
    Transform _closestMissileTarget;
    float _closestMissileDistance = Mathf.Infinity;
    float _checkingDistance;
    PlayerInputAction _inputAction;
    Vector2 _movement = Vector2.zero;
    bool _isFireBeingHeld = false;
    bool _isLeftShiftHeld = false;

    private void Start()
    {
        InitializeInput();
        _spawnManager = GameObject.FindAnyObjectByType<SpawnManager>();
        _cameraManager = Camera.main.GetComponent<CameraManager>();
        if (_spawnManager == null)
        {
            Debug.LogError("SpawnManager is NULL!", this);
        }
        ShieldActive(_isShieldActive, _currentShieldStrength);
        UIManager.Instance.UpdateScore(_score);
        UIManager.Instance.UpdateThruster(_engineHeat);
        UIManager.Instance.UpdateAmmoCount(_ammoCount);
        _shortWait = new WaitForSeconds(_shortWaitTime);
        _midWait = new WaitForSeconds(_midWaitTime);
        _longWait = new WaitForSeconds(_longWaitTime);
    }

    private void InitializeInput()
    {
        _inputAction = new PlayerInputAction();
        _inputAction.Player.Enable();
        _inputAction.Player.Fire.started += Fire_started;
        _inputAction.Player.Fire.canceled += Fire_canceled;
        _inputAction.Player.ThrusterShift.started += ThrusterShift_started;
        _inputAction.Player.ThrusterShift.canceled += ThrusterShift_canceled;
    }

    private void ThrusterShift_started(InputAction.CallbackContext obj)
    {
        _isLeftShiftHeld = true;
    }

    private void ThrusterShift_canceled(InputAction.CallbackContext obj)
    {
        _isLeftShiftHeld = false;
    }

    private void Fire_canceled(InputAction.CallbackContext obj)
    {
        _isFireBeingHeld = false;
    }

    private void Fire_started(InputAction.CallbackContext obj)
    {
        _isFireBeingHeld = true;
    }

    private void OnDisable()
    {
        _inputAction.Player.Fire.started -= Fire_started;
        _inputAction.Player.Fire.canceled -= Fire_canceled;
        _inputAction.Player.ThrusterShift.started -= ThrusterShift_started;
        _inputAction.Player.ThrusterShift.canceled -= ThrusterShift_canceled;
        _inputAction.Player.Disable();
    }

    private void Update()
    {
        _wasHitThisFrame = false;
        ThrusterCalculation();
        CalculateMovement();
        if (_isFireBeingHeld && _whenCanFire < Time.time)
        {
            FireLaser();
        }
    }

    private void ThrusterCalculation()
    {
        if (_engineHeat <= _maxEngineHeat && !_isEngineOverheated)
        {
            if (_isLeftShiftHeld)
            {
                _thrustingMultiplier = _thrustMultiplier;
                _isThrustActive = true;
            }
            else
            {
                _thrustingMultiplier = 1;
                _isThrustActive = false;
            }
        }
        else if (_engineHeat > _maxEngineHeat)
        {
            _engineHeat = _maxEngineHeat;
            _thrustingMultiplier = 1;
            _isThrustActive = false;
            _isEngineOverheated = true;
        }
        if (_engineHeat < 0)
        {
            _engineHeat = 0;
            _isEngineOverheated = false;
        }
        if (_isThrustActive && _engineHeat < _maxEngineHeat)
        {
            _engineHeat += _heatingRate * Time.deltaTime;
        }
        if (!_isThrustActive && _engineHeat > 0)
        {
            _engineHeat -= _coolingRate * Time.deltaTime;
        }
        UIManager.Instance.UpdateThruster(_engineHeat);
    }

    private void CalculateMovement()
    {
        _movement = _inputAction.Player.Movement.ReadValue<Vector2>();
        _direction = _movement;
        _direction.z = 0;
        transform.Translate(_direction * (_speed * _boostedMultiplier * _thrustingMultiplier * Time.deltaTime) * _jammedControlsMultiplier);
        if (_direction != Vector3.zero)
            BoundsCheck();
    }

    private void BoundsCheck()
    {
        _position = transform.position;
        if (_position.y < _bottomBounds)
            _position.y = _bottomBounds;
        if (_position.y > _topBounds)
            _position.y = _topBounds;
        if (_position.x < _leftBounds)
            _position.x = _rightBounds;
        if (_position.x > _rightBounds)
            _position.x = _leftBounds;
        transform.position = _position;
    }

    private void FireLaser()
    {
        if (_ammoCount > 0)
        {
            switch (_fireType)
            {
                case FireType.Regular:
                    Instantiate(_laserPrefab, transform.position + _laserOffset, Quaternion.identity, _laserContainer);
                    break;
                case FireType.QuadShot:
                    Instantiate(_quadShotPrefab, transform.position, Quaternion.identity, _laserContainer);
                    break;
                case FireType.GatlingGun:
                    _currentPoint = _gatlingPointCounter % _gatlingPoints.Length;
                    Instantiate(_laserPrefab, _gatlingPoints[_currentPoint].position,  Quaternion.identity, _laserContainer);
                    _gatlingPointCounter++;
                    break;
                case FireType.Starburster:
                    Instantiate(_starBursterPrefab, transform.position + _laserOffset, Quaternion.identity, _laserContainer);
                    break;
                case FireType.HomingMissile:
                    if (_homingMissileCount >= 1)
                        HomingMissileFire();
                    if (_homingMissileCount <= 0)
                        _fireType = FireType.Regular;
                    break;
                default:
                    break;
            }
            AudioManager.Instance.PlaySoundAtPlayer(_laserAudioClip);
            if (_isGatlingActive == false)
                _ammoCount--;
            UIManager.Instance.UpdateAmmoCount(_ammoCount);
        }
        else
        {
            if (_audioClips.Length > 0)
            {
                int rng = Random.Range(0, _audioClips.Length);
                _audioSource.clip = _audioClips[rng];
                _audioSource.Play();
            }
        }
        switch (_fireType)
        {
            case FireType.GatlingGun:
                _whenCanFire = Time.time + _gatlingFireRate;
                break;
            case FireType.Starburster:
                _whenCanFire = Time.time + _starBursterFireRate;
                break;
            case FireType.HomingMissile:
                _whenCanFire = Time.time + _missileFireRate;
                break;
            default:
                _whenCanFire = Time.time + _fireRate;
                break;

        }
    }

    private void HomingMissileFire()
    {
        _missile = Instantiate(_homingMissilePrefab, transform.position, Quaternion.identity);
        _missileTargets = GameObject.FindGameObjectsWithTag("Enemy");
        if (_missileTargets.Length > 0)
        {
            _closestMissileTarget = null;
            _closestMissileDistance = Mathf.Infinity;
            foreach (GameObject enemy in _missileTargets)
            {
                _checkingDistance = Vector3.Distance(enemy.transform.position, transform.position);
                if (_closestMissileDistance > _checkingDistance)
                {
                    _closestMissileDistance = _checkingDistance;
                    _closestMissileTarget = enemy.transform;
                }
            }
            _missile.GetComponent<HomingMissile>()?.AssignTarget(_closestMissileTarget);
        }
        _missile.transform.LookAt(transform.position + Vector3.up);
        _homingMissileCount--;
    }
    public void Damage()
    {
        if (_wasHitThisFrame)
            return;
        _wasHitThisFrame = true;
        if (_isShieldActive)
        {
            _currentShieldStrength--;
            _shieldVisualization.ApplyStrength(_currentShieldStrength);
            if (_currentShieldStrength <= 0)
            {
                _isShieldActive = false;
                ShieldActive(false, 0);
            }
            return;
        }
        _health--;
        UIManager.Instance.UpdateLives(_health);
        _cameraManager.CameraShake(_cameraShakeDuration, _cameraShakeIntensity);
        if (_health == 2)
        {
            int rng = Random.Range(0, _damageVisualization.Length);
            _damageVisualization[rng].SetActive(true);
        }
        if (_health == 1)
        {
            if (_damageVisualization[0].activeInHierarchy == false)
            {
                _damageVisualization[0].SetActive(true);
            }
            else
            {
                _damageVisualization[1].SetActive(true);
            }
        }
        if (_health <= 0)
        {
            _spawnManager.OnPlayerDeath();
            transform.GetChild(0).gameObject.SetActive(false);
            transform.GetChild(1).gameObject.SetActive(false);
            _damageVisualization[0].SetActive(false);
            _damageVisualization[1].SetActive(false);
            this.enabled = false;
        }
    }

    public void ActivateQuadShot()
    {
        _fireType = FireType.QuadShot;
        if (_QuadPowerTimerRoutine == null)
        {
            _QuadPowerTimerRoutine = StartCoroutine(QuadShotPowerDownRoutine());
        }
        else
        {
            StopCoroutine(_QuadPowerTimerRoutine);
            _QuadPowerTimerRoutine = StartCoroutine(QuadShotPowerDownRoutine());
        }
        if (_ammoCount <= 5)
            AddAmmo(5);
    }

    IEnumerator QuadShotPowerDownRoutine()
    {
        yield return _midWait;
        _fireType = FireType.Regular;
        _QuadPowerTimerRoutine = null;
    }

    public void ShieldActive(bool isActive, int strength)
    {
        _shieldVisuals.gameObject.SetActive(isActive);
        _isShieldActive = isActive;
        _currentShieldStrength = strength;
        if (_currentShieldStrength > _maxShieldStrength)
            _currentShieldStrength = _maxShieldStrength;
        _shieldVisualization.ApplyStrength(_currentShieldStrength);
    }

    public void ActivateSpeedBoost()
    {
        _boostedMultiplier = _speedBoostMultiplier;
        if (_SpeedBoostTimerRoutine == null)
        {
            _SpeedBoostTimerRoutine = StartCoroutine(SpeedBoostPowerDownRoutine());
        }
        else
        {
            StopCoroutine(_SpeedBoostTimerRoutine);
            _SpeedBoostTimerRoutine = StartCoroutine(SpeedBoostPowerDownRoutine());
        }
    }

    IEnumerator SpeedBoostPowerDownRoutine()
    {
        yield return _midWait;
        _boostedMultiplier = 1;
        _SpeedBoostTimerRoutine = null;
    }

    public void AddScore(int points)
    {
        _score += points;
        UIManager.Instance.UpdateScore(_score);
    }

    public void RestoreHealth()
    {
        if (_health == 2)
        {
            int RNG = Random.Range(0, _damageVisualization.Length);
            _damageVisualization[RNG].SetActive(false);
        }
        else
        {
            if (_damageVisualization[0].activeInHierarchy == true)
                _damageVisualization[0].SetActive(false);
            else
                _damageVisualization[1].SetActive(false);
        }
        UIManager.Instance.UpdateLives(_health);
    }

    public void AddAmmo(int amount)
    {
        _ammoCount += amount;
        if (_ammoCount > _maxAmmoCount)
            _ammoCount = _maxAmmoCount;
        UIManager.Instance.UpdateAmmoCount(_ammoCount);
    }

    public void AddHealth()
    {
        _health++;
        if (_health > 3)
            _health = 3;
        RestoreHealth();
    }

    public void ActivateGatlingGun()
    {
        _fireType = FireType.GatlingGun;
        _gatlingPointCounter = 0;
        StartCoroutine(GatlingShutdownRoutine());
        if (_ammoCount <= 5)
            AddAmmo(5);
    }

    IEnumerator GatlingShutdownRoutine()
    {
        yield return _midWait;
        _fireType = FireType.Regular;
    }

    public void ActivateStarBurster()
    {
        _fireType = FireType.Starburster;
        StartCoroutine(StarBursterPowerDownRoutine());
        if (_ammoCount <= 5)
            AddAmmo(5);
    }

    IEnumerator StarBursterPowerDownRoutine()
    {
        yield return _longWait;
        _fireType = FireType.Regular;
    }

    public void ActivateJammedControls()
    {
        _jammedControlsMultiplier = -1;
        StartCoroutine(JammedControlsCooldownRoutine());
    }

    IEnumerator JammedControlsCooldownRoutine()
    {
        yield return _midWait;
        _jammedControlsMultiplier = 1;
    }

    public void AddMissiles()
    {
        _homingMissileCount += 5;
        _fireType = FireType.HomingMissile;
    }

}
