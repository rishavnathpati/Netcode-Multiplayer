using System.Text;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    private const float MovementSpeed = 10f;

    private readonly NetworkVariable<CustomData> _randomNumber = new(new CustomData
    {
        IntValue = 5,
        BoolValue = false
    }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private NetworkVariable<int> _randomInt = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
    private void Update()
    {
        if (!IsOwner) return;

        HandleMovement();
        HandleInput();
    }

    public override void OnNetworkSpawn()
    {
        _randomInt.OnValueChanged += LogIntChange;
        _randomNumber.OnValueChanged += LogCustomDataChange;
    }
    
    private void LogIntChange(int previousValue, int newValue)
    {
        Debug.Log($"Owner Client ID: {OwnerClientId}, Random Int : {newValue}");
    }

    private void LogCustomDataChange(CustomData previousValue, CustomData newValue)
    {
        Debug.Log($"Owner Client ID: {OwnerClientId}, Custom Data : {newValue.IntValue} {newValue.BoolValue}");
    }

    private void HandleMovement()
    {
        var moveInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        var moveVelocity = moveInput.normalized * MovementSpeed;
        transform.Translate(moveVelocity * Time.deltaTime);
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _randomInt.Value = Random.Range(0, 100);
            _randomNumber.Value = new CustomData
            {
                IntValue = Random.Range(0, 100),
                BoolValue = Random.Range(0, 2) == 1
            };
        }
        
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            string author = "Rishav Nath Pati";
            byte[] bytes = Encoding.ASCII.GetBytes(author);
            
            NotifyServerRpc(bytes);
        }
        
        // NotifyClientRpc(_randomNumber);
    }

    [ServerRpc]
    private void NotifyServerRpc(byte[] bytes)
    {
        // Convert a byte array to a C# string.
        
        Debug.Log($"ServerRpc called {OwnerClientId} {Encoding.ASCII.GetString(bytes)}");
    }

    // [ClientRpc]
    // private void NotifyClientRpc(NetworkVariable<CustomData> message)
    // {
    //     Debug.Log($"ClientRpc called {OwnerClientId} {message.Value} {message.Value.IntValue} {message.Value.BoolValue}");
    // }

    private struct CustomData : INetworkSerializable
    {
        public int IntValue;
        public bool BoolValue;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref IntValue);
            serializer.SerializeValue(ref BoolValue);
        }
    }
}