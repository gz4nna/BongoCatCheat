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

    private delegate bool EnumWindowsCallback(IntPtr windowHandle, IntPtr parameter);

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
