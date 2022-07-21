using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetController : MonoBehaviour
{
    #region Variables

    //NON-SERIALIZED
    private Transform transform;

    //SERIALIZERD
    [SerializeField] private float speed = 1f;

    //REFERENCES
    private GameManager gM;
 
    #endregion

    #region Functions

    void Awake()
    {
        gM = FindObjectOfType<GameManager>();
        transform = GetComponent<Transform>(); 
    }

    // Update is called once per frame
    void Update()
    {
        if (gM.GetCurrentGameState() == GameManager.GameState.Gameplay)
        {
            MoveNets();
        }   
    }

    void MoveNets()
    {
        transform.position += transform.right * -speed * Time.deltaTime;
    }

    #endregion
}
