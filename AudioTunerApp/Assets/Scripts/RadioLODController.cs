using UnityEngine;
using UnityEngine.UI;
using System;
using System.Diagnostics;

[RequireComponent(typeof(AudioSource))]
public class RadioLODController : MonoBehaviour
{
    public Transform playerTransform;
    public AudioClip inputAudio;
    public Text resultText;

    public float lodStepDistance = 3.0f;
    private float lastProcessedDistance = -100f;

    [HideInInspector] public AudioSource audioSource;
    private float[] rawAudioBuffer;
    private float[] compressedAudioBuffer;
    private int sampleRate;
    private int length;

    private AudioClip compressedClip;
    public bool isShowingCompressed = false;

    public GameObject hoverLabel; // Drag your new World Space Canvas here!

    void Start()
    {
        Application.targetFrameRate = 60;

        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        sampleRate = inputAudio.frequency;

        int actualSamples = inputAudio.samples;
        int powerOfTwo = 1;
        while (powerOfTwo < actualSamples) powerOfTwo *= 2;

        length = Mathf.Min(powerOfTwo, 4194304);

        rawAudioBuffer = new float[length];
        compressedAudioBuffer = new float[length];

        float[] tempBuffer = new float[actualSamples];
        inputAudio.GetData(tempBuffer, 0);
        Array.Copy(tempBuffer, rawAudioBuffer, actualSamples);

        audioSource.clip = inputAudio;
        //resultText.text = "Look at the Radio and press 'E' to Power On.\nMode: ORIGINAL";
    }

    void Update()
    {
        if (!audioSource.isPlaying) return;

        float currentDistance = Vector3.Distance(playerTransform.position, transform.position);

        // --- Spacebar Toggle ---
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isShowingCompressed = !isShowingCompressed;
            int currentPlaybackTime = audioSource.timeSamples;

            if (isShowingCompressed)
            {
                lastProcessedDistance = currentDistance;
                RunExperiment(currentDistance);
            }
            else
            {
                audioSource.clip = inputAudio;
                resultText.text = $"Mode: ORIGINAL (Uncompressed)\nDistance: {currentDistance:F1} m\nCompression: 1x";
            }

            if (audioSource.clip != null)
            {
                audioSource.timeSamples = Mathf.Min(currentPlaybackTime, audioSource.clip.samples - 1);
                if (!audioSource.isPlaying) audioSource.Play();
            }
        }

        // --- Auto LOD Stepping ---
        if (isShowingCompressed && Mathf.Abs(currentDistance - lastProcessedDistance) >= lodStepDistance)
        {
            lastProcessedDistance = currentDistance;
            int currentPlaybackTime = audioSource.timeSamples;
            RunExperiment(currentDistance);

            if (audioSource.clip != null)
            {
                audioSource.timeSamples = Mathf.Min(currentPlaybackTime, audioSource.clip.samples - 1);
                if (!audioSource.isPlaying) audioSource.Play();
            }
        }
    }

    public void RunExperiment(float distance)
    {
        // float multiplier = Mathf.Clamp(distance * 1000f, 1f, 30000f);
        // Exponential Curve: (Distance^2) * 12
        // Hits 30,000x perfectly at the 30-meter map boundary
        float multiplier = Mathf.Clamp((distance * distance) * 33.3f, 1f, 30000f);

        Stopwatch timer = new Stopwatch();
        timer.Start();

        // Calling our new static core script!
        int destroyedBins = AudioEngineCore.CompressAudio(rawAudioBuffer, compressedAudioBuffer, length, sampleRate, multiplier);

        timer.Stop();
        long latencyMs = timer.ElapsedMilliseconds;

        // Math for UI
        float duration = (float)length / sampleRate;
        float originalSizeBytes = (sampleRate * duration * 16f * 1f) / 8f;
        float percentageKept = 1f - ((float)destroyedBins / (float)length);
        float compressedSizeBytes = originalSizeBytes * percentageKept;
        float compressionRatio = originalSizeBytes / Mathf.Max(1, compressedSizeBytes);
        float compressedBitrate = ((sampleRate * 16f * 1f) / 1000f) * percentageKept;
        float snrValue = AudioEngineCore.CalculateSNR(rawAudioBuffer, compressedAudioBuffer, length);

        resultText.text = $"Mode: LOD (Compressed)\nDistance: {distance:F1} m\nLevel: {multiplier:F0}x\nLatency: {latencyMs} ms\nDestroyed: {destroyedBins} bins\nRatio: {compressionRatio:F2}:1\nBitrate: {compressedBitrate:F0} kbps\nSNR: {snrValue:F2} dB";

        int actualSamples = inputAudio.samples;
        if (compressedClip == null) compressedClip = AudioClip.Create("CompressedAudio", actualSamples, 1, sampleRate, false);

        float[] trimmedBuffer = new float[actualSamples];
        Array.Copy(compressedAudioBuffer, trimmedBuffer, actualSamples);
        compressedClip.SetData(trimmedBuffer, 0);
        audioSource.clip = compressedClip;
    }
}