using System.Runtime.InteropServices;
using System.Text;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.UIA3;

namespace BongoCatCheat;

public class Program
{
    private const string NotepadWindowClassName = "Notepad";
    private const int RestoreWindowCommand = 9;

    private const uint InputKeyboard = 1;
    private const uint KeyEventKeyUp = 0x0002;
    private const uint KeyEventScanCode = 0x0008;

    // F13 ~ F24，标准键盘上不存在、不产生字符、无需焦点
    private static readonly ushort[] FunctionKeys =
    {
        0x7C, 0x7D, 0x7E, 0x7F, 0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87,
    };

    private delegate bool EnumWindowsCallback(IntPtr windowHandle, IntPtr parameter);

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint type;
        public InputUnion union;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public KeyboardInput keyboard;
        [FieldOffset(0)] public MouseInput mouse;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort virtualKey;
        public ushort scanCode;
        public uint flags;
        public uint time;
        public IntPtr extraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr extraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint inputCount, Input[] inputs, int inputSize);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint code, uint mapType);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr windowHandle, StringBuilder className, int maxCount);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr windowHandle);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr windowHandle, int command);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr windowHandle);

    static async Task Main(string[] args)
    {
        if (args.Contains("--legacy"))
        {
            await RunLegacyModeAsync();
            return;
        }

        await RunFunctionKeyModeAsync();
    }

    private static async Task RunFunctionKeyModeAsync()
    {
        while (true)
        {
            SendFunctionKeys(keyUp: false);
            await Task.Delay(100);
            SendFunctionKeys(keyUp: true);
            await Task.Delay(50);
        }
    }

    private static void SendFunctionKeys(bool keyUp)
    {
        var inputs = new Input[FunctionKeys.Length];
        for (var i = 0; i < FunctionKeys.Length; i++)
        {
            inputs[i].type = InputKeyboard;
            inputs[i].union.keyboard.scanCode = (ushort)MapVirtualKey(FunctionKeys[i], 0);
            inputs[i].union.keyboard.flags = KeyEventScanCode | (keyUp ? KeyEventKeyUp : 0);
        }

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
    }

    private static async Task RunLegacyModeAsync()
    {
        System.Diagnostics.Process.Start("notepad.exe");

        var windowHandle = await WaitForNotepadWindowAsync();
        using var automation = new UIA3Automation();
        var window = automation.FromHandle(windowHandle);
        var _conditionFactory = new ConditionFactory(new UIA3PropertyLibrary());
        var textBox = (await WaitForTextBoxAsync(window, _conditionFactory)).AsTextBox();

        ShowWindow(windowHandle, RestoreWindowCommand);
        SetForegroundWindow(windowHandle);

        while (true)
        {
            Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
            await Task.Delay(20);
            Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_A);
            await Task.Delay(20);

            Keyboard.Press(FlaUI.Core.WindowsAPI.VirtualKeyShort.BACK);
            await Task.Delay(20);
            Keyboard.Release(FlaUI.Core.WindowsAPI.VirtualKeyShort.BACK);
            await Task.Delay(20);
        }
    }

    private static IntPtr GetNotepadWindow()
    {
        var windowHandle = IntPtr.Zero;
        EnumWindows((handle, parameter) =>
        {
            if (!IsWindowVisible(handle))
            {
                return true;
            }

            var className = new StringBuilder(256);
            if (GetClassName(handle, className, className.Capacity) > 0 &&
                className.ToString() == NotepadWindowClassName)
            {
                windowHandle = handle;
                return false;
            }

            return true;
        }, IntPtr.Zero);

        return windowHandle;
    }

    private static async Task<IntPtr> WaitForNotepadWindowAsync()
    {
        while (true)
        {
            var windowHandle = GetNotepadWindow();
            if (windowHandle != IntPtr.Zero)
            {
                return windowHandle;
            }

            await Task.Delay(100);
        }
    }

    private static async Task<AutomationElement> WaitForTextBoxAsync(AutomationElement window, ConditionFactory conditionFactory)
    {
        while (true)
        {
            try
            {
                var textBox = window.FindFirstDescendant(conditionFactory.ByClassName("RichEditD2DPT"))
                           ?? window.FindFirstDescendant(conditionFactory.ByControlType(ControlType.Document))
                           ?? window.FindFirstDescendant(conditionFactory.ByClassName("Edit"));
                if (textBox is not null)
                {
                    return textBox;
                }
            }
            catch
            {
                // 窗口已创建，但 UIA 树尚未就绪
            }

            await Task.Delay(200);
        }
    }
}
