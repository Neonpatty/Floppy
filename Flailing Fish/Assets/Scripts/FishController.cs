using UnityEngine;
using UnityEngine.InputSystem;

public class FishController : MonoBehaviour
{
    #region Variables

    //NON-SERIALIZED
    private Rigidbody2D rBody;
    private Animator flopAni;

    //SERIALIZED
    [SerializeField] private float thrust = 1f;
    [SerializeField] private float maxForce = 10f;

    //BOOLS
    private bool flopped;

    //REFRENECES
    [SerializeField] private GameManager gM;
    
    private PlayerControls playerControls;

    #endregion

    #region Functions

    private void Awake()
    {
        playerControls = new PlayerControls();
        flopAni = GetComponent<Animator>();
        rBody = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }

    public void Flail(InputAction.CallbackContext context)
    {
        if (gM.GetCurrentGameState() == GameManager.GameState.Gameplay)
        {
            rBody.AddForce(Vector2.up * thrust, ForceMode2D.Impulse);
            if (rBody.velocity.magnitude > maxForce)
            {
                rBody.velocity = Vector2.ClampMagnitude(rBody.velocity, maxForce);
            }
        }
        else
        {
            gM.PreGame();
        }
    }

    public void FlopAni(InputAction.CallbackContext context)
    {
        if (flopped)
        {
            flopAni.SetBool("Flopped", true);
            flopped = false;
        }
        else
        {
            flopAni.SetBool("Flopped", false);
            flopped = true;
        }
    }

    #endregion
}