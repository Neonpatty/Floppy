using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class HitController : MonoBehaviour
{
    #region Variables

    //NON-SERIALIZED
    private int pointCount;

    //SERIALIZED
    [SerializeField] private TMP_Text pointText;

    //REFERENCES
    [SerializeField] private GameManager gM;

    #endregion

    #region Functions

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Point")
        {
            pointCount++;
            Debug.Log("You got a point!");
            pointText.text = pointCount.ToString();

        }

        if (collision.tag == "Wall")
        {
            gM.EndGame();    
        }
    }

    #endregion
}
