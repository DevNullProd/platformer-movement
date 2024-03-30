using UnityEngine;

// Handles PlayerMovement Gravity
partial class PlayerMovement
{
  void UpdateGravity(){
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
  }

  public void SetGravityScale(float scale){
    RB.gravityScale = scale;
  }
}
