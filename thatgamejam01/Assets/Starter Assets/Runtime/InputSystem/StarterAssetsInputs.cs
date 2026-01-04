using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;
		public bool interact;
		public bool callLight;

		[Header("Movement Settings")]
		public bool analogMovement;
        public bool movementEnabled = true; // Added for Rescue Mechanic

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM
		public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector2>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

		public void OnJump(InputValue value)
		{
			JumpInput(value.isPressed);
		}

		public void OnSprint(InputValue value)
		{
			SprintInput(value.isPressed);
		}
		
		public void OnInteract(InputValue value)
		{
			// 当按键按下时，interact 变为 true；松开变为 false
			interact = value.isPressed;
		}
		
		public void OnCallLight(InputValue value)
		{
			// 当按键按下时，interact 变为 true；松开变为 false
			callLight = value.isPressed;
		}
#endif


		public void MoveInput(Vector2 newMoveDirection)
		{
            if (movementEnabled)
			    move = newMoveDirection;
            else
                move = Vector2.zero;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
            if (movementEnabled)
			    jump = newJumpState;
            else 
                jump = false;
		}

		public void SprintInput(bool newSprintState)
		{
            // Allow tracking sprint state even if movement is disabled
            // This ensures resume-to-sprint works if key is held
			sprint = newSprintState;
		}
		
		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
	
}