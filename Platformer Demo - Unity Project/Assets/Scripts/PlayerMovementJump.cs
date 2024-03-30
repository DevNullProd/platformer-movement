using UnityEngine;

// Handles PlayerMovement Jump
partial class PlayerMovement
{
  void UpdateJumpChecks(){
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
}
