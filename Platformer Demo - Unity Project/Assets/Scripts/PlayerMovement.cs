using System.Collections;
using UnityEngine;

class PlayerMovement : MonoBehaviour
{
  // Scriptable object which holds all the
  // player's movement parameters.
  public PlayerData Data;

  #region COMPONENTS
  public Rigidbody2D RB { get; private set; }

  // Script to handle all player animations,
  public PlayerAnimator AnimHandler { get; private set; }
  #endregion

  /////////////////////////////////////////////////////

  #region STATE PARAMETERS
  // Player state properties
  public bool IsFacingRight { get; private set; }
  public bool IsJumping { get; private set; }
  public bool IsWallJumping { get; private set; }
  public bool IsDashing { get; private set; }
  public bool IsSliding { get; private set; }

  // Player timers
  public float LastOnGroundTime { get; private set; }
  public float LastOnWallTime { get; private set; }
  public float LastOnWallRightTime { get; private set; }
  public float LastOnWallLeftTime { get; private set; }

  // Jump
  private bool _isJumpCut;
  private bool _isJumpFalling;

  // Wall Jump
  private float _wallJumpStartTime;
  private int _lastWallJumpDir;

  // Dash
  private int _dashesLeft;
  private bool _dashRefilling;
  private Vector2 _lastDashDir;
  private bool _isDashAttacking;
  #endregion

  /////////////////////////////////////////////////////

  #region INPUT PARAMETERS
  private Vector2 _moveInput;

  public float LastPressedJumpTime { get; private set; }
  public float LastPressedDashTime { get; private set; }
  #endregion

  /////////////////////////////////////////////////////

  #region CHECK PARAMETERS
  // Set all of these up in the inspector
  [Header("Checks")] 

  [SerializeField]
  private Transform _groundCheckPoint;

  // Size of groundCheck depends on the size
  // of your character. Generally you want them
  // slightly small than width (for ground)
  // and height (for the wall check)
  [SerializeField]
  private Vector2 _groundCheckSize =
    new Vector2(0.49f, 0.03f);

  [Space(5)]
  [SerializeField]
  private Transform _frontWallCheckPoint;

  [SerializeField]
  private Transform _backWallCheckPoint;

  [SerializeField]
  private Vector2 _wallCheckSize =
    new Vector2(0.5f, 1f);
  #endregion

  /////////////////////////////////////////////////////

  #region LAYERS & TAGS
  [Header("Layers & Tags")]

  [SerializeField]
  private LayerMask _groundLayer;
  #endregion

  /////////////////////////////////////////////////////

  private void Awake(){
    RB = GetComponent<Rigidbody2D>();
    AnimHandler = GetComponent<PlayerAnimator>();
  }

  private void Start(){
    SetGravityScale(Data.gravityScale);
    IsFacingRight = true;
  }

