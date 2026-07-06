using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace WindowsHelloScript___HelloIR;



public class HiddenWindow : Form
{
    [StructLayout(LayoutKind.Sequential)]
    public struct POWERBROADCAST_SETTING
    {
        public Guid PowerSetting;

        public uint DataLength;
    }

    private const int WM_POWERBROADCAST = 0x0218;
    private const int WM_WTSSESSION_CHANGE = 0x02B1;
    private const int DEVICE_NOTIFY_WINDOW_HANDLE = 0;

    private const int WTS_SESSION_LOCK = 0x7;
    private const int WTS_SESSION_UNLOCK = 0x8;

    private const string RGB_CAMERA_INSTANCE_ID =
    @"USB\VID_5986&PID_2169&MI_00\6&17E2B06B&1&0000";

    private System.Threading.Timer? logonUiTimer;
    private bool logonUiDetected;

    private const int CR_SUCCESS = 0x00000000;
    private const uint CM_DISABLE_UI_NOT_OK = 0x00000002;

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    private static extern int CM_Locate_DevNodeW(
        out uint pdnDevInst,
        string pDeviceID,
        uint ulFlags);

    [DllImport("cfgmgr32.dll")]
    private static extern int CM_Disable_DevNode(
        uint dnDevInst,
        uint ulFlags);

    [DllImport("cfgmgr32.dll")]
    private static extern int CM_Enable_DevNode(
uint dnDevInst,
uint ulFlags);

    private static readonly Guid GUID_CONSOLE_DISPLAY_STATE =
        new("6FE69556-704A-47A0-8F24-C28D936FDA47");

    private static readonly Guid GUID_SESSION_DISPLAY_STATUS =
        new("2B84C20E-AD23-4DDF-93DB-05FFBD7EFCA5");

    [DllImport("Wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSRegisterSessionNotification(
    IntPtr hWnd,
    int dwFlags);

    private const int NOTIFY_FOR_THIS_SESSION = 0;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr RegisterPowerSettingNotification(
        IntPtr hRecipient,
        in Guid PowerSettingGuid,
        int Flags);

    private static void Log(string message)
    {
        Console.WriteLine(
            $"[{DateTime.Now:HH:mm:ss.fff}] [T{Thread.CurrentThread.ManagedThreadId}] {message}");
    }

    private static void DisableRGBCamera()
    {
        var sw = Stopwatch.StartNew();

        Log("Direct C# camera disable started...");

        int locateResult = CM_Locate_DevNodeW(
            out uint devInst,
            RGB_CAMERA_INSTANCE_ID,
            0);

        Log($"CM_Locate_DevNodeW result : 0x{locateResult:X8}");

        if (locateResult != CR_SUCCESS)
        {
            Log("ERROR: Could not locate RGB camera device.");
            return;
        }

        int disableResult = CM_Disable_DevNode(
            devInst,
            CM_DISABLE_UI_NOT_OK);

        sw.Stop();

        Log($"CM_Disable_DevNode result : 0x{disableResult:X8}");
        Log($"Direct camera disable finished in {sw.Elapsed.TotalMilliseconds:F3} ms");
    }

    private static void EnableRGBCamera()
{
    var sw = Stopwatch.StartNew();

    Log("Direct C# camera enable started...");

    int locateResult = CM_Locate_DevNodeW(
        out uint devInst,
        RGB_CAMERA_INSTANCE_ID,
        0);

    Log($"CM_Locate_DevNodeW result : 0x{locateResult:X8}");

    if (locateResult != CR_SUCCESS)
    {
        Log("ERROR: Could not locate RGB camera device.");
        return;
    }

    int enableResult = CM_Enable_DevNode(
        devInst,
        0);

    sw.Stop();

    Log($"CM_Enable_DevNode result : 0x{enableResult:X8}");
    Log($"Direct camera enable finished in {sw.Elapsed.TotalMilliseconds:F3} ms");
}

    private void CheckForLogonUI(object? state)
    {
        var processes = Process.GetProcessesByName("LogonUI");
        bool isRunning = processes.Length > 0;

        foreach (var process in processes)
            process.Dispose();

        if (isRunning && !logonUiDetected)
        {
            logonUiDetected = true;
            Log(">>> LogonUI.exe DETECTED <<<");
            DisableRGBCamera();
        }
        else if (!isRunning && logonUiDetected)
        {
            logonUiDetected = false;
            Log(">>> LogonUI.exe DISAPPEARED <<<");
        }
    }

    public HiddenWindow()
    {
        Log("HiddenWindow constructor");
        ShowInTaskbar = false;

        FormBorderStyle = FormBorderStyle.None;

        WindowState = FormWindowState.Minimized;

        Opacity = 0;

        Load += (_, _) =>
        {
            Log("HiddenWindow loaded");
            Hide();
        };

        Microsoft.Win32.SystemEvents.PowerModeChanged += (s, e) =>
        {
            Log($"PowerModeChanged : {e.Mode}");
        };

        logonUiTimer = new System.Threading.Timer(
    CheckForLogonUI,
    null,
    0,
    10);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        var h1 = RegisterPowerSettingNotification(
    Handle,
    GUID_CONSOLE_DISPLAY_STATE,
    DEVICE_NOTIFY_WINDOW_HANDLE);

        var h2 = RegisterPowerSettingNotification(
            Handle,
            GUID_SESSION_DISPLAY_STATUS,
            DEVICE_NOTIFY_WINDOW_HANDLE);

        Log($"Console display registration : {(h1 != IntPtr.Zero ? "OK" : "FAILED")}");
        Log($"Session display registration : {(h2 != IntPtr.Zero ? "OK" : "FAILED")}");

        bool ok = WTSRegisterSessionNotification(
    Handle,
    NOTIFY_FOR_THIS_SESSION);

        Log($"Session notification registration : {(ok ? "OK" : "FAILED")}");
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_POWERBROADCAST)
        {
            switch ((int)m.WParam)
            {
                case 4:
                    Log("PBT_APMSUSPEND");
                    break;

                case 7:
                    Log("PBT_APMRESUMEAUTOMATIC");
                    break;

                case 18:
                    Log("PBT_APMRESUMESUSPEND");
                    break;

                case 32787: // PBT_POWERSETTINGCHANGE
                    {
                        var setting =
                            Marshal.PtrToStructure<POWERBROADCAST_SETTING>(m.LParam);

                        Console.WriteLine();
                        Log("===== POWER SETTING CHANGE =====");
                        Log($"GUID       : {setting.PowerSetting}");
                        Log($"DataLength : {setting.DataLength}");

                        if (setting.DataLength == 4)
                        {
                            int value = Marshal.ReadInt32(
                                m.LParam,
                                Marshal.SizeOf<POWERBROADCAST_SETTING>());

                            Log($"Value      : {value}");
                        }

                        Console.WriteLine();

                        break;
                    }

                default:
                    Log($"WM_POWERBROADCAST : {m.WParam}");
                    break;


            }
        }
        else if (m.Msg == WM_WTSSESSION_CHANGE)
        {
            switch ((int)m.WParam)
            {
                case WTS_SESSION_LOCK:
                    Log("SESSION_LOCK");
                    // DisableRGBCamera();
                    break;

                case WTS_SESSION_UNLOCK:
                    Log("SESSION_UNLOCK");
                    EnableRGBCamera();
                    break;

                default:
                    Console.WriteLine($"SESSION : {m.WParam}");
                    break;
            }
        }

        base.WndProc(ref m);
    }
}