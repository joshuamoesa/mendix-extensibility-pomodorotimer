using System.ComponentModel.Composition;
using System.Linq;
using System.Text.Json;
using Mendix.StudioPro.ExtensionsAPI.UI.WebServer;

namespace MyCompany.MyProject.PomodoroTimer;

[Export(typeof(WebServerExtension))]
[method: ImportingConstructor]
public class PomodoroWebServer(PomodoroHistoryStore historyStore, PomodoroStoryStore storyStore) : WebServerExtension
{
    private const string HtmlContent = """
    <!DOCTYPE html>
    <html lang="en">
    <head>
      <meta charset="UTF-8" />
      <meta name="viewport" content="width=device-width, initial-scale=1.0" />
      <title>Pomodoro Timer</title>
      <style>
        *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }

        :root {
          --bg: #f5f5f5;
          --surface: #ffffff;
          --text: #1a1a1a;
          --subtext: #666666;
          --work-color: #e74c3c;
          --break-color: #27ae60;
          --long-break-color: #2980b9;
          --btn-bg: #e0e0e0;
          --btn-hover: #c8c8c8;
          --ring-track: #e0e0e0;
          --dot-empty: #d0d0d0;
          --divider: #e0e0e0;
          --input-border: #d0d0d0;
        }

        @media (prefers-color-scheme: dark) {
          :root {
            --bg: #1e1e1e;
            --surface: #2d2d2d;
            --text: #f0f0f0;
            --subtext: #aaaaaa;
            --btn-bg: #3a3a3a;
            --btn-hover: #4a4a4a;
            --ring-track: #3a3a3a;
            --dot-empty: #444444;
            --divider: #3a3a3a;
            --input-border: #444444;
          }
        }

        body {
          font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
          background: var(--bg);
          color: var(--text);
          padding: 20px;
          min-height: 100vh;
        }

        /* ── Views ── */
        .view { display: none; flex-direction: column; align-items: center; gap: 20px; }
        .view.active { display: flex; }

        /* ── Top bar ── */
        .top-bar {
          width: 100%;
          max-width: 280px;
          display: flex;
          align-items: center;
          justify-content: space-between;
        }

        .top-bar-title {
          font-size: 12px;
          font-weight: 600;
          letter-spacing: 0.08em;
          text-transform: uppercase;
          color: var(--subtext);
        }

        .icon-btn {
          background: none;
          border: none;
          cursor: pointer;
          color: var(--subtext);
          font-size: 16px;
          padding: 4px;
          line-height: 1;
          border-radius: 4px;
          transition: color 0.15s, background 0.15s;
        }
        .icon-btn:hover { color: var(--text); background: var(--btn-bg); }

        /* ── Task input ── */
        .task-row {
          width: 100%;
          max-width: 280px;
        }

        .task-input {
          width: 100%;
          padding: 8px 10px;
          border: 1px solid var(--input-border);
          border-radius: 6px;
          background: var(--surface);
          color: var(--text);
          font-size: 13px;
          outline: none;
          transition: border-color 0.15s;
        }
        .task-input:focus { border-color: var(--work-color); }

        /* ── Phase label ── */
        .phase-label {
          font-size: 13px;
          font-weight: 600;
          letter-spacing: 0.1em;
          text-transform: uppercase;
          color: var(--subtext);
        }

        /* ── Ring ── */
        .ring-container { position: relative; width: 180px; height: 180px; }
        .ring-svg { transform: rotate(-90deg); width: 180px; height: 180px; }
        .ring-track { fill: none; stroke: var(--ring-track); stroke-width: 8; }
        .ring-progress {
          fill: none; stroke: var(--work-color); stroke-width: 8;
          stroke-linecap: round;
          transition: stroke-dashoffset 0.9s linear, stroke 0.3s ease;
        }
        .ring-center {
          position: absolute; top: 50%; left: 50%;
          transform: translate(-50%, -50%); text-align: center;
        }
        .time-display {
          font-size: 38px; font-weight: 300;
          font-variant-numeric: tabular-nums; letter-spacing: -1px; line-height: 1;
        }

        /* ── Dots ── */
        .dots { display: flex; gap: 10px; align-items: center; }
        .dot {
          width: 11px; height: 11px; border-radius: 50%;
          background: var(--dot-empty);
          transition: background 0.3s ease, transform 0.3s ease;
        }
        .dot.filled { background: var(--work-color); transform: scale(1.1); }

        /* ── Controls ── */
        .controls { display: flex; gap: 10px; }
        button {
          padding: 8px 18px; border: none; border-radius: 6px;
          background: var(--btn-bg); color: var(--text);
          font-size: 13px; font-weight: 500; cursor: pointer;
          transition: background 0.15s ease; min-width: 68px;
        }
        button:hover { background: var(--btn-hover); }
        #startBtn { background: var(--work-color); color: #fff; min-width: 84px; }
        #startBtn:hover { filter: brightness(1.1); }

        .session-info { font-size: 12px; color: var(--subtext); }

        /* ── Divider ── */
        .divider { width: 100%; max-width: 280px; height: 1px; background: var(--divider); }

        /* ── History ── */
        .history-section { width: 100%; max-width: 280px; display: flex; flex-direction: column; gap: 8px; }
        .section-header { display: flex; align-items: center; justify-content: space-between; }
        .section-title {
          font-size: 11px; font-weight: 600;
          text-transform: uppercase; letter-spacing: 0.08em; color: var(--subtext);
        }
        .badge {
          font-size: 11px; color: var(--subtext);
          background: var(--btn-bg); padding: 2px 7px; border-radius: 10px;
        }
        .history-list {
          display: flex; flex-direction: column; gap: 3px;
          max-height: 150px; overflow-y: auto;
        }
        .history-item {
          display: grid; grid-template-columns: 26px 1fr auto;
          gap: 6px; align-items: center;
          padding: 5px 8px; background: var(--surface); border-radius: 5px; font-size: 12px;
        }
        .h-num { color: var(--subtext); font-weight: 600; }
        .h-task { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
        .h-time { color: var(--subtext); font-variant-numeric: tabular-nums; white-space: nowrap; }
        .list-empty { font-size: 12px; color: var(--subtext); text-align: center; padding: 10px 0; }

        /* ── Settings view ── */
        .settings-view { width: 100%; max-width: 280px; display: flex; flex-direction: column; gap: 14px; }
        .settings-header { display: flex; align-items: center; gap: 8px; }
        .settings-title { font-size: 14px; font-weight: 600; }
        .story-list { display: flex; flex-direction: column; gap: 4px; max-height: 200px; overflow-y: auto; }
        .story-item {
          display: flex; align-items: center; justify-content: space-between;
          padding: 7px 10px; background: var(--surface); border-radius: 6px; font-size: 13px;
        }
        .story-name { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; flex: 1; margin-right: 8px; }
        .remove-btn {
          background: none; border: none; cursor: pointer; color: var(--subtext);
          font-size: 15px; padding: 0 2px; line-height: 1; min-width: unset;
          transition: color 0.15s;
        }
        .remove-btn:hover { color: var(--work-color); background: none; }
        .add-row { display: flex; gap: 8px; }
        .add-input {
          flex: 1; padding: 8px 10px; border: 1px solid var(--input-border);
          border-radius: 6px; background: var(--surface); color: var(--text);
          font-size: 13px; outline: none; transition: border-color 0.15s;
        }
        .add-input:focus { border-color: var(--work-color); }
        .add-btn {
          padding: 8px 14px; background: var(--work-color); color: #fff;
          border: none; border-radius: 6px; font-size: 13px; font-weight: 500;
          cursor: pointer; min-width: unset; transition: filter 0.15s;
        }
        .add-btn:hover { filter: brightness(1.1); }
      </style>
    </head>
    <body>

      <!-- ── Main view ── -->
      <div class="view active" id="mainView">

        <div class="top-bar">
          <span class="top-bar-title" id="phaseLabel">Focus</span>
          <button class="icon-btn" id="gearBtn" title="Manage user stories">⚙</button>
        </div>

        <div class="task-row">
          <input class="task-input" type="text" id="taskInput"
                 list="storyList" placeholder="Working on…" autocomplete="off" />
          <datalist id="storyList"></datalist>
        </div>

        <div class="ring-container">
          <svg class="ring-svg" viewBox="0 0 180 180">
            <circle class="ring-track" cx="90" cy="90" r="78" />
            <circle class="ring-progress" id="ringProgress" cx="90" cy="90" r="78"
              stroke-dasharray="490.09" stroke-dashoffset="0" />
          </svg>
          <div class="ring-center">
            <div class="time-display" id="timeDisplay">25:00</div>
          </div>
        </div>

        <div class="dots" id="dots">
          <div class="dot" id="dot0"></div>
          <div class="dot" id="dot1"></div>
          <div class="dot" id="dot2"></div>
          <div class="dot" id="dot3"></div>
        </div>

        <div class="controls">
          <button id="startBtn">Start</button>
          <button id="pauseBtn" disabled>Pause</button>
          <button id="resetBtn">Reset</button>
        </div>

        <span class="session-info" id="sessionInfo">Session 1 of 4</span>

        <div class="divider"></div>

        <div class="history-section">
          <div class="section-header">
            <span class="section-title">History</span>
            <span class="badge" id="historyBadge">0</span>
          </div>
          <div class="history-list" id="historyList">
            <div class="list-empty">No sessions yet</div>
          </div>
        </div>

      </div>

      <!-- ── Settings view ── -->
      <div class="view" id="settingsView">
        <div class="settings-view">
          <div class="settings-header">
            <button class="icon-btn" id="backBtn" title="Back">←</button>
            <span class="settings-title">User Stories</span>
          </div>
          <div class="story-list" id="storyList2"></div>
          <div class="add-row">
            <input class="add-input" type="text" id="addInput" placeholder="Add user story…" />
            <button class="add-btn" id="addBtn">Add</button>
          </div>
        </div>
      </div>

      <script>
        // ─── Constants ────────────────────────────────────────────────────────
        const DURATIONS = { work: 25*60, shortBreak: 5*60, longBreak: 15*60 };
        const PHASE_LABELS = { work: 'Focus', shortBreak: 'Short Break', longBreak: 'Long Break' };
        const PHASE_COLORS = { work: 'var(--work-color)', shortBreak: 'var(--break-color)', longBreak: 'var(--long-break-color)' };
        const CIRCUMFERENCE = 2 * Math.PI * 78; // ≈ 490.09

        // ─── State ────────────────────────────────────────────────────────────
        let phase = 'work', completedWork = 0;
        let remaining = DURATIONS.work, totalDuration = DURATIONS.work;
        let running = false, intervalId = null;

        // ─── DOM ──────────────────────────────────────────────────────────────
        const timeDisplay  = document.getElementById('timeDisplay');
        const phaseLabel   = document.getElementById('phaseLabel');
        const ringProgress = document.getElementById('ringProgress');
        const sessionInfo  = document.getElementById('sessionInfo');
        const startBtn     = document.getElementById('startBtn');
        const pauseBtn     = document.getElementById('pauseBtn');
        const resetBtn     = document.getElementById('resetBtn');
        const taskInput    = document.getElementById('taskInput');
        const storyList    = document.getElementById('storyList');   // datalist
        const historyList  = document.getElementById('historyList');
        const historyBadge = document.getElementById('historyBadge');
        const storyList2   = document.getElementById('storyList2');  // settings list
        const addInput     = document.getElementById('addInput');
        const dots         = [0,1,2,3].map(i => document.getElementById('dot'+i));

        // ─── Helpers ──────────────────────────────────────────────────────────
        function pad(n) { return String(n).padStart(2,'0'); }
        function fmtSecs(s) { return pad(Math.floor(s/60))+':'+pad(s%60); }
        function escHtml(s) { return s.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;'); }

        function updateRing() {
          ringProgress.style.strokeDashoffset = CIRCUMFERENCE * (1 - remaining/totalDuration);
          ringProgress.style.stroke = PHASE_COLORS[phase];
        }

        function updateUI() {
          timeDisplay.textContent = fmtSecs(remaining);
          phaseLabel.textContent  = PHASE_LABELS[phase];
          updateRing();
          dots.forEach((d,i) => d.classList.toggle('filled', i < completedWork));
          sessionInfo.textContent = phase === 'work'
            ? 'Session '+(completedWork+1)+' of 4'
            : phase === 'longBreak' ? 'Long break — well earned!' : 'Short break';
          startBtn.style.background = PHASE_COLORS[phase];
        }

        // ─── Message bridge ───────────────────────────────────────────────────
        function sendMessage(type, extra = {}) {
          const payload = JSON.stringify({ type, task: taskInput.value.trim() || 'Untitled', ...extra });
          try {
            if (window.webkit?.messageHandlers?.studioPro)
              window.webkit.messageHandlers.studioPro.postMessage(payload);
            else if (window.chrome?.webview)
              window.chrome.webview.postMessage(payload);
          } catch(err) { console.warn('postMessage failed:', err); }
        }

        // ─── History ──────────────────────────────────────────────────────────
        async function loadHistory() {
          try {
            const res = await fetch('./history');
            if (!res.ok) return;
            const records = await res.json();
            historyBadge.textContent = records.length;
            historyList.innerHTML = records.length === 0
              ? '<div class="list-empty">No sessions yet</div>'
              : records.map(r => `
                  <div class="history-item">
                    <span class="h-num">#${r.number}</span>
                    <span class="h-task" title="${escHtml(r.task)}">${escHtml(r.task)}</span>
                    <span class="h-time">${r.time}</span>
                  </div>`).join('');
          } catch { /* ignore */ }
        }

        // ─── Stories ──────────────────────────────────────────────────────────
        async function loadStories() {
          try {
            const res = await fetch('./stories');
            if (!res.ok) return;
            const stories = await res.json();
            // Populate datalist
            storyList.innerHTML = stories.map(s => `<option value="${escHtml(s)}"></option>`).join('');
            // Render settings list
            storyList2.innerHTML = stories.length === 0
              ? '<div class="list-empty">No stories added yet</div>'
              : stories.map(s => `
                  <div class="story-item">
                    <span class="story-name" title="${escHtml(s)}">${escHtml(s)}</span>
                    <button class="remove-btn" onclick="removeStory(${JSON.stringify(s)})" title="Remove">×</button>
                  </div>`).join('');
          } catch { /* ignore */ }
        }

        function removeStory(story) {
          sendMessage('RemoveStory', { story });
          setTimeout(loadStories, 100);
        }

        document.getElementById('addBtn').addEventListener('click', () => {
          const val = addInput.value.trim();
          if (!val) return;
          sendMessage('AddStory', { story: val });
          addInput.value = '';
          setTimeout(loadStories, 100);
        });

        addInput.addEventListener('keydown', e => {
          if (e.key === 'Enter') document.getElementById('addBtn').click();
        });

        // ─── View switching ───────────────────────────────────────────────────
        document.getElementById('gearBtn').addEventListener('click', () => {
          loadStories();
          document.getElementById('mainView').classList.remove('active');
          document.getElementById('settingsView').classList.add('active');
        });

        document.getElementById('backBtn').addEventListener('click', () => {
          loadStories(); // refresh datalist before going back
          document.getElementById('settingsView').classList.remove('active');
          document.getElementById('mainView').classList.add('active');
        });

        // ─── Timer ────────────────────────────────────────────────────────────
        function tick() {
          if (remaining <= 0) {
            clearInterval(intervalId); intervalId = null; running = false;
            onPhaseComplete(); return;
          }
          remaining--;
          updateUI();
        }

        function onPhaseComplete() {
          if (phase === 'work') {
            completedWork++;
            sendMessage('WorkComplete');
            setTimeout(loadHistory, 200);
            phase = completedWork >= 4 ? 'longBreak' : 'shortBreak';
            remaining = totalDuration = DURATIONS[phase];
          } else if (phase === 'shortBreak') {
            sendMessage('BreakComplete');
            phase = 'work'; remaining = totalDuration = DURATIONS.work;
          } else if (phase === 'longBreak') {
            sendMessage('BreakComplete');
            completedWork = 0; phase = 'work'; remaining = totalDuration = DURATIONS.work;
          }
          updateUI();
          startBtn.textContent = 'Start'; startBtn.disabled = false; pauseBtn.disabled = true;
        }

        startBtn.addEventListener('click', () => {
          if (running) return;
          running = true;
          taskInput.disabled = true;
          startBtn.textContent = 'Running…'; startBtn.disabled = true; pauseBtn.disabled = false;
          intervalId = setInterval(tick, 1000);
        });

        pauseBtn.addEventListener('click', () => {
          if (!running) return;
          clearInterval(intervalId); intervalId = null; running = false;
          taskInput.disabled = false;
          startBtn.textContent = 'Resume'; startBtn.disabled = false; pauseBtn.disabled = true;
        });

        resetBtn.addEventListener('click', () => {
          clearInterval(intervalId); intervalId = null; running = false;
          remaining = totalDuration; taskInput.disabled = false;
          startBtn.textContent = 'Start'; startBtn.disabled = false; pauseBtn.disabled = true;
          updateUI();
        });

        // ─── Init ─────────────────────────────────────────────────────────────
        updateUI();
        loadHistory();
        loadStories();
      </script>
    </body>
    </html>
    """;

    public override void InitializeWebServer(IWebServer webServer)
    {
        webServer.AddRoute("pomodoro", async (_, response, _) =>
        {
            response.ContentType = "text/html";
            response.StatusCode = 200;
            var bytes = System.Text.Encoding.UTF8.GetBytes(HtmlContent);
            response.ContentLength64 = bytes.Length;
            await response.OutputStream.WriteAsync(bytes);
        });

        webServer.AddRoute("history", async (_, response, _) =>
        {
            var records = historyStore.GetAll().Select(r => new
            {
                number = r.Number,
                task   = r.Task,
                time   = r.CompletedAt.ToString("HH:mm")
            });
            var json = JsonSerializer.Serialize(records);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            response.ContentType = "application/json";
            response.StatusCode = 200;
            response.ContentLength64 = bytes.Length;
            await response.OutputStream.WriteAsync(bytes);
        });

        webServer.AddRoute("stories", async (_, response, _) =>
        {
            var json = JsonSerializer.Serialize(storyStore.GetAll());
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            response.ContentType = "application/json";
            response.StatusCode = 200;
            response.ContentLength64 = bytes.Length;
            await response.OutputStream.WriteAsync(bytes);
        });
    }
}
