using System.Collections;
using UnityEngine;

// PlayerMovement Helpers
partial class PlayerMovement
{
  void UpdateTimers(){
    LastOnGroundTime -= Time.deltaTime;
    LastOnWallTime -= Time.deltaTime;
    LastOnWallRightTime -= Time.deltaTime;
    LastOnWallLeftTime -= Time.deltaTime;

    LastPressedJumpTime -= Time.deltaTime;
    LastPressedDashTime -= Time.deltaTime;
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
}