  private void Update(){
    #region TIMERS
    LastOnGroundTime -= Time.deltaTime;
    LastOnWallTime -= Time.deltaTime;
    LastOnWallRightTime -= Time.deltaTime;
    LastOnWallLeftTime -= Time.deltaTime;

    LastPressedJumpTime -= Time.deltaTime;
    LastPressedDashTime -= Time.deltaTime;
    #endregion

    ///////////////////////////////////////

    #region INPUT HANDLER
    _moveInput.x = Input.GetAxisRaw("Horizontal");
    _moveInput.y = Input.GetAxisRaw("Vertical");

    if (_moveInput.x != 0)
      CheckDirectionToFace(_moveInput.x > 0);

    if(Input.GetKeyDown(KeyCode.Space) ||
       Input.GetKeyDown(KeyCode.C) ||
       Input.GetKeyDown(KeyCode.J))
      OnJumpInput();

    if(Input.GetKeyUp(KeyCode.Space) ||
       Input.GetKeyUp(KeyCode.C) ||
       Input.GetKeyUp(KeyCode.J))
      OnJumpUpInput();

    if(Input.GetKeyDown(KeyCode.X) ||
       Input.GetKeyDown(KeyCode.LeftShift) ||
       Input.GetKeyDown(KeyCode.K))
      OnDashInput();
    #endregion

    ///////////////////////////////////////

    #region COLLISION CHECKS
    if(!IsDashing && !IsJumping){
      // Ground overlap check
      bool groundOverlap = Physics2D.OverlapBox(
        _groundCheckPoint.position,
        _groundCheckSize,
        0,
        _groundLayer
      );

      // Right wall overlap check
      bool rightWallOverlap =
        (Physics2D.OverlapBox(
           _frontWallCheckPoint.position, _wallCheckSize, 0, _groundLayer
         ) && IsFacingRight) ||
        (Physics2D.OverlapBox(
           _backWallCheckPoint.position, _wallCheckSize, 0, _groundLayer
         ) && !IsFacingRight);

      // Left wall overlap check
      bool leftWallOverlap =
        (Physics2D.OverlapBox(
           _frontWallCheckPoint.position, _wallCheckSize, 0, _groundLayer
         ) && !IsFacingRight) ||
        (Physics2D.OverlapBox(
           _backWallCheckPoint.position, _wallCheckSize, 0, _groundLayer
        ) && IsFacingRight);

      if(groundOverlap){
        if(LastOnGroundTime < -0.1f)
          AnimHandler.justLanded = true;

        // Set the lastGrounded to coyoteTime
        LastOnGroundTime = Data.coyoteTime;
      }    

      if(rightWallOverlap && !IsWallJumping)
        LastOnWallRightTime = Data.coyoteTime;

      if(leftWallOverlap && !IsWallJumping)
        LastOnWallLeftTime = Data.coyoteTime;

      // Two checks needed for both left and right
      // walls since whenever the play turns the wall
      // checkPoints swap sides
      LastOnWallTime = Mathf.Max(LastOnWallLeftTime, LastOnWallRightTime);
    }
    #endregion

    ///////////////////////////////////////

    #region JUMP CHECKS
    if(IsJumping && RB.velocity.y < 0){
      IsJumping = false;
      _isJumpFalling = true;
    }

    if(IsWallJumping && Time.time - _wallJumpStartTime > Data.wallJumpTime)
      IsWallJumping = false;

    if(LastOnGroundTime > 0 && !IsJumping && !IsWallJumping){
      _isJumpCut = false;
      _isJumpFalling = false;
    }

    if(!IsDashing){
      // Jump
      if(CanJump() && LastPressedJumpTime > 0){
        IsJumping = true;
        IsWallJumping = false;
        _isJumpCut = false;
        _isJumpFalling = false;
        Jump();

        AnimHandler.startedJumping = true;
      }

      // Wall Jump
      else if(CanWallJump() && LastPressedJumpTime > 0){
        IsWallJumping = true;
        IsJumping = false;
        _isJumpCut = false;
        _isJumpFalling = false;

        _wallJumpStartTime = Time.time;
        _lastWallJumpDir = (LastOnWallRightTime > 0) ? -1 : 1;

        WallJump(_lastWallJumpDir);
      }
    }
    #endregion

    ///////////////////////////////////////

    #region DASH CHECKS
    if(CanDash() && LastPressedDashTime > 0){
      // Freeze game for split second. Adds juiciness and a bit
      // of forgiveness over directional input
      Sleep(Data.dashSleepTime); 

      // If not direction pressed, dash forward
      if(_moveInput != Vector2.zero)
        _lastDashDir = _moveInput;
      else
        _lastDashDir = IsFacingRight ?
          Vector2.right : Vector2.left;

      IsDashing = true;
      IsJumping = false;
      IsWallJumping = false;
      _isJumpCut = false;

      StartCoroutine(nameof(StartDash), _lastDashDir);
    }
    #endregion

    ///////////////////////////////////////

    #region SLIDE CHECKS
    bool leftSlide = LastOnWallLeftTime > 0 && _moveInput.x < 0;
    bool rightSlide = LastOnWallRightTime > 0 && _moveInput.x > 0;
    if(CanSlide() && (leftSlide || rightSlide))
      IsSliding = true;
    else
      IsSliding = false;
    #endregion

    ///////////////////////////////////////

    #region GRAVITY
    if(!_isDashAttacking){
      // Higher gravity if we've released the jump input or are falling
      if(IsSliding){
        SetGravityScale(0);

      }else if(RB.velocity.y < 0 && _moveInput.y < 0){
        // Much higher gravity if holding down
        SetGravityScale(Data.gravityScale * Data.fastFallGravityMult);

        // Cap maximum fall speed, so when falling over large distances
        // we don't accelerate to insanely high speeds
        RB.velocity = new Vector2(
          RB.velocity.x,
          Mathf.Max(RB.velocity.y, -Data.maxFastFallSpeed)
        );

      }else if(_isJumpCut){
        // Higher gravity if jump button released
        SetGravityScale(Data.gravityScale * Data.jumpCutGravityMult);
        RB.velocity = new Vector2(
          RB.velocity.x,
          Mathf.Max(RB.velocity.y, -Data.maxFallSpeed)
        );

      }else if((IsJumping || IsWallJumping || _isJumpFalling) &&
        Mathf.Abs(RB.velocity.y) < Data.jumpHangTimeThreshold){
        SetGravityScale(Data.gravityScale * Data.jumpHangGravityMult);

      }else if (RB.velocity.y < 0){
        //Higher gravity if falling
        SetGravityScale(Data.gravityScale * Data.fallGravityMult);

        // Cap maximum fall speed, so when falling over large distances
        // we don't accelerate to insanely high speeds
        RB.velocity = new Vector2(
          RB.velocity.x,
          Mathf.Max(RB.velocity.y, -Data.maxFallSpeed)
        );

      //Default gravity if standing on a platform or moving upwards
      }else
        SetGravityScale(Data.gravityScale);

    // No gravity when dashing (returns to normal once initial
    // dashAttack phase over)
    }else
      SetGravityScale(0);
    #endregion
  }

