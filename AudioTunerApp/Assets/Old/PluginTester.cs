using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using System;
using System.Diagnostics;

[RequireComponent(typeof(AudioSource))]
public class PluginTester : MonoBehaviour
{
    [DllImport("AudioPlugin", CallingConvention = CallingConvention.Cdecl)]
    private static extern int CompressAudio(float[] inAudio, float[] outAudio, int length, int sampleRate, float compressionMultiplier);

    [DllImport("AudioPlugin", CallingConvention = CallingConvention.Cdecl)]
    private static extern float CalculatePitch(float[] audioData, int length, int sampleRate);

    [DllImport("AudioPlugin", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr GetMusicalNote(float frequency);

    private AudioSource audioSource;
    private float[] rawAudioBuffer;
    private float[] compressedAudioBuffer;
    private int sampleRate;
    private int length;

    public AudioClip inputAudio;
    public Slider compressionSlider;
    public Text resultText;

    // --- NEW: A/B Testing Variables ---
    private AudioClip compressedClip;
    private bool isShowingCompressed = true;

    // --- NEW: 3D Tracking Variables ---
    public Transform playerTransform; // We will drag your Main Camera here
    public Transform radioTransform;  // We will drag the Radio here

    // --- NEW: Auto-LOD Tracking ---
    public float lodStepDistance = 3.0f; // How many meters you must walk to trigger a new LOD tier
    private float lastProcessedDistance = -100f; // Keeps track of where you last stood

    // --- NEW: Visual Feedback Variables ---
    private Material radioMaterial;
    private bool isHoveringRadio = false;


    void Start()
    {
        audioSource = radioTransform.GetComponent<AudioSource>();
        audioSource.loop = true;

        if (inputAudio == null)
        {
            UnityEngine.Debug.LogError("Please assign an Audio Clip in the Inspector!");
            return;
        }

        sampleRate = inputAudio.frequency;
        int actualSamples = inputAudio.samples;

        // --- THE FIX: ROUND UP INSTEAD OF DOWN ---
        int powerOfTwo = 1;
        while (powerOfTwo < actualSamples)
        {
            powerOfTwo *= 2;
        }

        // Increased the cap to over 1.5 minutes (4,194,304 samples) to fit the whole song
        int maxSafeLength = 4194304;
        length = Mathf.Min(powerOfTwo, maxSafeLength);

        rawAudioBuffer = new float[length];
        compressedAudioBuffer = new float[length];

        // Safely extract the exact audio length...
        float[] tempBuffer = new float[actualSamples];
        inputAudio.GetData(tempBuffer, 0);

        // ...and copy it into our larger Power-of-2 array. 
        // The leftover space at the end naturally stays as 0 (perfect silence).
        Array.Copy(tempBuffer, rawAudioBuffer, actualSamples);

        // Start in Original mode
        // isShowingCompressed = false;
        // audioSource.clip = inputAudio;
        // audioSource.Play();

        // // Set initial UI
        // resultText.text = "Mode: ORIGINAL (Uncompressed)\nWalk away and press SPACE to trigger LOD.";

        // Start in Original mode, but DO NOT play automatically
        isShowingCompressed = false;
        audioSource.clip = inputAudio;

        // Update the starting UI instructions
        resultText.text = "Look at the Radio and press 'E' to Power On.\nMode: ORIGINAL";

        // Grab the material so we can make it glow later
        radioMaterial = radioTransform.GetComponentInChildren<Renderer>().material;
    }


    void Update()
    {
        float currentDistance = Vector3.Distance(playerTransform.position, radioTransform.position);

        // Reset our "currently looking" state every frame
        bool currentlyLookingAtRadio = false;

        // --- 1. THE RAYCAST (LINE OF SIGHT INTERACTION) ---
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 5f))
        {
            if (hit.transform == radioTransform)
            {
                currentlyLookingAtRadio = true; // We are looking at it!

                if (Input.GetKeyDown(KeyCode.E))
                {
                    if (audioSource.isPlaying)
                    {
                        audioSource.Pause();
                        resultText.text = "Radio Paused.";
                    }
                    else
                    {
                        audioSource.Play();
                        lastProcessedDistance = currentDistance;
                        if (isShowingCompressed) RunExperiment();
                    }
                }
            }
        }

        // --- 2. GLOW LOGIC ---
        // If we just looked at it, turn the glow ON
        if (currentlyLookingAtRadio && !isHoveringRadio)
        {
            isHoveringRadio = true;
            radioMaterial.EnableKeyword("_EMISSION");
            // A soft white/grey value creates a nice subtle glow
            radioMaterial.SetColor("_EmissionColor", new Color(0.25f, 0.25f, 0.25f));
        }
        // If we just looked away, turn the glow OFF
        else if (!currentlyLookingAtRadio && isHoveringRadio)
        {
            isHoveringRadio = false;
            radioMaterial.SetColor("_EmissionColor", Color.black);
        }

        // --- 3. LOD LOGIC (Only runs if the radio is actively playing music) ---
        if (audioSource.isPlaying)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                isShowingCompressed = !isShowingCompressed;
                int currentPlaybackTime = audioSource.timeSamples;

                if (isShowingCompressed)
                {
                    UnityEngine.Debug.Log("▶️ Mode: COMPRESSED (Auto-LOD Active)");
                    lastProcessedDistance = currentDistance;
                    RunExperiment();
                }
                else
                {
                    UnityEngine.Debug.Log("▶️ Mode: ORIGINAL (Raw Audio)");
                    audioSource.clip = inputAudio;

                    resultText.text = $"Mode: ORIGINAL (Uncompressed)\n" +
                                      $"Distance from Radio: {currentDistance:F1} meters\n" +
                                      $"Compression Level: 1x\n" +
                                      $"Processing Latency: 0 ms\n" +
                                      $"Compression Ratio: 1:1";
                }

                if (audioSource.clip != null)
                {
                    audioSource.timeSamples = Mathf.Min(currentPlaybackTime, audioSource.clip.samples - 1);
                    if (!audioSource.isPlaying) audioSource.Play();
                }
            }

