using System;
using System.Threading.Tasks;
using System.IO;

using UnityEngine;

using Grpc.Core;
using Service;
using static Service.GetResponseRequest.Types;
using Google.Protobuf;
using TMPro;
using UnityEditor;

namespace Convai.gRPCAPI
{
    public class ConvaiGRPCAPI : MonoBehaviour
    {
        public static ConvaiGRPCAPI Instance;

        public GameObject activeCharacter;

        // [HideInInspector]
        public ConvaiNPC activeConvaiNPC;

        [HideInInspector] public string APIKey;

        private ConvaiChatUIHandler convaiChatUIHandler;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            // else
            // {
            //     Destroy(gameObject);
            // }


            convaiChatUIHandler = FindObjectOfType<ConvaiChatUIHandler>();

            ConvaiAPIKeySetup APIKeyScriptableObject = Resources.Load<ConvaiAPIKeySetup>("ConvaiAPIKey");

            if (APIKeyScriptableObject != null)
            {
                APIKey = APIKeyScriptableObject.APIKey;
            }
            else
            {
                Debug.LogError("No API Key data found. Please complete the Convai Setup. In the Menu Bar, click Convai > Setup.");
            }
        }

        /// <summary>
        ///     This function sets the active character based on the who the player character is facing.
        /// </summary>
        /// <param name="other">The collider of the object with which this collider collides</param>
        private void OnTriggerEnter(Collider other)
        {
            if (other.tag == "Character" && other.gameObject.GetComponent<ConvaiNPC>() != null)
            {
                if (activeCharacter != null)
                {
                    activeConvaiNPC = activeCharacter.GetComponent<ConvaiNPC>();
                    activeConvaiNPC.isCharacterActive = false;
                }

                activeCharacter = other.gameObject;
                activeConvaiNPC = activeCharacter.GetComponent<ConvaiNPC>();

                activeConvaiNPC.isCharacterActive = true;
            }
        }

        #region CONVAI_UTILS

        public byte[] ProcessRequestAudiotoWav(AudioClip requestAudioClip)
        {
            float[] floatAudioData = new float[requestAudioClip.samples];
            requestAudioClip.GetData(floatAudioData, 0);

            var intData = new short[floatAudioData.Length];
            //converting in 2 float[] steps to Int16[], //then Int16[] to Byte[]

            var bytesData = new byte[floatAudioData.Length * 2];
            //bytesData array is twice the size of
            //dataSource array because a float converted in Int16 is 2 bytes.

            var rescaleFactor = 32767; //to convert float to Int16

            for (var i = 0; i < floatAudioData.Length; i++)
            {
                intData[i] = (short)(floatAudioData[i] * rescaleFactor);

                var byteArr = BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }

            byte[] wavByteData = AddByteToArray(bytesData, requestAudioClip.frequency.ToString());

            return wavByteData;
        }

        /// <summary>
        /// Converts a byte array to a float array representing audio data
        /// </summary>
        /// <param name="source"></param>
        /// <returns>float array containing audio data</returns>
        float[] Convert16BitByteArrayToFloatAudioClipData(byte[] source)
        {
            int x = sizeof(short);
            int convertedSize = source.Length / x;
            float[] data = new float[convertedSize];

            int byte_idx = 0;
            int data_idx = 0;

            while (byte_idx < source.Length)
            {
                var first_byte = source[byte_idx];
                var second_byte = source[byte_idx + 1];
                byte_idx += 2;
                // convert two bytes to one short (little endian)
                short s = (short)((second_byte << 8) | first_byte);
                data[data_idx] = s / 32768.0F;
                data_idx++;
            }
            return data;

        }