  private void FixedUpdate(){
    // Handle Run
    if(!IsDashing){
      if(IsWallJumping)
        Run(Data.wallJumpRunLerp);
      else
        Run(1);

    }else if(_isDashAttacking)
      Run(Data.dashEndRunLerp);

    // Handle Slide
    if(IsSliding)
      Slide();
  }

  /////////////////////////////////////////////////////

  #region INPUT CALLBACKS
  //Methods which whandle input detected in Update()
  public void OnJumpInput(){
    LastPressedJumpTime = Data.jumpInputBufferTime;
  }

  public void OnJumpUpInput(){
    if (CanJumpCut() || CanWallJumpCut())
      _isJumpCut = true;
  }

  public void OnDashInput(){
    LastPressedDashTime = Data.dashInputBufferTime;
  }
  #endregion

  /////////////////////////////////////////////////////

  #region GENERAL METHODS
  public void SetGravityScale(float scale)
  {
    RB.gravityScale = scale;
  }

  // Method used so we don't need to call
  // StartCoroutine everywhere.
  private void Sleep(float duration){
    StartCoroutine(nameof(PerformSleep), duration);
  }

  // Must be Realtime since timeScale with be 0 
  private IEnumerator PerformSleep(float duration)
  {
    Time.timeScale = 0;
    yield return new WaitForSecondsRealtime(duration);
    Time.timeScale = 1;
  }
  #endregion

  /////////////////////////////////////////////////////

