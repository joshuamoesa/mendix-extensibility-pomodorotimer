using System.ComponentModel.Composition;
using Mendix.StudioPro.ExtensionsAPI.UI.WebServer;

namespace MyCompany.MyProject.PomodoroTimer;

[Export(typeof(WebServerExtension))]
public class PomodoroWebServer : WebServerExtension
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
          }
        }

        body {
          font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
          background: var(--bg);
          color: var(--text);
          display: flex;
          flex-direction: column;
          align-items: center;
          justify-content: center;
          min-height: 100vh;
          padding: 24px;
          gap: 24px;
        }

        .phase-label {
          font-size: 13px;
          font-weight: 600;
          letter-spacing: 0.1em;
          text-transform: uppercase;
          color: var(--subtext);
        }

        /* Circular timer ring */
        .ring-container {
          position: relative;
          width: 200px;
          height: 200px;
        }

        .ring-svg {
          transform: rotate(-90deg);
          width: 200px;
          height: 200px;
        }

        .ring-track {
          fill: none;
          stroke: var(--ring-track);
          stroke-width: 8;
        }

        .ring-progress {
          fill: none;
          stroke: var(--work-color);
          stroke-width: 8;
          stroke-linecap: round;
          transition: stroke-dashoffset 0.9s linear, stroke 0.3s ease;
        }

        .ring-center {
          position: absolute;
          top: 50%;
          left: 50%;
          transform: translate(-50%, -50%);
          text-align: center;
        }

        .time-display {
          font-size: 42px;
          font-weight: 300;
          font-variant-numeric: tabular-nums;
          letter-spacing: -1px;
          line-height: 1;
        }

        /* Session dots */
        .dots {
          display: flex;
          gap: 10px;
          align-items: center;
        }

        .dot {
          width: 12px;
          height: 12px;
          border-radius: 50%;
          background: var(--dot-empty);
          transition: background 0.3s ease, transform 0.3s ease;
        }

        .dot.filled {
          background: var(--work-color);
          transform: scale(1.1);
        }

        /* Controls */
        .controls {
          display: flex;
          gap: 12px;
        }

        button {
          padding: 9px 20px;
          border: none;
          border-radius: 6px;
          background: var(--btn-bg);
          color: var(--text);
          font-size: 13px;
          font-weight: 500;
          cursor: pointer;
          transition: background 0.15s ease;
          min-width: 72px;
        }

        button:hover {
          background: var(--btn-hover);
        }

        #startBtn {
          background: var(--work-color);
          color: #fff;
          min-width: 88px;
        }

        #startBtn:hover {
          filter: brightness(1.1);
        }

        .session-info {
          font-size: 12px;
          color: var(--subtext);
        }
      </style>
    </head>
    <body>

      <span class="phase-label" id="phaseLabel">Focus</span>

      <div class="ring-container">
        <svg class="ring-svg" viewBox="0 0 200 200">
          <circle class="ring-track" cx="100" cy="100" r="88" />
          <circle class="ring-progress" id="ringProgress" cx="100" cy="100" r="88"
            stroke-dasharray="552.92"
            stroke-dashoffset="0" />
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

      <script>
        // ─── Constants ────────────────────────────────────────────────────────
        const DURATIONS = {
          work:       25 * 60,
          shortBreak:  5 * 60,
          longBreak:  15 * 60,
        };

        const PHASE_LABELS = {
          work:       'Focus',
          shortBreak: 'Short Break',
          longBreak:  'Long Break',
        };

        const PHASE_COLORS = {
          work:       'var(--work-color)',
          shortBreak: 'var(--break-color)',
          longBreak:  'var(--long-break-color)',
        };

        const CIRCUMFERENCE = 2 * Math.PI * 88; // ≈ 552.92

        // ─── State ────────────────────────────────────────────────────────────
        let phase         = 'work';    // 'work' | 'shortBreak' | 'longBreak'
        let completedWork = 0;         // work sessions completed this cycle (0–3)
        let remaining     = DURATIONS.work;
        let totalDuration = DURATIONS.work;
        let running       = false;
        let intervalId    = null;

        // ─── DOM refs ─────────────────────────────────────────────────────────
        const timeDisplay  = document.getElementById('timeDisplay');
        const phaseLabel   = document.getElementById('phaseLabel');
        const ringProgress = document.getElementById('ringProgress');
        const sessionInfo  = document.getElementById('sessionInfo');
        const startBtn     = document.getElementById('startBtn');
        const pauseBtn     = document.getElementById('pauseBtn');
        const resetBtn     = document.getElementById('resetBtn');
        const dots         = [0,1,2,3].map(i => document.getElementById('dot' + i));

        // ─── Helpers ──────────────────────────────────────────────────────────
        function pad(n) { return String(n).padStart(2, '0'); }

        function formatTime(secs) {
          return pad(Math.floor(secs / 60)) + ':' + pad(secs % 60);
        }

        function updateRing() {
          const progress = remaining / totalDuration;
          ringProgress.style.strokeDashoffset = CIRCUMFERENCE * (1 - progress);
          ringProgress.style.stroke = PHASE_COLORS[phase];
        }

        function updateDots() {
          dots.forEach((dot, i) => {
            dot.classList.toggle('filled', i < completedWork);
          });
        }

        function updateUI() {
          timeDisplay.textContent = formatTime(remaining);
          phaseLabel.textContent  = PHASE_LABELS[phase];
          updateRing();
          updateDots();

          if (phase === 'work') {
            sessionInfo.textContent = 'Session ' + (completedWork + 1) + ' of 4';
          } else {
            sessionInfo.textContent = phase === 'longBreak' ? 'Long break — well earned!' : 'Short break';
          }

          startBtn.style.background = PHASE_COLORS[phase];
        }

        // ─── Message bridge (cross-platform) ─────────────────────────────────
        function sendMessage(msg) {
          try {
            if (window.webkit && window.webkit.messageHandlers && window.webkit.messageHandlers.studioPro) {
              window.webkit.messageHandlers.studioPro.postMessage(JSON.stringify({ message: msg }));
            } else if (window.chrome && window.chrome.webview) {
              window.chrome.webview.postMessage({ message: msg });
            }
          } catch (err) {
            console.warn('postMessage failed:', err);
          }
        }

        // ─── Timer tick ───────────────────────────────────────────────────────
        function tick() {
          if (remaining <= 0) {
            clearInterval(intervalId);
            intervalId = null;
            running    = false;
            onPhaseComplete();
            return;
          }
          remaining--;
          updateUI();
        }

        // ─── Phase transitions ────────────────────────────────────────────────
        function onPhaseComplete() {
          if (phase === 'work') {
            completedWork++;
            if (completedWork >= 4) {
              sendMessage('WorkComplete');
              phase         = 'longBreak';
              remaining     = DURATIONS.longBreak;
              totalDuration = DURATIONS.longBreak;
            } else {
              sendMessage('WorkComplete');
              phase         = 'shortBreak';
              remaining     = DURATIONS.shortBreak;
              totalDuration = DURATIONS.shortBreak;
            }
          } else if (phase === 'shortBreak') {
            sendMessage('BreakComplete');
            phase         = 'work';
            remaining     = DURATIONS.work;
            totalDuration = DURATIONS.work;
          } else if (phase === 'longBreak') {
            sendMessage('BreakComplete');
            // Reset full cycle
            completedWork = 0;
            phase         = 'work';
            remaining     = DURATIONS.work;
            totalDuration = DURATIONS.work;
          }

          updateUI();
          startBtn.textContent = 'Start';
          pauseBtn.disabled    = true;
        }

        // ─── Button handlers ──────────────────────────────────────────────────
        startBtn.addEventListener('click', () => {
          if (running) return;
          running = true;
          startBtn.textContent = 'Running…';
          startBtn.disabled    = true;
          pauseBtn.disabled    = false;
          intervalId = setInterval(tick, 1000);
        });

        pauseBtn.addEventListener('click', () => {
          if (!running) return;
          clearInterval(intervalId);
          intervalId           = null;
          running              = false;
          startBtn.textContent = 'Resume';
          startBtn.disabled    = false;
          pauseBtn.disabled    = true;
        });

        resetBtn.addEventListener('click', () => {
          clearInterval(intervalId);
          intervalId           = null;
          running              = false;
          remaining            = totalDuration;
          startBtn.textContent = 'Start';
          startBtn.disabled    = false;
          pauseBtn.disabled    = true;
          updateUI();
        });

        // ─── Init ─────────────────────────────────────────────────────────────
        updateUI();
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
    }
}
