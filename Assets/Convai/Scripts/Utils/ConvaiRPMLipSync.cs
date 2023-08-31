using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConvaiRPMLipSync : MonoBehaviour
{
    [SerializeField]
    public SkinnedMeshRenderer faceMeshRenderer;

    [SerializeField]
    public SkinnedMeshRenderer teethMeshRenderer;

    // public AudioClip currentAudioClip;

    private ConvaiNPC convaiNPC;

    [HideInInspector]
    public float[] audioSamples = null;

    AudioSource audioSource;

    bool playingStopLoop = false;

    float targetWeight = 0.0f;

    float blendShapeWeight = 0.0f;

    float amplitudeThreshold = 0.0f;
    float blendShapeMultiplier = 10.0f;
    float smoothSpeed = 5.0f;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        convaiNPC = GetComponent<ConvaiNPC>();

        StartCoroutine(DoLipSync());
    }

    // Update is called once per frame
    void Update()
    {
        SetBlendShapeWeights(targetWeight);
    }

    IEnumerator DoLipSync()
    {
        while (!playingStopLoop)
        {
            targetWeight = GetAmplitude();
            yield return new WaitForSeconds(1/30f);
        }
    }
    

    float GetAmplitude()
    {
        if (audioSource.clip != null)
        {
            audioSamples = new float[audioSource.clip.samples];

            audioSource.clip.GetData(audioSamples, audioSource.timeSamples);

            var amplitude = audioSamples[audioSource.timeSamples];

            return Mathf.Clamp01(amplitude * blendShapeMultiplier);
        }

        return blendShapeWeight;
    }

    private void SetBlendShapeWeights(float targetWeight)
    {
        blendShapeWeight = Mathf.Lerp(blendShapeWeight, targetWeight, Time.deltaTime * smoothSpeed);

        faceMeshRenderer.SetBlendShapeWeight(0, blendShapeWeight * 3f);
        teethMeshRenderer.SetBlendShapeWeight(0, blendShapeWeight * 0.5f * 3f);
    }

    void OnApplicationQuit()
    {
        playingStopLoop = true;
    }
}
