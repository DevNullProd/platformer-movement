using UnityEngine;

// Handles PlayerMovement Collisions
partial class PlayerMovement
{
  void UpdateCollisions(){
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
           _frontWallCheckPoint.position,
           _wallCheckSize,
           0, _groundLayer
         ) && IsFacingRight) ||
        (Physics2D.OverlapBox(
           _backWallCheckPoint.position,
           _wallCheckSize,
           0, _groundLayer
         ) && !IsFacingRight);

      // Left wall overlap check
      bool leftWallOverlap =
        (Physics2D.OverlapBox(
           _frontWallCheckPoint.position,
           _wallCheckSize,
           0, _groundLayer
         ) && !IsFacingRight) ||
        (Physics2D.OverlapBox(
           _backWallCheckPoint.position,
           _wallCheckSize,
           0, _groundLayer
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
      LastOnWallTime = Mathf.Max(
        LastOnWallLeftTime,
        LastOnWallRightTime
      );
    }
  }
}
