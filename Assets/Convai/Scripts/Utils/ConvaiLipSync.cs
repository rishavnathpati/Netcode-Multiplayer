using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConvaiLipSync : MonoBehaviour
{

    [SerializeField]
    public SkinnedMeshRenderer mouthMeshRenderer;


    [SerializeField]
    public int indexOfMouthOpen;

    [SerializeField]
    public float mouthOpenWeight;

    [SerializeField]
    public SkinnedMeshRenderer teethMeshRenderer;

    [SerializeField]
    public int indexOfTeethOpen;

    [SerializeField]
    public float teethOpenWeight;

    private ConvaiNPC convaiNPC;

    [HideInInspector]
    public float[] audioSamples = null;

    AudioSource audioSource;

    bool playingStopLoop = false;

    float targetWeight = 0.0f;

    float blendShapeWeight = 0.0f;

    float amplitudeThreshold = 0.0f;
    float blendShapeMultiplier = 1.0f;
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
            yield return new WaitForSeconds(1 / 20f);
        }
    }


    float GetAmplitude()
    {
        // blendShapeWeight = 0;

        if (audioSource.clip != null)
        {
            audioSamples = new float[audioSource.clip.samples];

            audioSource.clip.GetData(audioSamples, audioSource.timeSamples);

            var amplitude = audioSamples[(audioSource.timeSamples < audioSamples.Length) ? (audioSource.timeSamples) : (audioSamples.Length - 1)];

            blendShapeWeight = Mathf.Clamp01(amplitude * blendShapeMultiplier);
        }

        return blendShapeWeight;
    }

    private void SetBlendShapeWeights(float targetWeight)
    {
        blendShapeWeight = Mathf.Lerp(blendShapeWeight, targetWeight, Time.deltaTime * smoothSpeed);

        mouthMeshRenderer.SetBlendShapeWeight(indexOfMouthOpen, blendShapeWeight * mouthOpenWeight);

        teethMeshRenderer.SetBlendShapeWeight(indexOfTeethOpen, blendShapeWeight * 0.5f * teethOpenWeight);
    }

    void OnApplicationQuit()
    {
        playingStopLoop = true;
    }
}
