# 3D Audio LOD Engine & Psychoacoustic Compression

**Author:** Nguyen Huy Hoang  
**Student ID:** 20224313  
**Institution:** Hanoi University of Science and Technology (HUST)  

## Project Overview
This project demonstrates a real-time Level of Detail (LOD) audio engine built in Unity. It dynamically degrades audio fidelity based on 3D spatial distance using a custom C++ plugin to simulate psychoacoustic compression. 

It features a live interactive 3D environment where users can walk between different audio sources (a Radio and an RV) to experience real-time FFT bin destruction, latency tracking, and dynamic bitrate adjustments.

## Setup Instructions (Running on a New PC)

### Step 1: Clone the Repository
Open your terminal or command prompt and run:
`git clone https://github.com/BiteZaDust01/MDCC-Project-1---3D-Audio-LOD-Engine.git`

### Step 2: Open in Unity
1. Install **Unity Hub** and ensure **Unity Editor 2022.3.6f1** is installed.
2. Open Unity Hub, click **Add / Open**, and select the cloned repository folder.
3. Unity will take a few minutes to resolve packages and import the 3D assets.
4. In the Project window, navigate to `Assets/Scenes/` and double-click **SampleScene** (or the main Desert scene).

### Step 3: Recompiling the C++ Plugin (Optional)
*The compiled `.dll` (Windows) or `.so` (Linux) is already included in `Assets/Plugins`. You only need to do this if you modify the C++ math.*

1. Close Unity completely.
2. Navigate to the C++ source folder.
3. Compile using GCC (Ubuntu/MinGW):
   `g++ -O3 -shared -fPIC AudioPlugin.cpp kiss_fft.c kiss_fftr.c -o AudioPlugin.dll`
4. Move the resulting `.dll` or `.so` file into `Assets/Plugins/` and reopen Unity.

## Interactive Controls
* **WASD:** Move around the environment.
* **Mouse:** Look around (First-Person view).
* **E:** Interact with objects (Power on Radio / Trigger RV Engine).
* **SPACE:** Toggle A/B testing (Compressed LOD vs. Original Raw Audio).
* **TAB:** Open/Close the Real-time Metrics Window (displays Latency, Compression Ratio, SNR, and Destroyed Bins).

## Code Architecture (Separation of Concerns)
* `AudioEngineCore.cs`: Static C# wrapper bridging the Unity environment with the unmanaged C++ DLL. Calculates SNR and handles data.
* `PlayerRaycaster.cs`: Handles player line-of-sight interactions, Raycasting, and visual UI affordances (Emission highlights).
* `RadioLODController.cs` & `RVLODController.cs`: Object-specific logic handling dynamic distance calculations, exponential degradation curves, and deadzones.
* `AudioPlugin.cpp`: The core C++ algorithmic processing layer handling KissFFT and psychoacoustic frequency masking.