            if (isShowingCompressed)
            {
                if (Mathf.Abs(currentDistance - lastProcessedDistance) >= lodStepDistance)
                {
                    lastProcessedDistance = currentDistance;
                    int currentPlaybackTime = audioSource.timeSamples;

                    RunExperiment();

                    if (audioSource.clip != null)
                    {
                        audioSource.timeSamples = Mathf.Min(currentPlaybackTime, audioSource.clip.samples - 1);
                        if (!audioSource.isPlaying) audioSource.Play();
                    }
                }
            }
        }
    }


    public void RunExperiment()
    {
        // 1. Calculate 3D Distance instead of using a UI Slider
        float distance = Vector3.Distance(playerTransform.position, radioTransform.position);

        // Map distance to compression aggressiveness
        // By multiplying by 300, walking just 20 meters away hits the 6000x maximum
        float multiplier = Mathf.Clamp(distance * 1000f, 1f, 30000f);

        // 2. Start the Stopwatch (Latency Metric)
        Stopwatch timer = new Stopwatch();
        timer.Start();

        // 3. Your C++ Psychoacoustic Engine! 
        int destroyedBins = CompressAudio(rawAudioBuffer, compressedAudioBuffer, length, sampleRate, multiplier);

        // 4. Stop the clock
        timer.Stop();
        long latencyMs = timer.ElapsedMilliseconds;

        // --- 5. THE PROFESSOR'S EVALUATION MATH ---
        float duration = (float)length / sampleRate;
        float resolution = 16f; // 16-bit PCM
        float channels = 1f;    // Mono
        float originalSizeBytes = (sampleRate * duration * resolution * channels) / 8f;

        float percentageKept = 1f - ((float)destroyedBins / (float)length);
        float compressedSizeBytes = originalSizeBytes * percentageKept;

        float compressionRatio = originalSizeBytes / Mathf.Max(1, compressedSizeBytes);

        float originalBitrate = (sampleRate * resolution * channels) / 1000f; // ~705 kbps
        float compressedBitrate = originalBitrate * percentageKept;

        // 6. Print everything to your UI screen
        resultText.text = $"Mode: LOD (Compressed)\n" +
                          $"Distance from Radio: {distance:F1} meters\n" +
                          $"Compression Level: {multiplier:F0}x\n" +  // <-- Add this line right here!
                          $"Processing Latency: {latencyMs} ms\n" +
                          $"Data Destroyed: {destroyedBins} bins\n" +
                          $"Compression Ratio: {compressionRatio:F2}:1\n" +
                          $"Dynamic Bitrate: {compressedBitrate:F0} kbps";

        // --- 7. FIX: TRIM THE SILENCE & RESTORE THE LOOP ---
        // Get the exact original length of the song without the Power-of-2 padding
        int actualSamples = inputAudio.samples;

        if (compressedClip == null)
        {
            // IMPORTANT: Create the clip using 'actualSamples', NOT the padded 'length'
            compressedClip = AudioClip.Create("CompressedAudio", actualSamples, 1, sampleRate, false);
        }

        // Create an array that exactly matches the original song length
        float[] trimmedBuffer = new float[actualSamples];

        // Copy the processed audio out of the C++ buffer, leaving the ghost silence behind!
        Array.Copy(compressedAudioBuffer, trimmedBuffer, actualSamples);

        // Push only the actual song data into the Unity Audio Clip
        compressedClip.SetData(trimmedBuffer, 0);

        AudioSource radioAudio = radioTransform.GetComponent<AudioSource>();
        radioAudio.clip = compressedClip;
    }

    private float CalculateSNR(float[] original, float[] compressed, int length)
    {
        float signalPower = 0f;
        float noisePower = 0f;

        for (int i = 0; i < length; i++)
        {
            signalPower += original[i] * original[i];
            float difference = original[i] - compressed[i];
            noisePower += difference * difference;
        }

        if (noisePower == 0f) return float.PositiveInfinity;
        return 10f * Mathf.Log10(signalPower / noisePower);
    }

    // --- NEW: WAV Exporter ---
    private void ExportToWav(string baseFilename, AudioClip clip)
    {
        string exportFolder = System.IO.Path.Combine(Application.dataPath, "Exports");
        if (!System.IO.Directory.Exists(exportFolder))
        {
            System.IO.Directory.CreateDirectory(exportFolder);
        }

        // --- NEW: Smart Auto-Incrementing ---
        int fileIndex = 1;
        string finalFilename = $"{baseFilename}_{fileIndex}.wav";
        string filepath = System.IO.Path.Combine(exportFolder, finalFilename);

        // Keep incrementing the number until we find a filename that doesn't exist yet
        while (System.IO.File.Exists(filepath))
        {
            fileIndex++;
            finalFilename = $"{baseFilename}_{fileIndex}.wav";
            filepath = System.IO.Path.Combine(exportFolder, finalFilename);
        }
        // ------------------------------------

        float[] clipData = new float[clip.samples];
        clip.GetData(clipData, 0);

        Int16[] intData = new Int16[clipData.Length];
        Byte[] bytesData = new Byte[clipData.Length * 2];
        int rescaleFactor = 32767;

        for (int i = 0; i < clipData.Length; i++)
        {
            intData[i] = (short)(clipData[i] * rescaleFactor);
            BitConverter.GetBytes(intData[i]).CopyTo(bytesData, i * 2);
        }

        using (var fileStream = new System.IO.FileStream(filepath, System.IO.FileMode.Create))
        using (var writer = new System.IO.BinaryWriter(fileStream))
        {
            writer.Write("RIFF".ToCharArray());
            writer.Write(36 + bytesData.Length);
            writer.Write("WAVE".ToCharArray());
            writer.Write("fmt ".ToCharArray());
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(sampleRate);
            writer.Write(sampleRate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write("data".ToCharArray());
            writer.Write(bytesData.Length);
            writer.Write(bytesData);
        }

        UnityEngine.Debug.Log($"Successfully exported to: {filepath}");
    }
}