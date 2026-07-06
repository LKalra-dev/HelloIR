###### **# HelloIR**

Copyright © 2026 Luckshay Kalra



**## The Problem**

In April 2025, Microsoft introduced a \[security change to Windows Hello facial recognition](https://support.microsoft.com/en-gb/topic/april-8-2025-kb5055523-os-build-26100-3775-277a9d11-6ebf-410c-99f7-8c61957461eb):



"After installing this update or a later Windows update, for enhanced security, Windows Hello facial recognition requires color cameras to see a visible face when signing in."



For affected systems, Windows Hello face recognition may fail in darkness or difficult lighting conditions on systems that previously relied on their IR hardware to authenticate users without visible light.



HelloIR is a lightweight Windows background utility that temporarily disables the RGB camera before the Windows lock screen appears, allowing Windows Hello face recognition to rely on the IR camera in low-light or dark environments.



After the user unlocks the session, HelloIR automatically re-enables the RGB camera.



**## How It Works**



HelloIR runs silently in the background and monitors for the creation of `LogonUI.exe`, the Windows process responsible for displaying the sign-in and lock screen interface.



When `LogonUI.exe` is detected, HelloIR immediately disables the RGB camera using the Windows Configuration Manager API. This occurs before the session lock event is received, preventing the color camera from being available when Windows Hello begins facial authentication.



The sequence is:



1\. HelloIR detects `LogonUI.exe`.

2\. The RGB camera is disabled through `CM\_Disable\_DevNode`.

3\. The Windows lock screen appears with the RGB camera unavailable, allowing Windows Hello to operate using the remaining IR camera hardware.

4\. After the user signs in and the session is unlocked, HelloIR receives the Windows session unlock event.

5\. The RGB camera is restored through `CM\_Enable\_DevNode`.



HelloIR communicates directly with Windows through `cfgmgr32.dll`. An earlier implementation launched a PowerShell script using `Disable-PnpDevice`, but the process startup overhead took approximately 150–180 ms. Calling the Configuration Manager API directly reduced the camera-disable operation to only a few milliseconds during testing.



The utility itself runs as a hidden .NET 8 Windows application and remains active in the background while the user is signed in.



**## Current Status and Limitations**



DOES NOT CURRENTLY WORK LIKE AN install\&enjoy CORPORATE APP! Some manual configuration is required before it can work on another device.



HelloIR is currently a working prototype that has been tested on the system for which it was originally developed.



The current implementation has the following limitations:



\- The RGB camera device instance ID is hardcoded in the source code.

\- The utility therefore requires manual configuration before it can work on a different system.

\- HelloIR has currently been tested only on Windows 11 and on hardware with separate RGB and IR camera devices.

\- Administrator privileges are required to enable and disable the camera device.



* Despite these limitations, the current source code can be adapted to another compatible system by replacing the hardcoded RGB camera device instance ID with the ID of that system's RGB camera.



The next major development step is to replace the hardcoded device ID with automatic or user-configurable RGB camera detection.



**## Requirements**



\- Windows 11 (possibly Windows 10, but can't be quoted directly because it hasn't been tested on Windows 10 first-hand)

\- .NET 8 SDK when building from source

\- A Windows Hello-compatible camera system with RGB and IR camera devices module.

\- Administrator privileges



**## Building from Source**



Clone the repository:



```powershell

git clone https://github.com/LKalra-dev/HelloIR.git

cd HelloIR



Before building on another system, the user must replace the hardcoded `RGB\_CAMERA\_INSTANCE\_ID` in their local copy of `HiddenWindow.cs` with the device instance ID of that system's RGB camera.

