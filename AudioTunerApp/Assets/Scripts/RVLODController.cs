using UnityEngine;
using UnityEngine.UI;
using System;
using System.Diagnostics; // Required for the Stopwatch latency tracker

[RequireComponent(typeof(AudioSource))]
public class RVLODController : MonoBehaviour
{
    public Transform playerTransform;
    public AudioClip carSound;
    public Text resultText;
    public GameObject hoverLabel;

    private AudioSource audioSource;
    private float[] rawAudioBuffer;
    private float[] compressedAudioBuffer;
    private int sampleRate;
    private int length;
    private AudioClip compressedClip;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = false; // Ensure it only plays once
        sampleRate = carSound.frequency;

        // Buffer padding for the C++ FFT math
        int actualSamples = carSound.samples;
        int powerOfTwo = 1;
        while (powerOfTwo < actualSamples) powerOfTwo *= 2;
        length = Mathf.Min(powerOfTwo, 4194304);

        rawAudioBuffer = new float[length];
        compressedAudioBuffer = new float[length];

        float[] tempBuffer = new float[actualSamples];
        carSound.GetData(tempBuffer, 0);
        Array.Copy(tempBuffer, rawAudioBuffer, actualSamples);
    }

    // This is called by the Player's Raycast laser
    public void PlayCompressed()
    {
        // Don't interrupt if the sound is currently playing
        if (audioSource.isPlaying) return;

        float distance = Vector3.Distance(playerTransform.position, transform.position);

        // --- 1. THE NEW DEADZONE MATH ---
        float multiplier = 1f; // Default to 1x (uncompressed)

        if (distance > 15f)
        {
            // Calculate the remaining distance outside the 15m radius
            float effectiveDist = distance - 15f;
            // 133.33 * (15^2) hits exactly 30,000x at the 30-meter boundary
            multiplier = Mathf.Clamp((effectiveDist * effectiveDist) * 133.33f, 1f, 30000f);
        }

        // --- 2. START THE LATENCY CLOCK ---
        Stopwatch timer = new Stopwatch();
        timer.Start();

        // Crunch the audio using the static math toolbox
        int destroyedBins = AudioEngineCore.CompressAudio(rawAudioBuffer, compressedAudioBuffer, length, sampleRate, multiplier);

        // Stop the clock
        timer.Stop();
        long latencyMs = timer.ElapsedMilliseconds;

        // --- 3. CALCULATE ALL METRICS ---
        float duration = (float)length / sampleRate;
        float originalSizeBytes = (sampleRate * duration * 16f * 1f) / 8f;
        float percentageKept = 1f - ((float)destroyedBins / (float)length);
        float compressedSizeBytes = originalSizeBytes * percentageKept;
        float compressionRatio = originalSizeBytes / Mathf.Max(1, compressedSizeBytes);
        float compressedBitrate = ((sampleRate * 16f * 1f) / 1000f) * percentageKept;

        // Calculate SNR
        float snrValue = AudioEngineCore.CalculateSNR(rawAudioBuffer, compressedAudioBuffer, length);

        // --- 4. UPDATE THE UI WINDOW ---
        if (resultText != null)
        {
            resultText.text = $"Mode: RV LOD Compressed\n" +
                              $"Distance: {distance:F1} m\n" +
                              $"Level: {multiplier:F0}x\n" +
                              $"Latency: {latencyMs} ms\n" +
                              $"Destroyed: {destroyedBins} bins\n" +
                              $"Ratio: {compressionRatio:F2}:1\n" +
                              $"Bitrate: {compressedBitrate:F0} kbps\n" +
                              $"SNR: {snrValue:F2} dB";
        }

        // --- 5. TRIM AND PLAY ---
        int actualSamples = carSound.samples;
        if (compressedClip == null)
        {
            compressedClip = AudioClip.Create("RVCompressed", actualSamples, 1, sampleRate, false);
        }

        float[] trimmedBuffer = new float[actualSamples];
        Array.Copy(compressedAudioBuffer, trimmedBuffer, actualSamples);
        compressedClip.SetData(trimmedBuffer, 0);

        audioSource.clip = compressedClip;
        audioSource.Play();
    }
}