using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    public float speed;
    public NetworkMan networkMan;
    private Vector3 moveDirection;

    // Start is called before the first frame update
    void Start()
    {
        networkMan = FindObjectOfType<NetworkMan>();
    }

    // Update is called once per frame
    void Update()
    {
        if (gameObject.name != networkMan.myAddress) { return; }

        float horizontalMovement = Input.GetAxisRaw("Horizontal") * speed * Time.deltaTime;
        float veritcalMovement = Input.GetAxisRaw("Vertical") * speed * Time.deltaTime;

        moveDirection = new Vector3(horizontalMovement, veritcalMovement);
        transform.Translate(moveDirection);
    }
}