        /// <summary>
        /// Converts string audio data to an audio clip
        /// </summary>
        /// <param name="audioData">string containining audio data</param>
        /// <param name="stringSampleRate">string containing the sample rate of the audio</param>
        /// <returns>audio clip</returns>
        public AudioClip ProcessStringAudioDataToAudioClip(string audioData, string stringSampleRate)
        {
            byte[] byteAudio = Convert.FromBase64String(audioData);

            // trim 44 bytes of header
            byte[] trimmedByteAudio = new byte[byteAudio.Length - 44];

            for (int i = 0, j = 44; i < byteAudio.Length - 44; i++, j++)
            {
                trimmedByteAudio[i] = byteAudio[j];
            }

            float[] samples = Convert16BitByteArrayToFloatAudioClipData(trimmedByteAudio);

            int channels = 1;
            int sampleRate = int.Parse(stringSampleRate);

            AudioClip clip = AudioClip.Create("ClipName", samples.Length, channels, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Converts byte array containing audio data into audio clip
        /// </summary>
        /// <param name="byteAudio">byte array containing audio data</param>
        /// <param name="stringSampleRate">string containing the sample rate of the audio</param>
        /// <returns>audio clip</returns>
        public AudioClip ProcessByteAudioDataToAudioClip(byte[] byteAudio, string stringSampleRate)
        {
            // trim 44 bytes of header
            byte[] trimmedByteAudio = new byte[byteAudio.Length - 44];

            for (int i = 0, j = 44; i < byteAudio.Length - 44; i++, j++)
            {
                trimmedByteAudio[i] = byteAudio[j];
            }

            float[] samples = Convert16BitByteArrayToFloatAudioClipData(trimmedByteAudio);

            int channels = 1;
            int sampleRate = int.Parse(stringSampleRate);

            AudioClip clip = AudioClip.Create("ClipName", samples.Length, channels, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Starts recording the audio and sends it to the server in periodic chunks.
        /// </summary>
        /// <param name="client">gRPC service Client object</param>
        /// <param name="isActionActive">Bool specifying whether we are expecting action responses</param>
        /// <param name="recordingFrequency">Frequency of the audio being sent</param>
        /// <param name="recordingLength">Length of the recording from the microphone</param>
        /// <param name="characterID">Character ID obtained from the playground</param>
        /// <param name="actionConfig">Object containing the action configuration</param>
        /// <param name="enableTestMode">Bool specifying whether test mode is enabled</param>
        /// <param name="testUserQuery">String containing the test query that will be sent to the server for processing</param>
        public async Task StartRecordAudio(ConvaiService.ConvaiServiceClient client, int recordingFrequency, int recordingLength, string characterID,  bool enableTestMode, string testUserQuery)
        {
            // causes the computer to freeze
            AsyncDuplexStreamingCall<GetResponseRequest, GetResponseResponse> call = client.GetResponse();

            // Start the duplex streaming call
            GetResponseRequest getResponseConfigRequest = null;

            // Debug.Log("Record audio!");

            #region CONFIG_SETUP

            {
                getResponseConfigRequest = new GetResponseRequest
                {
                    GetResponseConfig = new GetResponseConfig
                    {
                        CharacterId = characterID,
                        ApiKey = APIKey,
                        SessionId = activeConvaiNPC.sessionID,
                        AudioConfig = new AudioConfig
                        {
                            SampleRateHertz = recordingFrequency,
                        }
                    }
                };
            }

            //Debug.Log("API Key sent to server: " + getResponseConfigRequest.GetResponseConfig.ApiKey);
            try
            {
                await call.RequestStream.WriteAsync(getResponseConfigRequest);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
            #endregion

            AudioClip audioClip = Microphone.Start(null, false, recordingLength, recordingFrequency);

            convaiChatUIHandler.isUserTalking = true;

            Task task = Task.Run(async () =>
            {
                await ReceiveResultFromServer(call);
            });

            float[] audioData = null;
            int pos = 0;

            int microphonePosition = Microphone.GetPosition(null);

            int diff = microphonePosition - pos;

            while (Microphone.IsRecording(null))
            {
                // Wait for a period of time
                await Task.Delay(200);

                microphonePosition = Microphone.GetPosition(null);
                diff = microphonePosition - pos;

                audioData = new float[recordingFrequency * recordingLength];
                audioClip.GetData(audioData, pos);

                await ProcessAudioChunk(call, diff, audioData, enableTestMode, testUserQuery);

                pos += diff;
            }

            await ProcessAudioChunk(call, diff, audioData, enableTestMode, testUserQuery);

            await call.RequestStream.CompleteAsync();

            convaiChatUIHandler.isUserTalking = false;
        }

        /// <summary>
        /// Stops recording and processing the audio.
        /// </summary>
        public void StopRecordAudio()
        {
            Microphone.End(null);
        }

        /// <summary>
        /// Processes each audio chunk and sends it to the server.
        /// </summary>
        /// <param name="call">gRPC Streaming call connecting to the getResponse function</param>
        /// <param name="diff">Length of the audio data from the current position to the position of the last sent chunk</param>
        /// <param name="audioData">Chunk of audio data that we want to be processed</param>
        /// <param name="enableTestMode">Bool specifying whether test mode is enabled</param>
        /// <param name="testUserQuery">String containing the test query that will be sent to the server for processing</param>
        async Task ProcessAudioChunk(AsyncDuplexStreamingCall<GetResponseRequest, GetResponseResponse> call, int diff, float[] audioData, bool enableTestMode, string testUserQuery)
        {
            // Debug.Log("Recorded diff: " + diff);
            if (!enableTestMode)
            {
                if (diff > 0)
                {
                    // Convert audio data to byte array
                    byte[] audioByteArray = new byte[diff * sizeof(short)];

                    for (int i = 0; i < diff; i++)
                    {
                        float sample = audioData[i];
                        short shortSample = (short)(sample * short.MaxValue);
                        byte[] shortBytes = BitConverter.GetBytes(shortSample);
                        audioByteArray[i * sizeof(short)] = shortBytes[0];
                        audioByteArray[i * sizeof(short) + 1] = shortBytes[1];
                    }

                    // Send audio data to the gRPC server
                    try
                    {
                        await call.RequestStream.WriteAsync(new GetResponseRequest
                        {
                            GetResponseData = new GetResponseData
                            {
                                AudioData = ByteString.CopyFrom(audioByteArray)
                            }
                        });

                    }
                    catch (RpcException rpcException)
                    {
                        if (rpcException.StatusCode == StatusCode.Cancelled)
                        {
                            Debug.LogException(rpcException);
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }
            else
            {
                // Debug.Log("In the else block!");

                try
                {
                    await call.RequestStream.WriteAsync(new GetResponseRequest
                    {
                        GetResponseData = new GetResponseData
                        {
                            TextData = testUserQuery
                        }
                    });

                }
                catch (RpcException rpcException)
                {
                    if (rpcException.StatusCode == StatusCode.Cancelled)
                    {
                        Debug.LogException(rpcException);
                    }
                    else
                    {
                        throw;
                    }
                }
            }

        }

        /// <summary>
        /// Periodically receives responses from the server and adds it to a static list in streaming NPC
        /// </summary>
        /// <param name="call">gRPC Streaming call connecting to the getResponse function</param>
        async Task ReceiveResultFromServer(AsyncDuplexStreamingCall<GetResponseRequest, GetResponseResponse> call)
        {
            while (await call.ResponseStream.MoveNext())
            {
                try
                {
                    GetResponseResponse result = call.ResponseStream.Current;

                    if (result.UserQuery != null)
                    {
                       convaiChatUIHandler.userText  = result.UserQuery.TextData;
                    }

                    if (result.AudioResponse != null)
                    {
                        activeConvaiNPC.getResponseResponses.Add(call.ResponseStream.Current);
                    }

                    activeConvaiNPC.sessionID = call.ResponseStream.Current.SessionId;
                }
                catch (RpcException rpcException)
                {
                    if (rpcException.StatusCode == StatusCode.Cancelled)
                    {
                        Debug.LogException(rpcException);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Adds WAV header to the audio data
        /// </summary>
        /// <param name="audioByteArray">Byte array containing audio data</param>
        /// <param name="sampleRate">Bit rate of the audio that needs to be processed</param>
        /// <returns></returns>
        byte[] AddByteToArray(byte[] audioByteArray, string sampleRate)
        {
            var newArray = new byte[audioByteArray.Length + 44];
            audioByteArray.CopyTo(newArray, 44);

            int intSampleRate = Convert.ToInt32(sampleRate);

            newArray[0] = Convert.ToByte('R');
            newArray[1] = Convert.ToByte('I');
            newArray[2] = Convert.ToByte('F');
            newArray[3] = Convert.ToByte('F');

            var newLength = BitConverter.GetBytes(audioByteArray.Length + 36);
            Buffer.BlockCopy(newLength, 0, newArray, 4, 4);

            newArray[8] = Convert.ToByte('W');
            newArray[9] = Convert.ToByte('A');
            newArray[10] = Convert.ToByte('V');
            newArray[11] = Convert.ToByte('E');

            newArray[12] = Convert.ToByte('f');
            newArray[13] = Convert.ToByte('m');
            newArray[14] = Convert.ToByte('t');
            newArray[15] = Convert.ToByte(' ');

            Buffer.BlockCopy(BitConverter.GetBytes(16), 0, newArray, 16, 4); // Chunk size
            Buffer.BlockCopy(BitConverter.GetBytes(1), 0, newArray, 20, 2); // Audio Format
            Buffer.BlockCopy(BitConverter.GetBytes(1), 0, newArray, 22, 2); // Num of channels

            Buffer.BlockCopy(BitConverter.GetBytes(intSampleRate), 0, newArray, 24, 4); // Sample rate

            Buffer.BlockCopy(BitConverter.GetBytes(intSampleRate * 2), 0, newArray, 28, 4); // Bit rate
            Buffer.BlockCopy(BitConverter.GetBytes(2), 0, newArray, 32, 2); // Block Align
            Buffer.BlockCopy(BitConverter.GetBytes(16), 0, newArray, 34, 2); // Bits sample

            newArray[36] = Convert.ToByte('d');
            newArray[37] = Convert.ToByte('a');
            newArray[38] = Convert.ToByte('t');
            newArray[39] = Convert.ToByte('a');

            Buffer.BlockCopy(BitConverter.GetBytes(audioByteArray.Length), 0, newArray, 40, 4); // Number of byte of audio data
            Buffer.BlockCopy(audioByteArray, 0, newArray, 44, audioByteArray.Length);

            return newArray;
        }

        #endregion
    }
}