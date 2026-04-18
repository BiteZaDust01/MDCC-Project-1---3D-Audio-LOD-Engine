using UnityEngine;
using System;
using System.Runtime.InteropServices;

public static class AudioEngineCore
{
    // --- C++ DLL IMPORTS ---
    [DllImport("AudioPlugin", CallingConvention = CallingConvention.Cdecl)]
    public static extern int CompressAudio(float[] inAudio, float[] outAudio, int length, int sampleRate, float compressionMultiplier);

    // [DllImport("AudioPlugin", CallingConvention = CallingConvention.Cdecl)]
    // public static extern float CalculatePitch(float[] audioData, int length, int sampleRate);

    // [DllImport("AudioPlugin", CallingConvention = CallingConvention.Cdecl)]
    // public static extern IntPtr GetMusicalNote(float frequency);

    // --- WAV EXPORTER ---
    public static void ExportToWav(string baseFilename, AudioClip clip, int sampleRate)
    {
        string exportFolder = System.IO.Path.Combine(Application.dataPath, "Exports");
        if (!System.IO.Directory.Exists(exportFolder))
        {
            System.IO.Directory.CreateDirectory(exportFolder);
        }

        int fileIndex = 1;
        string finalFilename = $"{baseFilename}_{fileIndex}.wav";
        string filepath = System.IO.Path.Combine(exportFolder, finalFilename);

        while (System.IO.File.Exists(filepath))
        {
            fileIndex++;
            finalFilename = $"{baseFilename}_{fileIndex}.wav";
            filepath = System.IO.Path.Combine(exportFolder, finalFilename);
        }

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

    // --- SNR CALCULATOR ---
    public static float CalculateSNR(float[] original, float[] compressed, int length)
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
}