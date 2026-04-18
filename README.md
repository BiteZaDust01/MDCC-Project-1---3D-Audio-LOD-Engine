# 3D Audio LOD Engine & Psychoacoustic Compression

**Author:** Nguyen Huy Hoang  
**Student ID:** 20224313  
**Institution:** Hanoi University of Science and Technology (HUST)  

## Project Overview
This project demonstrates a real-time Level of Detail (LOD) audio engine built in Unity. It dynamically degrades audio fidelity based on 3D spatial distance using a custom C++ plugin to simulate psychoacoustic compression. 

It features a live interactive 3D environment where users can walk between different audio sources (a Radio and an RV) to experience real-time FFT bin destruction, latency tracking, and dynamic bitrate adjustments.

## Environment & Requirements
* **Environment/Engine:** Unity 2022.3.62f3 (Required for exact API and package compatibility)
* **C++ Compiler:** MSVC (Visual Studio 2022 Build Tools)
* **Target Platforms:** Windows 10/11 (x64)

## Setup Instructions (Running on a New PC)

### Step 1: Clone the Repository
Open your terminal or command prompt and run:
`git clone https://github.com/BiteZaDust01/MDCC-Project-1---3D-Audio-LOD-Engine.git`

### Step 2: Open in Unity
1. Install **Unity Hub** and ensure **Unity Editor 2022.3.62f3** is installed.
2. Open Unity Hub, click **Add / Open**, and select the cloned repository folder.
3. Unity will take a few minutes to resolve packages and import the 3D assets.
4. In the Project window, navigate to `Assets/Scenes/` and double-click **SampleScene** (or your main Desert scene).

### Step 3: Recompiling the C++ Plugin (Optional)
*The compiled `.dll` is already included in `Assets/Plugins`. You only need to do this if you modify the C++ math.*

1. Close Unity completely to avoid file lock errors.
2. Open the **x64 Native Tools Command Prompt for VS 2022** (Search for it in the Windows Start menu).
3. Navigate to the C++ source folder using the `cd` command (e.g., `cd /d "F:\MDCC\MDCC-Project-1---3D-Audio-LOD-Engine\AudioCodecPlugin"`).
4. Compile the plugin by running the following command:
   `cl /LD /O2 /EHsc AudioPlugin.cpp kiss_fft.c kiss_fftr.c /Fe:AudioPlugin.dll`
5. Move the resulting `AudioPlugin.dll` file into the Unity project's `Assets/Plugins/` folder and reopen Unity.

## Interactive Controls
* **WASD:** Move around the environment.
* **Mouse:** Look around (First-Person view).
* **E:** Interact with objects (Power on Radio / Trigger RV Engine).
* **SPACE:** Toggle A/B testing (Compressed LOD vs. Original Raw Audio).
* **TAB:** Open/Close the Real-time Metrics Window (displays Latency, Compression Ratio, SNR, and Destroyed Bins).

## **Dataset**: The project utilizes two embedded audio samples located in Assets/Audio_sources: A dynamic multi-frequency music track (Radio_sound.wav) and a dense, noisy mechanical effect (car_sound.wav).

## Code Architecture (Separation of Concerns)
* `AudioEngineCore.cs`: Static C# wrapper bridging the Unity environment with the unmanaged C++ DLL. Calculates SNR and handles data arrays.
* `PlayerRaycaster.cs`: Handles player line-of-sight interactions, Raycasting, and visual UI affordances (Emission highlights).
* `RadioLODController.cs` & `RVLODController.cs`: Object-specific logic handling dynamic distance calculations, exponential degradation curves, and proximity deadzones.
* `AudioPlugin.cpp`: The core C++ algorithmic processing layer handling KissFFT and psychoacoustic frequency masking.
