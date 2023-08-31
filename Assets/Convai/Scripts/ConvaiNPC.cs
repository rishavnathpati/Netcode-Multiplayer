using System;
using System.Collections;

using Convai.gRPCAPI;

using UnityEngine;
using UnityEngine.SceneManagement;

using Grpc.Core;
using Service;

using TMPro;

using System.Collections.Generic;
using ReadyPlayerMe;
using static ConvaiNPC;
using UnityEngine.Android;

// This script uses gRPC for streaming and is a work in progress
// Edit this script directly to customize your intelligent NPC character
[RequireComponent(typeof(Animator), typeof(AudioSource))]
public class ConvaiNPC : MonoBehaviour
{
    // do not edit
    [HideInInspector]
    public List<GetResponseResponse> getResponseResponses = new List<GetResponseResponse>();

    public string sessionID = "-1";

    List<ResponseAudio> ResponseAudios = new List<ResponseAudio>();

    public class ResponseAudio
    {
        public AudioClip audioClip;
        public string audioTranscript;
    };

    [SerializeField] public string CharacterID;
    [SerializeField] public string CharacterName;

    private AudioSource audioSource;
    private Animator characterAnimator;
    private VoiceHandler voiceHandler;

    private ConvaiRPMLipSync convaiRPMLipSync;
    private ConvaiChatUIHandler convaiChatUIHandler;

    bool animationPlaying = false;

    bool playingStopLoop = false;

    private Channel channel;
    private ConvaiService.ConvaiServiceClient client;

    private const int AUDIO_SAMPLE_RATE = 44100;
    private const string GRPC_API_ENDPOINT = "stream.convai.com";

    private int recordingFrequency = AUDIO_SAMPLE_RATE;
    private int recordingLength = 30;

    private ConvaiGRPCAPI grpcAPI;

    [SerializeField] public bool isCharacterActive;
    [SerializeField] bool enableTestMode;
    [SerializeField] string testUserQuery;

    public bool isCharacterListening = false;

    private void Awake()
    {
        grpcAPI = FindObjectOfType<ConvaiGRPCAPI>();
        convaiChatUIHandler = FindObjectOfType<ConvaiChatUIHandler>();

        audioSource = GetComponent<AudioSource>();
        characterAnimator = GetComponent<Animator>();

        if (GetComponent<VoiceHandler>())
        {
            voiceHandler = GetComponent<VoiceHandler>();
        }

        if (GetComponent<ConvaiRPMLipSync>() != null)
        {
            convaiRPMLipSync = GetComponent<ConvaiRPMLipSync>();
        }
    }

    private void Start()
    {
        StartCoroutine(playAudioInOrder());

#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
#endif

            // do not edit
            #region GRPC_SETUP
            SslCredentials credentials = new SslCredentials();

        // The IP Address could be down
        channel = new Channel(GRPC_API_ENDPOINT, credentials);

        client = new ConvaiService.ConvaiServiceClient(channel);
        #endregion
    }

    public void StartListening()
    {
        grpcAPI.StartRecordAudio(client, recordingFrequency, recordingLength, CharacterID, enableTestMode, testUserQuery);
    }

    public void StopListening()
    {
        grpcAPI.StopRecordAudio();
    }

    private void Update()
    {
        // this block starts and stops audio recording and processing
        if (isCharacterActive)
        {
            // Start recording when the left control is pressed
            if (Input.GetKeyDown(KeyCode.Space))
            {
                // Start recording audio
                StartListening();
            }

            // Stop recording once left control is released and saves the audio file locally
            if (Input.GetKeyUp(KeyCode.Space))
            {
                StopListening();
            }
        }

        if (Input.GetKey(KeyCode.R) && Input.GetKey(KeyCode.Equals))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        if (Input.GetKey(KeyCode.Escape) && Input.GetKey(KeyCode.Equals))
        {
            Application.Quit();
        }

        // if there is some audio in the queue, play it next
        // essentially make a playlist
        if (getResponseResponses.Count > 0)
        {
            ProcessResponseAudio(getResponseResponses[0]);
            getResponseResponses.Remove(getResponseResponses[0]);
        }

        // if any audio is playing, play the talking animation
        if (ResponseAudios.Count > 0)
        {
            if (animationPlaying == false)
            {
                // enable animation according to response
                // try talking first, then base it on the response
                animationPlaying = true;

                characterAnimator.SetBool("Talk", true);
            }
        }
        else
        {
            if (animationPlaying == true)
            {
                // deactivate animations to idle
                animationPlaying = false;

                characterAnimator.SetBool("Talk", false);
            }
        }
    }

    /// <summary>
    ///     When the response list has more than one elements, then the audio will be added to a playlist. This function adds individual responses to the list.
    /// </summary>
    /// <param name="getResponseResponse">The getResponseResponse object that will be processed to add the audio and transcript to the playlist</param>
    void ProcessResponseAudio(GetResponseResponse getResponseResponse)
    {
        if (isCharacterActive)
        {
            string tempString = "";

            if (getResponseResponse.AudioResponse.TextData != null)
                tempString = getResponseResponse.AudioResponse.TextData;

            byte[] byteAudio = getResponseResponse.AudioResponse.AudioData.ToByteArray();

            AudioClip clip = grpcAPI.ProcessByteAudioDataToAudioClip(byteAudio, getResponseResponse.AudioResponse.AudioConfig.SampleRateHertz.ToString());

            ResponseAudios.Add(new ResponseAudio
            {
                audioClip = clip,
                audioTranscript = tempString
            });
        }
    }

    

    /// <summary>
    /// If the playlist is not empty, then it is played.
    /// </summary>
    IEnumerator playAudioInOrder()
    {
        // plays audio as soon as there is audio to play
        while (!playingStopLoop)
        {
            if (ResponseAudios.Count > 0)
            {
                // add animation logic here

                if (GetComponent<OVRLipSync>())
                {
                    audioSource.clip = ResponseAudios[0].audioClip;
                    audioSource.Play();
                }
                else if (voiceHandler)
                {
                    voiceHandler.PlayAudioClip(ResponseAudios[0].audioClip);
                }
                else
                {
                    audioSource.clip = ResponseAudios[0].audioClip;
                    audioSource.Play();
                }

                if (convaiChatUIHandler != null)
                    convaiChatUIHandler.isCharacterTalking = true;

                if (convaiChatUIHandler != null)
                {
                    convaiChatUIHandler.characterText = ResponseAudios[0].audioTranscript;
                    convaiChatUIHandler.characterName = CharacterName;
                }

                yield return new WaitForSeconds(ResponseAudios[0].audioClip.length);

                if (convaiChatUIHandler != null)
                    convaiChatUIHandler.isCharacterTalking = false;

                audioSource.Stop();
                // audioSource.clip = null;

                if (ResponseAudios.Count > 0)
                    ResponseAudios.RemoveAt(0);
            }
            else
                yield return null;
        }
    }

    void OnApplicationQuit()
    {
        playingStopLoop = true;
    }
}
