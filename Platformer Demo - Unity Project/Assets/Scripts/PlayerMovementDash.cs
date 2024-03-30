using System.Collections;
using UnityEngine;

// Handles PlayerMovement Dash
partial class PlayerMovement
{
  void UpdateDashChecks(){
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
  }

  private bool CanDash(){
    if(!IsDashing &&
        _dashesLeft < Data.dashAmount &&
        LastOnGroundTime > 0 && !_dashRefilling)
      StartCoroutine(nameof(RefillDash), 1);

    return _dashesLeft > 0;
  }

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
}
