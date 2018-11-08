using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;
using UnityEngine.UI;
using TMPro;

namespace UnityStandardAssets.Characters.FirstPerson
{
    [RequireComponent(typeof (CharacterController))]
    [RequireComponent(typeof (AudioSource))]
    public class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private bool m_IsWalking;
        [SerializeField] private float m_WalkSpeed;
        [SerializeField] private float m_RunSpeed;
        [SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten;
        [SerializeField] private float m_JumpSpeed;
        [SerializeField] private float m_StickToGroundForce;
        [SerializeField] private float m_GravityMultiplier;
        [SerializeField] private MouseLook m_MouseLook;
        [SerializeField] private bool m_UseFovKick;
        [SerializeField] private FOVKick m_FovKick = new FOVKick();
        [SerializeField] private bool m_UseHeadBob;
        [SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
        [SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
        [SerializeField] private float m_StepInterval;
        [SerializeField] private AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
        [SerializeField] private AudioClip m_JumpSound;           // the sound played when character leaves the ground.
        [SerializeField] private AudioClip m_LandSound;           // the sound played when character touches back on ground.

        private Camera m_Camera;
        private bool m_Jump;
        private float m_YRotation;
        private Vector2 m_Input;
        private Vector3 m_MoveDir = Vector3.zero;
        private CharacterController m_CharacterController;
        private CollisionFlags m_CollisionFlags;
        private bool m_PreviouslyGrounded;
        private Vector3 m_OriginalCameraPosition;
        private float m_StepCycle;
        private float m_NextStep;
        private bool m_Jumping;
        private AudioSource m_AudioSource;

        public Text instructionText;
        public InputField codeInput;
        public GameObject chest;
        public GameObject axe;
        private bool hasAxe;
        private bool hasEraser;
        private bool boardErased;
        private bool bookPulled;
        public GameObject door;
        public TextMeshPro blackboardText;
        public GameObject bookshelf;
        public GameObject ladder;
        public GameObject eraser;
        public GameObject book;
        public GameObject key;
        public GameObject candle;
        public GameObject fire;

        //0 = main, 1 = note, 2 = chest, 3 = door
        public int locationIndex;

        // Use this for initialization
        private void Start()
        {
            m_CharacterController = GetComponent<CharacterController>();
            m_Camera = Camera.main;
            m_OriginalCameraPosition = m_Camera.transform.localPosition;
            m_FovKick.Setup(m_Camera);
            m_HeadBob.Setup(m_Camera, m_StepInterval);
            m_StepCycle = 0f;
            m_NextStep = m_StepCycle/2f;
            m_Jumping = false;
            m_AudioSource = GetComponent<AudioSource>();
			m_MouseLook.Init(transform , m_Camera.transform);

            instructionText.text = "";
            locationIndex = 0;
            codeInput.gameObject.SetActive(false);
            axe.gameObject.SetActive(false);
            fire.gameObject.SetActive(false);

            hasAxe = false;
            hasEraser = false;
            boardErased = false;
            bookPulled = false;

        }


        // Update is called once per frame
        private void Update()
        {
            RotateView();
            // the jump state needs to read here to make sure it is not missed
            if (!m_Jump)
            {
                m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
            }

            if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
            {
                StartCoroutine(m_JumpBob.DoBobCycle());
                PlayLandingSound();
                m_MoveDir.y = 0f;
                m_Jumping = false;
            }
            if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
            {
                m_MoveDir.y = 0f;
            }

            m_PreviouslyGrounded = m_CharacterController.isGrounded;

            if (Input.GetMouseButtonDown(0) && locationIndex == 1) {
                instructionText.text = "Code is 2625";
            } else if (Input.GetMouseButtonDown(0) && locationIndex == 2) {
                codeInput.gameObject.SetActive(true);
                codeInput.ActivateInputField();
                instructionText.text = "";
            } else if (Input.GetMouseButtonDown(0) && locationIndex == 3) {
                Destroy(axe.gameObject);
                instructionText.text = "";
                locationIndex = 0;
                hasAxe = true;
            } else if (Input.GetMouseButtonDown(0) && locationIndex == 4 && hasAxe) {
                instructionText.text = "Onto the next room";
            }
            else if (Input.GetMouseButtonDown(0) && locationIndex == 5 )
            {
                Destroy(eraser.gameObject);
                hasEraser = true;
                instructionText.text = "";
            }
            else if (Input.GetMouseButtonDown(0) && locationIndex == 6)
            {
                boardErased = true;
                blackboardText.text = "B            L           U         E                                              B              O     O  K       ";
                instructionText.text = "";
            }
            else if (Input.GetMouseButtonDown(0) && locationIndex == 7)
            {
                bookPulled = true;
                instructionText.text = "";
            }
            else if (Input.GetMouseButtonDown(0) && locationIndex == 8)
            {
                instructionText.text = "Onto the next room";
            }
            else if (Input.GetMouseButtonDown(0) && locationIndex == 9)
            {
                Destroy(key.gameObject);
                candle.transform.rotation = Quaternion.AngleAxis(90, Vector3.left);
                fire.gameObject.SetActive(true);

            }
        }


        private void PlayLandingSound()
        {
            m_AudioSource.clip = m_LandSound;
            m_AudioSource.Play();
            m_NextStep = m_StepCycle + .5f;
        }


        private void FixedUpdate()
        {
            float speed;
            GetInput(out speed);
            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = transform.forward*m_Input.y + transform.right*m_Input.x;

            // get a normal for the surface that is being touched to move along it
            RaycastHit hitInfo;
            Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                               m_CharacterController.height/2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

            m_MoveDir.x = desiredMove.x*speed;
            m_MoveDir.z = desiredMove.z*speed;


            if (m_CharacterController.isGrounded)
            {
                m_MoveDir.y = -m_StickToGroundForce;

                if (m_Jump)
                {
                    m_MoveDir.y = m_JumpSpeed;
                    PlayJumpSound();
                    m_Jump = false;
                    m_Jumping = true;
                }
            }
            else
            {
                m_MoveDir += Physics.gravity*m_GravityMultiplier*Time.fixedDeltaTime;
            }
            m_CollisionFlags = m_CharacterController.Move(m_MoveDir*Time.fixedDeltaTime);

            ProgressStepCycle(speed);
            UpdateCameraPosition(speed);

            m_MouseLook.UpdateCursorLock();
        }


        private void PlayJumpSound()
        {
            m_AudioSource.clip = m_JumpSound;
            m_AudioSource.Play();
        }


        private void ProgressStepCycle(float speed)
        {
            if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
            {
                m_StepCycle += (m_CharacterController.velocity.magnitude + (speed*(m_IsWalking ? 1f : m_RunstepLenghten)))*
                             Time.fixedDeltaTime;
            }

            if (!(m_StepCycle > m_NextStep))
            {
                return;
            }

            m_NextStep = m_StepCycle + m_StepInterval;

            PlayFootStepAudio();
        }


        private void PlayFootStepAudio()
        {
            if (!m_CharacterController.isGrounded)
            {
                return;
            }
            // pick & play a random footstep sound from the array,
            // excluding sound at index 0
            int n = Random.Range(1, m_FootstepSounds.Length);
            m_AudioSource.clip = m_FootstepSounds[n];
            m_AudioSource.PlayOneShot(m_AudioSource.clip);
            // move picked sound to index 0 so it's not picked next time
            m_FootstepSounds[n] = m_FootstepSounds[0];
            m_FootstepSounds[0] = m_AudioSource.clip;
        }


        private void UpdateCameraPosition(float speed)
        {
            Vector3 newCameraPosition;
            if (!m_UseHeadBob)
            {
                return;
            }
            if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
            {
                m_Camera.transform.localPosition =
                    m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
                                      (speed*(m_IsWalking ? 1f : m_RunstepLenghten)));
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
            }
            else
            {
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
            }
            m_Camera.transform.localPosition = newCameraPosition;
        }


        private void GetInput(out float speed)
        {
            // Read input
            float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
            float vertical = CrossPlatformInputManager.GetAxis("Vertical");

            bool waswalking = m_IsWalking;

#if !MOBILE_INPUT
            // On standalone builds, walk/run speed is modified by a key press.
            // keep track of whether or not the character is walking or running
            m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
#endif
            // set the desired speed to be walking or running
            speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;
            m_Input = new Vector2(horizontal, vertical);

            // normalize input if it exceeds 1 in combined length:
            if (m_Input.sqrMagnitude > 1)
            {
                m_Input.Normalize();
            }

            // handle speed change to give an fov kick
            // only if the player is going to a run, is running and the fovkick is to be used
            if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
            {
                StopAllCoroutines();
                StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
            }
        }


        private void RotateView()
        {
            m_MouseLook.LookRotation (transform, m_Camera.transform);
        }


        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;
            //dont move the rigidbody if the character is on top of it
            if (m_CollisionFlags == CollisionFlags.Below)
            {
                return;
            }

            if (body == null || body.isKinematic)
            {
                return;
            }
            body.AddForceAtPosition(m_CharacterController.velocity*0.1f, hit.point, ForceMode.Impulse);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Code Note")) {
                instructionText.text = "Read Note";
                locationIndex = 1;
            } else if (other.gameObject.CompareTag("Code Chest"))
            {
                instructionText.text = "Click to enter passcode";
                locationIndex = 2;
            } else if (other.gameObject.CompareTag("Axe")) {
                instructionText.text = "Click to pick up axe";
            } else if (other.gameObject.CompareTag("Door")) {
                instructionText.text = "Click to break down door";
                locationIndex = 4;
            }
            else if (other.gameObject.CompareTag("Eraser"))
            {
                instructionText.text = "Click to pick up eraser";
                locationIndex = 5;
            }
            else if (other.gameObject.CompareTag("Blackboard") && hasEraser)
            {
                instructionText.text = "Click to erase board";
                locationIndex = 6;
            }
            else if (other.gameObject.CompareTag("Bookshelf") && boardErased)
            {
                instructionText.text = "Click to take blue book";
                locationIndex = 7;
            }
            else if (other.gameObject.CompareTag("Ladder") && bookPulled)
            {
                instructionText.text = "Click to climb";
                locationIndex = 8;
            }
            else if (other.gameObject.CompareTag("Key"))
            {
                instructionText.text = "Click to take key";
                locationIndex = 9;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            instructionText.text = "";
            locationIndex = 0;
        }

        public void enterCode(String code) {
            instructionText.text = "";
            if (string.Equals(code, "2625")) {
                Destroy(codeInput.gameObject);
                Destroy(chest.gameObject);
                codeInput.gameObject.SetActive(false);
                locationIndex = 3;
                instructionText.text = "Click to pick up axe";
                axe.gameObject.SetActive(true);
            } else {

            }
        }
    }
}
