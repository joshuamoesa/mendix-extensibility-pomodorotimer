using System.ComponentModel.Composition;
using Mendix.StudioPro.ExtensionsAPI.UI.DockablePane;
using Mendix.StudioPro.ExtensionsAPI.UI.Services;

namespace MyCompany.MyProject.PomodoroTimer;

[method: ImportingConstructor]
[Export(typeof(DockablePaneExtension))]
public class PomodoroPaneExtension(IMessageBoxService messageBoxService) : DockablePaneExtension
{
    public const string ID = "pomodoro-timer";
    public override string Id => ID;
    public override DockablePaneViewModelBase Open()
        => new PomodoroPaneViewModel(WebServerBaseUrl!, messageBoxService);
}
