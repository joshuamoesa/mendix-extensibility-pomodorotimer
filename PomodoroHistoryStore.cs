using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace MyCompany.MyProject.PomodoroTimer;

public record PomodoroRecord(int Number, string Task, DateTime CompletedAt);

[Export]
public class PomodoroHistoryStore
{
    private readonly List<PomodoroRecord> _records = new();

    public void Add(PomodoroRecord record) { lock (_records) _records.Add(record); }
    public IReadOnlyList<PomodoroRecord> GetAll() { lock (_records) return _records.ToArray(); }
    public int Count { get { lock (_records) return _records.Count; } }
}
