using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    private const float MovementSpeed = 10f;
    private const int SampleRate = 44100 / 2;
    private const int MaxRecordingTime = 10;
    private const int ChunkSize = 1024;
    
    [SerializeField] Transform spawnObjectPrefab;

    private readonly NetworkVariable<int> _randomInt = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private readonly NetworkVariable<CustomData> _randomNumber = new(
        new CustomData { IntValue = 5, BoolValue = false },
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private AudioClip _clip;
    private int _lastSample;

    private readonly List<byte[]> audioChunks = new();
    [SerializeField] private Camera PlayerCamera;

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
        
        // If this is not the local player, return
        if (!IsOwner) return;
        
        // If this is the local player, enable the camera
        PlayerCamera.transform.gameObject.SetActive(true);
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
            StartRecording();
        }
        else if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            StopRecording();
            SendAudio();
        }
        
        if(Input.GetKeyDown(KeyCode.Alpha2))
        {
            RequestSpawnClientRpc();
        }
    }
    
    [ServerRpc]
    public void RequestSpawnServerRpc()
    {
        if (IsOwner)
        {
            var spawnObject = Instantiate(spawnObjectPrefab, transform.position, Quaternion.identity);
            spawnObject.position += new Vector3(UnityEngine.Random.Range(-5, 5), 0, UnityEngine.Random.Range(-5, 5));
            spawnObject.GetComponent<NetworkObject>().Spawn(true);
        }
        
    }
    
    [ClientRpc]
    public void RequestSpawnClientRpc()
    {
        RequestSpawnServerRpc();
    }

    private void StartRecording()
    {
        _clip = Microphone.Start(null, true, MaxRecordingTime, SampleRate);
    }

    private void StopRecording()
    {
        if (Microphone.IsRecording(null))
        {
            // Save the current microphone device position
            _lastSample = Microphone.GetPosition(null);

            // Stop recording
            Microphone.End(null);
        }
    }

    private void LogIntChange(int previousValue, int newValue)
    {
        Debug.Log($"Owner Client ID: {OwnerClientId}, Random Int : {newValue}");
    }

    private void LogCustomDataChange(CustomData previousValue, CustomData newValue)
    {
        Debug.Log($"Owner Client ID: {OwnerClientId}, Custom Data : {newValue.IntValue} {newValue.BoolValue}");
    }

    [ServerRpc]
    private void SendAudioToServerRpc(byte[] audioBytes)
    {
        Debug.Log($"Audio chunk received from client {OwnerClientId}. Length: {audioBytes.Length} bytes");

        // Save the received audio chunk
        audioChunks.Add(audioBytes);

        // If the size of the chunk is less than the maximum chunk size, this is the last chunk
        if (audioBytes.Length < ChunkSize)
        {
            // Concatenate all audio chunks into a single byte array
            var totalSize = audioChunks.Sum(chunk => chunk.Length);
            var allAudioBytes = new byte[totalSize];
            var offset = 0;
            foreach (var chunk in audioChunks)
            {
                Buffer.BlockCopy(chunk, 0, allAudioBytes, offset, chunk.Length);
                offset += chunk.Length;
            }

            // Convert the byte array to an AudioClip
            var clip = ToAudioClip(allAudioBytes);

            // Play the AudioClip
            var audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.Play();

            // Clear the audio chunks for the next audio message
            audioChunks.Clear();
        }
    }

    private AudioClip ToAudioClip(byte[] audioBytes)
    {
        // Convert the byte array back into a float array
        var samples = new float[audioBytes.Length / 4];
        Buffer.BlockCopy(audioBytes, 0, samples, 0, audioBytes.Length);

        // Create the AudioClip
        var clip = AudioClip.Create("ServerAudio", samples.Length, 1, SampleRate, false);
        clip.SetData(samples, 0);

        return clip;
    }


    private void SendAudio()
    {
        if (_clip == null)
        {
            Debug.LogError("No audio clip to send");
            return;
        }

        var samples = new float[_lastSample];
        _clip.GetData(samples, 0);

        // Convert float array to byte array
        var audioBytes = new byte[samples.Length * 4];
        Buffer.BlockCopy(samples, 0, audioBytes, 0, audioBytes.Length);

        // Send the audio data in chunks
        for (var i = 0; i < audioBytes.Length; i += ChunkSize)
        {
            var chunk = new byte[Mathf.Min(ChunkSize, audioBytes.Length - i)];
            Array.Copy(audioBytes, i, chunk, 0, chunk.Length);
            SendAudioToServerRpc(chunk);
        }
    }


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