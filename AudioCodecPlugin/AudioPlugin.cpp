#include <cmath>
#include "kiss_fftr.h"

#define EXPORT_API __declspec(dllexport)

// HELPER FUNCTION: Convert standard frequency (Hz) to the Psychoacoustic Bark Scale
float FrequencyToBark(float frequency)
{
    return 13.0f * std::atan(0.00076f * frequency) + 3.5f * std::atan(std::pow(frequency / 7500.0f, 2.0f));
}

// This lookup table represents the ATH curve for our 24 Bark Bands.
// Notice how bands 0-3 (Bass) and 20-23 (Treble) have high thresholds,
// while the middle bands (where the human ear is sensitive) have very low thresholds.
const float ATH_CURVE[24] = {
    500.0f, 300.0f, 150.0f, 100.0f, 50.0f, 20.0f, 10.0f, 5.0f,
    2.0f, 1.0f, 0.5f, 0.5f, 0.5f, 1.0f, 2.0f, 5.0f,
    10.0f, 20.0f, 50.0f, 100.0f, 200.0f, 500.0f, 1000.0f, 2000.0f};

extern "C"
{
    // MASTER FUNCTION: Full Audio Pipeline (FFT -> Mask -> iFFT)
    EXPORT_API int CompressAudio(float *inAudio, float *outAudio, int length, int sampleRate, float compressionMultiplier)
    {
        // 1. Allocate KissFFT for BOTH Forward (0) and Inverse (1) operations
        kiss_fftr_cfg cfgFwd = kiss_fftr_alloc(length, 0, NULL, NULL);
        kiss_fftr_cfg cfgInv = kiss_fftr_alloc(length, 1, NULL, NULL);

        int numBins = (length / 2) + 1;
        kiss_fft_cpx *freqData = new kiss_fft_cpx[numBins];

        // 2. Run Forward FFT to get the complex frequency data
        kiss_fftr(cfgFwd, inAudio, freqData);

        // 3. Calculate Bark Band Energies (so we know what is quiet vs loud)
        float barkEnergies[24] = {0};
        float hzPerBin = (float)sampleRate / (float)length;

        for (int i = 0; i < numBins; i++)
        {
            float freq = i * hzPerBin;
            int bandIndex = (int)FrequencyToBark(freq);
            if (bandIndex >= 0 && bandIndex < 24)
            {
                // Calculate magnitude
                float mag = std::sqrt((freqData[i].r * freqData[i].r) + (freqData[i].i * freqData[i].i));
                barkEnergies[bandIndex] += mag;
            }
        }

        // 4. Apply Masking: Delete the complex data if its band is too quiet!
        int binsDeleted = 0;
        for (int i = 0; i < numBins; i++)
        {
            float freq = i * hzPerBin;
            int bandIndex = (int)FrequencyToBark(freq);

            if (bandIndex >= 0 && bandIndex < 24)
            {
                float currentThreshold = ATH_CURVE[bandIndex] * compressionMultiplier;

                // If the entire band's energy is below human hearing, ZERO OUT the bin!
                if (barkEnergies[bandIndex] < currentThreshold)
                {
                    freqData[i].r = 0.0f; // Destroy the Real data
                    freqData[i].i = 0.0f; // Destroy the Imaginary data
                    binsDeleted++;
                }
            }
        }

        // 5. Run Inverse FFT (iFFT) to convert the surviving bins back into an audio wave
        kiss_fftri(cfgInv, freqData, outAudio);

        // 6. Normalize: KissFFT mathematically scales up the wave by "length", so we must divide it back down to normal volume.
        for (int i = 0; i < length; i++)
        {
            outAudio[i] /= (float)length;
        }

        // 7. Clean up RAM to prevent crashes
        free(cfgFwd);
        free(cfgInv);
        delete[] freqData;

        // Return how many raw frequency bins we deleted for the Unity UI
        return binsDeleted;
    }
}