  //MOVEMENT METHODS
  #region RUN METHODS
  private void Run(float lerpAmount)
  {
    // Direction we want to move in and our desired velocity
    float targetSpeed = _moveInput.x * Data.runMaxSpeed;

    // Smooth changes to are direction and speed
    targetSpeed = Mathf.Lerp(
      RB.velocity.x, targetSpeed, lerpAmount
    );

    ///////////////////////////////////////

    #region Calculate AccelRate
    float accelRate;

    // Get an acceleration value based on if we are
    // accelerating (includes turning) or trying to decelerate (stop).
    // As well as applying a multiplier if we're air borne.
    if(LastOnGroundTime > 0)
      accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ?
        Data.runAccelAmount : Data.runDeccelAmount;
    else
      accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ?
        Data.runAccelAmount * Data.accelInAir :
        Data.runDeccelAmount * Data.deccelInAir;
    #endregion

    ///////////////////////////////////////

    #region Add Bonus Jump Apex Acceleration
    // Increase are acceleration and maxSpeed when at the
    // apex of their jump, makes the jump feel a bit more bouncy,
    // responsive and natural
    if((IsJumping || IsWallJumping || _isJumpFalling) &&
       Mathf.Abs(RB.velocity.y) < Data.jumpHangTimeThreshold){
      accelRate *= Data.jumpHangAccelerationMult;
      targetSpeed *= Data.jumpHangMaxSpeedMult;
    }
    #endregion

    ///////////////////////////////////////

    #region Conserve Momentum
    // We won't slow the player down if they are moving in their
    // desired direction but at a greater speed than their maxSpeed
    bool xFaster = Mathf.Abs(RB.velocity.x) > Mathf.Abs(targetSpeed);
    bool xSameDir = Mathf.Sign(RB.velocity.x) == Mathf.Sign(targetSpeed);
    bool isMoving = Mathf.Abs(targetSpeed) > 0.01f;
    bool inAir = LastOnGroundTime < 0;
    if(Data.doConserveMomentum && xFaster && xSameDir && isMoving && inAir){
      // Prevent any deceleration from happening, or in other
      // words conserve are current momentum. You could experiment
      // with allowing for the player to slightly increae their speed
      // whilst in this "state".
      accelRate = 0; 
    }
    #endregion

    ///////////////////////////////////////

    // Calculate difference between current velocity and desired velocity
    float speedDif = targetSpeed - RB.velocity.x;

    // Calculate force along x-axis to apply to thr player
    float movement = speedDif * accelRate;

    // Convert this to a vector and apply to rigidbody
    RB.AddForce(movement * Vector2.right, ForceMode2D.Force);
  }

  private void Turn(){
    // Flip the player along the x axis, 
    Vector3 scale = transform.localScale; 
    scale.x *= -1;
    transform.localScale = scale;

    IsFacingRight = !IsFacingRight;
  }
  #endregion

  /////////////////////////////////////////////////////

  #region JUMP METHODS
  private void Jump(){
    // Ensures we can't call Jump multiple times from one press
    LastPressedJumpTime = 0;
    LastOnGroundTime = 0;

    #region Perform Jump
    // Increase the force applied if we are falling
    // This means we'll always feel like we jump the same amount 
    // (setting the player's Y velocity to 0 beforehand will likely
    //  work the same, but I find this more elegant :D)
    float force = Data.jumpForce;
    if (RB.velocity.y < 0)
      force -= RB.velocity.y;

    RB.AddForce(Vector2.up * force, ForceMode2D.Impulse);
    #endregion
  }

  private void WallJump(int dir){
    // Ensures we can't call Wall Jump multiple times from one press
    LastPressedJumpTime = 0;
    LastOnGroundTime = 0;
    LastOnWallRightTime = 0;
    LastOnWallLeftTime = 0;

    #region Perform Wall Jump
    // Apply force in opposite direction of wall
    Vector2 force = new Vector2(Data.wallJumpForce.x, Data.wallJumpForce.y);
    force.x *= dir;

    if(Mathf.Sign(RB.velocity.x) != Mathf.Sign(force.x))
      force.x -= RB.velocity.x;

    // Checks whether player is falling, if so we subtract the velocity.y
    // (counteracting force of gravity). This ensures the player always
    // reaches our desired jump force or greater
    if(RB.velocity.y < 0)
      force.y -= RB.velocity.y;

    // Unlike in the run we want to use the Impulse mode.
    // The default mode will apply are force instantly ignoring masss
    RB.AddForce(force, ForceMode2D.Impulse);
    #endregion
  }
  #endregion

  /////////////////////////////////////////////////////

