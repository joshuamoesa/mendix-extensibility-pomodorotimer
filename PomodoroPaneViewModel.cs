using System;
using Mendix.StudioPro.ExtensionsAPI.UI.DockablePane;
using Mendix.StudioPro.ExtensionsAPI.UI.Services;
using Mendix.StudioPro.ExtensionsAPI.UI.WebView;

namespace MyCompany.MyProject.PomodoroTimer;

public class PomodoroPaneViewModel(Uri webServerBaseUrl, IMessageBoxService messageBoxService)
    : WebViewDockablePaneViewModel
{
    public override void InitWebView(IWebView webView)
    {
        Title = "Pomodoro Timer";
        webView.MessageReceived += OnMessageReceived;
        webView.Address = new Uri(webServerBaseUrl + "pomodoro");
    }

    private void OnMessageReceived(object? sender, MessageReceivedEventArgs e)
    {
        if (e.Message == "WorkComplete")
            messageBoxService.ShowInformation("Pomodoro complete! Time for a break.");
        else if (e.Message == "BreakComplete")
            messageBoxService.ShowInformation("Break over! Time to focus.");
    }
}
