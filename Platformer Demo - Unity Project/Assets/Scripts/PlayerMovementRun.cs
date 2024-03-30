using UnityEngine;

// Handles PlayerMovement input
partial class PlayerMovement
{
  void FixedUpdateRun(){
    // Handle Run
    if(!IsDashing){
      if(IsWallJumping)
        Run(Data.wallJumpRunLerp);
      else
        Run(1);

    }else if(_isDashAttacking)
      Run(Data.dashEndRunLerp);
  }

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
}
