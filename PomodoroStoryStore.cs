using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace MyCompany.MyProject.PomodoroTimer;

[Export]
public class PomodoroStoryStore
{
    private readonly List<string> _stories = new();

    public void Add(string story) { lock (_stories) { if (!_stories.Contains(story)) _stories.Add(story); } }
    public void Remove(string story) { lock (_stories) _stories.Remove(story); }
    public IReadOnlyList<string> GetAll() { lock (_stories) return _stories.ToArray(); }
}
