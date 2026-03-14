using System;
using System.Text.Json;
using Mendix.StudioPro.ExtensionsAPI.UI.DockablePane;
using Mendix.StudioPro.ExtensionsAPI.UI.Services;
using Mendix.StudioPro.ExtensionsAPI.UI.WebView;

namespace MyCompany.MyProject.PomodoroTimer;

public class PomodoroPaneViewModel(
    Uri webServerBaseUrl,
    IMessageBoxService messageBoxService,
    PomodoroHistoryStore historyStore,
    PomodoroStoryStore storyStore) : WebViewDockablePaneViewModel
{
    public override void InitWebView(IWebView webView)
    {
        Title = "Pomodoro Timer";
        webView.MessageReceived += OnMessageReceived;
        webView.Address = new Uri(webServerBaseUrl + "pomodoro");
    }

    private void OnMessageReceived(object? sender, MessageReceivedEventArgs e)
    {
        try
        {
            using var doc = JsonDocument.Parse(e.Message);
            var root = doc.RootElement;
            var type = root.GetProperty("type").GetString();

            switch (type)
            {
                case "WorkComplete":
                    var task = root.TryGetProperty("task", out var t) ? t.GetString() ?? "Untitled" : "Untitled";
                    historyStore.Add(new PomodoroRecord(historyStore.Count + 1, task, DateTime.Now));
                    messageBoxService.ShowInformation($"Pomodoro complete! Time for a break.\nTask: {task}");
                    break;
                case "BreakComplete":
                    messageBoxService.ShowInformation("Break over! Time to focus.");
                    break;
                case "AddStory":
                    var addStory = root.TryGetProperty("story", out var s1) ? s1.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(addStory)) storyStore.Add(addStory);
                    break;
                case "RemoveStory":
                    var removeStory = root.TryGetProperty("story", out var s2) ? s2.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(removeStory)) storyStore.Remove(removeStory);
                    break;
            }
        }
        catch (JsonException) { /* ignore malformed messages */ }
    }
}
