using System.Collections.Generic;
using System.ComponentModel.Composition;
using Mendix.StudioPro.ExtensionsAPI.UI.Menu;
using Mendix.StudioPro.ExtensionsAPI.UI.Services;

namespace MyCompany.MyProject.PomodoroTimer;

[method: ImportingConstructor]
[Export(typeof(MenuExtension))]
public class PomodoroMenuExtension(IDockingWindowService dockingWindowService) : MenuExtension
{
    public override IEnumerable<MenuViewModel> GetMenus()
    {
        yield return new MenuViewModel(
            "Open Pomodoro Timer",
            () => dockingWindowService.OpenPane(PomodoroPaneExtension.ID));
    }
}