  #region DASH METHODS
  //Dash Coroutine
  private IEnumerator StartDash(Vector2 dir)
  {
    // Overall this method of dashing aims to mimic Celeste,
    // if you're looking for a more physics-based approach
    // try a method similar to that used in the jump

    LastOnGroundTime = 0;
    LastPressedDashTime = 0;

    float startTime = Time.time;

    _dashesLeft--;
    _isDashAttacking = true;

    SetGravityScale(0);

    // Keep the player's velocity at the dash speed during
    // the "attack" phase (in celeste the first 0.15s)
    while(Time.time - startTime <= Data.dashAttackTime){
      RB.velocity = dir.normalized * Data.dashSpeed;

      // Pauses the loop until the next frame, creating
      // something of a Update loop. This is a cleaner implementation
      // opposed to multiple timers and this coroutine approach is
      // actually what is used in Celeste :D
      yield return null;
    }

    startTime = Time.time;
    _isDashAttacking = false;

    // Begins the "end" of our dash where we return some control
    // to the player but still limit run acceleration
    // (see Update() and Run())
    SetGravityScale(Data.gravityScale);
    RB.velocity = Data.dashEndSpeed * dir.normalized;

    while(Time.time - startTime <= Data.dashEndTime){
      yield return null;
    }

    // Dash over
    IsDashing = false;
  }

  // Short period before the player is able to dash again
  private IEnumerator RefillDash(int amount){
    // Cooldown, so we can't constantly dash along the ground,
    // again this is the implementation in Celeste.
    _dashRefilling = true;
    yield return new WaitForSeconds(Data.dashRefillTime);

    _dashRefilling = false;
    _dashesLeft = Mathf.Min(Data.dashAmount, _dashesLeft + 1);
  }
  #endregion

  /////////////////////////////////////////////////////

  #region OTHER MOVEMENT METHODS
  private void Slide(){
    // Remove the remaining upwards Impulse to prevent upwards sliding
    if(RB.velocity.y > 0)
      RB.AddForce(-RB.velocity.y * Vector2.up,ForceMode2D.Impulse);

    // Works the same as the Run but only in the y-axis
    // This seems to work fine, buit maybe you'll find
    // a better way to implement a slide into this system
    float speedDif = Data.slideSpeed - RB.velocity.y;  
    float movement = speedDif * Data.slideAccel;

    // Clamp the movement here to prevent any over corrections
    // (these aren't noticeable in the Run).
    // The force applied can't be greater than the (negative) speedDifference *
    // by how many times a second FixedUpdate() is called.
    movement = Mathf.Clamp(movement,
      -Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime),
      Mathf.Abs(speedDif) * (1 / Time.fixedDeltaTime)
    );

    RB.AddForce(movement * Vector2.up);
  }
  #endregion

  /////////////////////////////////////////////////////

  #region CHECK METHODS
  public void CheckDirectionToFace(bool isMovingRight){
    if(isMovingRight != IsFacingRight)
      Turn();
  }

  private bool CanJump(){
    return LastOnGroundTime > 0 && !IsJumping;
  }

  private bool CanWallJump(){
    return LastPressedJumpTime > 0 &&
      LastOnWallTime > 0 &&
      LastOnGroundTime <= 0 &&
      (!IsWallJumping ||
       (LastOnWallRightTime > 0 && _lastWallJumpDir == 1) ||
       (LastOnWallLeftTime > 0 && _lastWallJumpDir == -1));
  }

  private bool CanJumpCut(){
    return IsJumping && RB.velocity.y > 0;
  }

  private bool CanWallJumpCut(){
    return IsWallJumping && RB.velocity.y > 0;
  }

  private bool CanDash(){
    if(!IsDashing &&
        _dashesLeft < Data.dashAmount &&
        LastOnGroundTime > 0 && !_dashRefilling)
      StartCoroutine(nameof(RefillDash), 1);

    return _dashesLeft > 0;
  }

  public bool CanSlide(){
    return LastOnWallTime > 0 &&
        !IsJumping &&
        !IsWallJumping &&
        !IsDashing &&
        LastOnGroundTime <= 0;
  }
  #endregion

  /////////////////////////////////////////////////////

  #region EDITOR METHODS
  private void OnDrawGizmosSelected()
  {
    Gizmos.color = Color.green;
    Gizmos.DrawWireCube(_groundCheckPoint.position, _groundCheckSize);
    Gizmos.color = Color.blue;
    Gizmos.DrawWireCube(_frontWallCheckPoint.position, _wallCheckSize);
    Gizmos.DrawWireCube(_backWallCheckPoint.position, _wallCheckSize);
  }
  #endregion
}
