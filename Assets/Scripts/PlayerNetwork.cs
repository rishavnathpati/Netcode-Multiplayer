
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    private NetworkVariable<int> _randomNumber= new(1);
    void Update()
    {
        Debug.Log(_randomNumber.Value);
        
        if(!IsOwner) return;
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _randomNumber.Value = Random.Range(1, 100);
        }
        
        Vector3 moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        
        Vector3 moveVelocity = moveInput.normalized * 10;
        transform.Translate(moveVelocity * Time.deltaTime);
    }
}
