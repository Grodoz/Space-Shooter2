using UnityEngine;

public class Powerup : MonoBehaviour
{
    [SerializeField] private float _speed = 5f;
    [SerializeField] private float _bottomBound = -12f;
    [SerializeField] private PowerupType _powerupID;
    [SerializeField] private AudioClip _clip;
    [SerializeField] private int _powerupAmount = 5;
    Player _player;
    PlayerInputAction _inputAction;
    bool _isBeingCalled = false;

    private void Start()
    {
        _inputAction = new PlayerInputAction();
        _inputAction.Player.Enable();
        _inputAction.Player.PowerUpCollect.started += PowerupCollect_started;
        _inputAction.Player.PowerUpCollect.canceled += PowerupCollect_canceled;
        _player = GameManager.Instance.Player;
    }

    private void PowerupCollect_started(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        _isBeingCalled = true;
    }

    private void PowerupCollect_canceled(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        _isBeingCalled = false;
    }

    private void Update()
    {
        CalculateMovement();
    }

    private void CalculateMovement()
    {
        if (_isBeingCalled)
            transform.position = Vector3.MoveTowards(transform.position, _player.transform.position, (_speed * 2 * Time.deltaTime));
        else
            transform.Translate(Vector3.down * _speed * Time.deltaTime);
        if (transform.position.y <= _bottomBound)
            Destroy(this.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            switch (_powerupID)
            {
                case PowerupType.None:
                    break;
                case PowerupType.QuadShot:
                    _player.ActivateQuadShot();
                    break;
                case PowerupType.SpeedBoost:
                    _player.ActivateSpeedBoost();
                    break;
                case PowerupType.Shield:
                    _player.ShieldActive(true, 99);
                    break;
                case PowerupType.Ammo:
                    _player.AddAmmo(_powerupAmount);
                    break;
                case PowerupType.Repair:
                    _player.AddHealth();
                    break;
                case PowerupType.GatlingGun:
                    _player.ActivateGatlingGun();
                    break;
                case PowerupType.Starburster:
                    _player.ActivateStarBurster();
                    break;
                case PowerupType.ControlJam:
                    _player.ActivateJammedControls();
                    break;
                case PowerupType.HomingMissile:
                    _player.AddMissiles();
                    break;
                default:
                    break;
            }
            OnCollect();
        }
        if (other.CompareTag("Projectile"))
        {
            if (other.TryGetComponent<IProjectile>(out IProjectile projectile))
            {
                if (projectile.IsEnemyProjectile() == true)
                {
                    Destroy(other.gameObject);
                    Destroy(this.gameObject);
                }
            }
            else
                Debug.LogError("IProjecitle not found", other.gameObject);
            
        }
    }

    private void OnCollect()
    {
        AudioManager.Instance.PlaySoundAtPlayer(_clip);
        _speed = 0;
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }
        GetComponent<Collider>().enabled = false;
        Destroy(this.gameObject, .9f);
    }

    private void OnDisable()
    {
        _inputAction.Player.PowerUpCollect.started -= PowerupCollect_started;
        _inputAction.Player.PowerUpCollect.canceled -= PowerupCollect_canceled;
        _inputAction.Player.Disable();
    }

}
