using UnityEngine;

// Handles PlayerMovement Slide
partial class PlayerMovement
{
  void UpdateSlideChecks(){
    bool leftSlide = LastOnWallLeftTime > 0 && _moveInput.x < 0;
    bool rightSlide = LastOnWallRightTime > 0 && _moveInput.x > 0;
    if(CanSlide() && (leftSlide || rightSlide))
      IsSliding = true;
    else
      IsSliding = false;
  }

  void FixedUpdateSlide(){
    // Handle Slide
    if(IsSliding)
      Slide();
  }

  public bool CanSlide(){
    return LastOnWallTime > 0 &&
        !IsJumping &&
        !IsWallJumping &&
        !IsDashing &&
        LastOnGroundTime <= 0;
  }

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
}
