using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStateMachine : MonoBehaviourPun
{
    private PlayerControls _playerControls;
    private float turnVelocity;

    [Header("Player Name")]
    public string PlayerName;
    public TextMeshPro PlayerNameText;

    [Header("Character Controller")]
    public CharacterController characterController;

    [Header("Camera")]
    public Camera cam;

    [Header("Player Animator")]
    public Animator animator;
    public AnimatorOverrideController baseOverrideController;
    [HideInInspector] public AnimatorOverrideController runtimeOverride;

    [Header("Parameters")]
    public float Speed = 5f;
    public float TurnVelocity = 0.1f;
    public float gravity = -9.81f;
    public float jumpHeight = 3f;
    public float groundDistance = 0.1f;
    public Transform groundCheck;
    public LayerMask gLayer;

    [Header("Enemy and Player Layer")]
    public LayerMask EnemyLayer;
    public LayerMask PlayerLayer;

    [Header("Attack Data")]
    public List<AttackData> attacks;

    [Header("Attack Origins")]
    public List<AttackOriginEntry> attackOrigins = new();
    public Dictionary<string, Transform> attackOriginMap = new();

    private Dictionary<string, AttackData> attackMap;
    private Dictionary<string, float> cooldowns;

    [Header("Grab Parameters")]
    public float grabRadius = 1.2f;
    public float grabDuration = 2.5f;
    public float grabCooldown = 10f;
    public LayerMask pushableLayer;
    public Transform grabPoint;
    [HideInInspector] public float grabCooldownTimer = 0f;
    private bool isGrabbing = false;

    [Header("Power Ups")]
    public GameObject bubbleShieldEffect;
    public bool isShieldActive = false;
    public float bubbleShieldDuration = 10f;
    public float airPullDistance = 10f;
    public float airPullDuration = 0.4f;
    [HideInInspector] public bool isDashing = false;

    [Header("Inputs")]
    public Vector2 moveInput;
    public Vector3 velocity;
    public bool isJumpPressed;
    public bool isGrounded;
    public bool isAttacking;

    [Header("!!Testing_Parry!!")]
    public bool isBlockHeld;
    public bool isBlockJustPressed;
    public bool isParryWindowOpen = false;
    public bool wasParried = false;
    private float parryWindowStartTime;
    [SerializeField] private float parryWindowDuration = 0.5f;

    private PlayerBaseState currentState;
    private PlayerStateFactory stateFactory;

    // Audio tracking
    private AudioClip lastBGMClip;

    void Awake()
    {
        InitializeControls();
        cam = Camera.main;
        runtimeOverride = new AnimatorOverrideController(baseOverrideController);
        animator.runtimeAnimatorController = runtimeOverride;

        SetUpAttackMaps();
        stateFactory = new PlayerStateFactory(this);
        currentState = stateFactory.Idle();
        currentState.EnterState();

        if (photonView.IsMine)
        {
            cam = Camera.main;
            enabled = true;
        }
        else
        {
            enabled = false;
        }

        int targetLayer = photonView.IsMine ? LayerMask.NameToLayer("Player") : LayerMask.NameToLayer("Enemy");
        SetLayerRecursively(gameObject, targetLayer);

        // Initial BGM state
        lastBGMClip = AudioManager.Instance?.bgmMenu;
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    [PunRPC]
    public void SetPlayerName(string _name)
    {
        PlayerName = _name;
        PlayerNameText.text = PlayerName;
    }

    void SetUpAttackMaps()
    {
        attackMap = new Dictionary<string, AttackData>();
        cooldowns = new Dictionary<string, float>();

        foreach (var atk in attacks)
        {
            attackMap[atk.inputActionName] = atk;
            cooldowns[atk.inputActionName] = 0f;
            var inputAction = _playerControls.Attack.Get().FindAction(atk.inputActionName);
            if (inputAction != null)
                inputAction.performed += ctx => OnAttackInput(atk);
        }

        foreach (var entry in attackOrigins)
        {
            if (!attackOriginMap.ContainsKey(entry.originName))
                attackOriginMap[entry.originName] = entry.originTransform;
        }
    }

    private void OnAttackInput(AttackData atk)
    {
        if (Time.time >= cooldowns[atk.inputActionName])
        {
            cooldowns[atk.inputActionName] = Time.time + atk.cooldown;
            SwitchState(stateFactory.Attack(atk));
        }
    }

    void InitializeControls()
    {
        _playerControls = new PlayerControls();
        _playerControls.Movement.Jump.performed += ctx => isJumpPressed = true;
        _playerControls.Grab.GrabMouse.performed += OnGrabPressed;
        _playerControls.PowerUps.Shield.performed += ActivateShield;
        _playerControls.PowerUps.PullThroughAir.performed += ActivatePullThroughAir;

        _playerControls.Parry.Parry.performed += ctx =>
        {
            isBlockHeld = true;
            isBlockJustPressed = true;
            if (!(currentState is PlayerDefenseState))
            {
                SwitchState(stateFactory.Defense());
            }
        };
        _playerControls.Parry.Parry.canceled += ctx =>
        {
            isBlockHeld = false;
        };
    }

    private void HandleIncomingAttacks(GameObject attacker, GameObject target, AttackData data)
    {
        if (target != gameObject) return;

        wasParried = false;
        TriggerParryWindow();

        if (isParryWindowOpen && isBlockJustPressed && !isBlockHeld)
        {
            wasParried = true;
        }
    }

    public void TriggerParryWindow()
    {
        parryWindowStartTime = Time.time;
        isParryWindowOpen = true;
    }

    private void OnEnable()
    {
        AttackEvents.OnIncomingAttack += HandleIncomingAttacks;
        _playerControls.Enable();
    }

    private void OnDisable()
    {
        AttackEvents.OnIncomingAttack -= HandleIncomingAttacks;
        _playerControls.Disable();
    }

    void OnGrabPressed(InputAction.CallbackContext ctx)
    {
        isGrabbing = ctx.ReadValueAsButton();
        if (isGrabbing)
        {
            SwitchState(stateFactory.Grab());
        }
    }

    void ActivateShield(InputAction.CallbackContext ctx)
    {
        SwitchState(stateFactory.PowerUp(PowerUpType.BubbleShield, bubbleShieldDuration));
    }

    void ActivatePullThroughAir(InputAction.CallbackContext ctx)
    {
        SwitchState(stateFactory.PowerUp(PowerUpType.PullThroughAir, airPullDuration));
    }

    void Update()
    {
        UpdateGroundStatus();
        ApplyGravity();
        HandleRotation();
        currentState.UpdateState();

        moveInput = _playerControls.Movement.Keyboard.ReadValue<Vector2>();
        if (isJumpPressed)
        {
            currentState.HandleJumpInput();
            isJumpPressed = false;
        }

        if (isParryWindowOpen)
        {
            float elapsed = Time.time - parryWindowStartTime;
            if (elapsed > parryWindowDuration)
            {
                isParryWindowOpen = false;
            }
            else if (isBlockJustPressed)
            {
                SwitchState(stateFactory.ParrySub());
                isParryWindowOpen = false;
            }
        }

        isBlockJustPressed = false;

        // 🔊 AUDIO MANAGEMENT
        if (photonView.IsMine && moveInput.magnitude > 0.1f && !isAttacking)
        {
            AudioClip walkingClip = AudioManager.Instance?.walkSound;
            if (walkingClip != null && lastBGMClip != walkingClip)
            {
                AudioManager.Instance.PlayBGM(walkingClip);
                lastBGMClip = walkingClip;
            }
        }
        else
        {
            AudioClip inGameClip = AudioManager.Instance?.bgmInGame;
            if (inGameClip != null && lastBGMClip != inGameClip)
            {
                AudioManager.Instance.PlayBGM(inGameClip);
                lastBGMClip = inGameClip;
            }
        }
    }

    public void SwitchState(PlayerBaseState newState)
    {
        currentState.ExitState();
        currentState = newState;
        newState.EnterState();
    }

    public void HandleRotation()
    {
        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.transform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnVelocity, TurnVelocity);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }
    }

    public Vector3 GetMoveDirection(Vector3 direction)
    {
        float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.transform.eulerAngles.y;
        Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        return moveDir.normalized;
    }

    void UpdateGroundStatus()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, gLayer);
    }

    void ApplyGravity()
    {
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    public void ApplyJumpVelocity()
    {
        velocity.y = Mathf.Sqrt(-2f * gravity * jumpHeight);
        AudioManager.Instance?.PlayJump();
    }

    public void ApplyAttackDamage()
    {
        if (currentState is PlayerAttackState atk)
            atk.ApplyDamage();
    }

    public void EndAttack()
    {
        if (currentState is PlayerAttackState atk)
            SwitchState(stateFactory.Idle());
    }

    public void EnableShield(bool active)
    {
        isShieldActive = active;
        if (bubbleShieldEffect != null)
        {
            bubbleShieldEffect.SetActive(active);
        }
    }

    public void PullPlayerThroughAir()
    {
        if (isDashing) return;

        Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        if (inputDirection == Vector3.zero)
            inputDirection = Vector3.forward;

        Vector3 moveDir = GetMoveDirection(inputDirection);
        transform.rotation = Quaternion.LookRotation(moveDir);
        StartCoroutine(PerformAirPull(moveDir));
    }

    private IEnumerator PerformAirPull(Vector3 direction)
    {
        isDashing = true;

        float elapsed = 0f;
        Vector3 start = transform.position;
        Vector3 target = start + direction * airPullDistance;

        bool wasGrounded = isGrounded;
        velocity.y = 0f;

        while (elapsed < airPullDuration)
        {
            float t = elapsed / airPullDuration;
            Vector3 newPosition = Vector3.Lerp(start, target, t);
            Vector3 delta = newPosition - transform.position;
            characterController.Move(delta);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Vector3 finalDelta = target - transform.position;
        characterController.Move(finalDelta);

        isDashing = false;

        if (!wasGrounded)
            velocity.y = 0f;
    }

    public void ForceGroundCheck()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, gLayer);
    }
}
