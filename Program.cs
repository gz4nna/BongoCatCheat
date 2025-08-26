using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Input;
using FlaUI.UIA3;

namespace BongoCatCheat;

public class Program
{
    static async Task Main(string[] args)
    {
        System.Diagnostics.Process.Start("notepad.exe");
        var app = FlaUI.Core.Application.Attach("notepad");
        using var automation = new UIA3Automation();
        var window = app.GetMainWindow(automation);
        var _conditionFactory = new ConditionFactory(new UIA3PropertyLibrary());
        var textBox = window.FindFirstDescendant(_conditionFactory.ByClassName("Edit")).AsTextBox();
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
}