export function renderHtml() {
    return `<!doctype html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>LXR GC Roadmap</title>
    <style>
        :root { color-scheme: light dark; }
        * { box-sizing: border-box; }
        body {
            margin: 0;
            background: var(--background-color-default, #ffffff);
            color: var(--text-color-default, #1f2328);
            font-family: var(--font-sans, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif);
            font-size: var(--text-body-medium, 14px);
            line-height: var(--leading-body-medium, 20px);
        }
        button, select { font: inherit; }
        button, select {
            border: 1px solid var(--border-color-default, #d0d7de);
            border-radius: 6px;
            background: var(--background-color-default, #ffffff);
            color: var(--text-color-default, #1f2328);
        }
        button { padding: 6px 12px; cursor: pointer; font-weight: var(--font-weight-semibold, 600); }
        button:hover { background: color-mix(in srgb, var(--background-color-default, #fff) 90%, var(--true-color-blue, #0969da)); }
        button:disabled { cursor: not-allowed; opacity: .55; }
        button.primary { background: var(--true-color-blue, #0969da); border-color: transparent; color: var(--color-white, #ffffff); }
        select { width: 100%; padding: 7px 9px; }
        .shell { max-width: 1800px; margin: 0 auto; padding: 20px; }
        .header { display: flex; justify-content: space-between; align-items: flex-start; gap: 20px; margin-bottom: 14px; }
        h1 { margin: 0 0 4px; font-size: var(--text-title-large, 26px); line-height: var(--leading-title-large, 32px); }
        h2 { font-size: 18px; margin: 0; }
        h3 { font-size: 15px; margin: 0; line-height: 20px; }
        .muted { color: var(--text-color-muted, #656d76); }
        .summary-grid { display: grid; grid-template-columns: 2fr repeat(3, minmax(90px, 1fr)); gap: 8px; margin-bottom: 12px; }
        .card, .chart-card, .phase-column {
            border: 1px solid var(--border-color-default, #d0d7de);
            border-radius: 8px;
            background: color-mix(in srgb, var(--background-color-default, #fff) 97%, var(--text-color-default, #1f2328));
        }
        .card { padding: 10px 12px; }
        .card strong { display: block; font-size: 20px; line-height: 24px; }
        .progress { height: 7px; display: flex; overflow: hidden; border-radius: 999px; background: var(--border-color-default, #d0d7de); margin-top: 7px; }
        .done-bg { background: #2da44e; }
        .failed-bg { background: #cf222e; }
        .in-progress-bg { background: #0969da; }
        .planned-bg { background: #8c959f; }
        .chart-card { padding: 14px; margin-bottom: 14px; }
        .chart-head { display: flex; justify-content: space-between; gap: 14px; align-items: flex-start; margin-bottom: 8px; }
        .chart-controls { display: grid; grid-template-columns: 1.2fr 1fr; gap: 8px; min-width: 320px; }
        #chart { width: 100%; height: 250px; display: block; }
        .legend { display: flex; gap: 18px; justify-content: center; margin-top: 2px; font-size: 12px; }
        .legend span::before { content: ""; width: 9px; height: 9px; display: inline-block; border-radius: 50%; margin-right: 5px; background: var(--legend-color); }
        .benchmark-note { margin-top: 8px; font-size: 12px; }
        .issue { margin-top: 6px; font-size: 12px; padding: 5px 8px; border-radius: 6px; background: var(--background-color-muted, #f6f8fa); color: var(--text-color-muted, #656d76); }
        .issue.error { background: #ffebe9; color: #cf222e; }
        .board {
            display: grid;
            grid-auto-flow: column;
            grid-auto-columns: minmax(230px, 1fr);
            gap: 10px;
            overflow-x: auto;
            align-items: start;
            padding-bottom: 10px;
            scroll-snap-type: x proximity;
        }
        .phase-column { min-height: 180px; overflow: hidden; scroll-snap-align: start; }
        .phase-header {
            padding: 11px 12px;
            border-bottom: 1px solid var(--border-color-default, #d0d7de);
            background: color-mix(in srgb, var(--background-color-default, #fff) 94%, var(--true-color-blue-muted, #ddf4ff));
        }
        .phase-heading { display: flex; gap: 8px; align-items: flex-start; }
        .phase-heading h3 { flex: 1; }
        .phase-count { white-space: nowrap; font-size: 12px; }
        .phase-list { display: grid; gap: 7px; padding: 8px; }
        .step-card {
            width: 100%;
            display: grid;
            grid-template-columns: auto 1fr;
            align-items: start;
            gap: 8px;
            padding: 9px;
            text-align: left;
            border-radius: 7px;
            box-shadow: 0 1px 0 color-mix(in srgb, var(--border-color-default, #d0d7de) 65%, transparent);
        }
        .step-card:focus-visible { outline: 2px solid var(--color-focus-outline, #0969da); outline-offset: 1px; }
        .step-name { font-weight: var(--font-weight-semibold, 600); line-height: 18px; }
        .step-id { display: block; font-size: 11px; color: var(--text-color-muted, #656d76); font-weight: 400; }
        .status-dot { width: 9px; height: 9px; border-radius: 50%; margin-top: 4px; background: #8c959f; }
        .status-dot.done { background: #2da44e; }
        .status-dot.failed { background: #cf222e; }
        .status-dot.in_progress { background: #0969da; }
        .runtime-badge {
            display: inline-flex;
            margin-top: 5px;
            border-radius: 999px;
            padding: 1px 7px;
            color: #0550ae;
            background: #ddf4ff;
            font-size: 10px;
            line-height: 16px;
            font-weight: 600;
        }
        dialog {
            width: min(720px, calc(100vw - 28px));
            max-height: min(82vh, 820px);
            padding: 0;
            border: 1px solid var(--border-color-default, #d0d7de);
            border-radius: 10px;
            background: var(--background-color-default, #ffffff);
            color: var(--text-color-default, #1f2328);
            box-shadow: 0 16px 48px #0006;
        }
        dialog::backdrop { background: #0008; }
        .modal-header { display: flex; align-items: flex-start; gap: 12px; padding: 16px 18px; border-bottom: 1px solid var(--border-color-default, #d0d7de); }
        .modal-title { flex: 1; }
        .modal-title h2 { margin-top: 3px; }
        .modal-close { padding: 3px 9px; font-size: 18px; }
        .modal-body { padding: 18px; overflow-y: auto; display: grid; gap: 16px; }
        .modal-footer { display: flex; justify-content: space-between; align-items: center; gap: 12px; padding: 12px 18px; border-top: 1px solid var(--border-color-default, #d0d7de); }
        .modal-action-status { font-size: 12px; }
        .detail-block strong { display: block; margin-bottom: 4px; font-size: 12px; text-transform: uppercase; letter-spacing: .04em; color: var(--text-color-muted, #656d76); }
        .detail-block p { margin: 0; white-space: pre-wrap; }
        .evidence-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
        .evidence-grid .detail-block { padding: 11px; border-left: 3px solid var(--border-color-default, #d0d7de); }
        .badge { display: inline-flex; border-radius: 999px; padding: 2px 8px; font-size: 11px; font-weight: 600; text-transform: uppercase; }
        .badge.done { color: #1a7f37; background: #dafbe1; }
        .badge.failed { color: #cf222e; background: #ffebe9; }
        .badge.in_progress { color: #0969da; background: #ddf4ff; }
        .badge.planned { color: var(--text-color-muted, #656d76); background: color-mix(in srgb, var(--background-color-default, #fff) 75%, #8c959f); }
        .notice { padding: 9px 11px; border-radius: 6px; margin-bottom: 10px; display: none; }
        .notice.visible { display: block; }
        .notice.error { background: #ffebe9; color: #cf222e; }
        @media (max-width: 760px) {
            .shell { padding: 12px; }
            .header, .chart-head { display: block; }
            .header button { margin-top: 8px; }
            .summary-grid { grid-template-columns: repeat(3, 1fr); }
            .summary-grid .card:first-child { grid-column: 1 / -1; }
            .chart-controls, .evidence-grid { grid-template-columns: 1fr; min-width: 0; }
            .board { grid-auto-columns: minmax(220px, 84vw); }
        }
    </style>
</head>
<body>
    <main class="shell">
        <header class="header">
            <div>
                <h1>LXR GC roadmap</h1>
                <div class="muted">Paper fidelity, correctness gates, and performance against .NET Workstation and Server GC</div>
            </div>
            <button id="refresh">Refresh</button>
        </header>
        <div id="notice" class="notice"></div>
        <div id="summary" class="summary-grid"></div>
        <section class="chart-card">
            <div class="chart-head">
                <div>
                    <h2>Benchmark progression</h2>
                    <div class="muted">Loaded from agent-produced files in <code>benchmark-results/</code>.</div>
                </div>
                <div class="chart-controls">
                    <select id="scenario" aria-label="Benchmark scenario"></select>
                    <select id="metric" aria-label="Benchmark metric">
                        <option value="latencyP99Ms">App latency p99 (ms)</option>
                        <option value="latencyP999Ms">App latency p99.9 (ms)</option>
                        <option value="latencyP9999Ms">App latency p99.99 (ms)</option>
                        <option value="latencyP50Ms">App latency p50 (ms)</option>
                        <option value="latencyMaxMs">App latency max (ms)</option>
                        <option value="pauseP99Ms">Pause p99 (ms)</option>
                        <option value="pauseAverageMs">Pause average (ms)</option>
                        <option value="pauseMaxMs">Pause max (ms)</option>
                        <option value="workingSetMb">Working set (MiB)</option>
                        <option value="committedMb">Committed (MiB)</option>
                        <option value="operationsPerSecond">Throughput (ops/s)</option>
                    </select>
                </div>
            </div>
            <svg id="chart" role="img" aria-label="Benchmark progression chart"></svg>
            <div class="legend">
                <span style="--legend-color:#8250df">LXR</span>
                <span style="--legend-color:#0969da">Workstation</span>
                <span style="--legend-color:#1a7f37">Server</span>
            </div>
            <div id="benchmark-note" class="benchmark-note muted"></div>
            <div id="benchmark-issues"></div>
        </section>
        <section id="board" class="board" aria-label="Implementation phases"></section>
    </main>
    <dialog id="step-modal">
        <div class="modal-header">
            <div class="modal-title">
                <span id="modal-status" class="badge"></span>
                <h2 id="modal-title"></h2>
            </div>
            <button id="modal-close" class="modal-close" aria-label="Close">&times;</button>
        </div>
        <div class="modal-body">
            <div class="detail-block"><strong>Summary</strong><p id="modal-summary"></p></div>
            <div class="evidence-grid">
                <div class="detail-block"><strong>Correctness</strong><p id="modal-correctness"></p></div>
                <div class="detail-block"><strong>Benchmarks</strong><p id="modal-benchmarks"></p></div>
            </div>
            <div id="modal-runtime-block" class="detail-block"><strong>Runtime changes</strong><p id="modal-runtime"></p></div>
            <div id="modal-evidence-block" class="detail-block"><strong>Evidence</strong><p id="modal-evidence"></p></div>
            <div id="modal-extra-blocks"></div>
            <div class="detail-block"><strong>Dependencies</strong><p id="modal-dependencies"></p></div>
            <div class="detail-block"><strong>References</strong><p id="modal-references"></p></div>
        </div>
        <div class="modal-footer">
            <span id="modal-action-status" class="modal-action-status muted">Creates a child App session in Plan mode.</span>
            <button id="implement-step" class="primary">Implement</button>
        </div>
    </dialog>
    <script>
        "use strict";
        var state;
        var lastPayload = null;
        var openStepId = null;
        var apiToken = new URLSearchParams(location.search).get("token") || "";
        var colors = { lxr: "#8250df", workstation: "#0969da", server: "#1a7f37" };

        function escapeHtml(value) {
            return String(value ?? "").replace(/[&<>"']/g, function (character) {
                return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[character];
            });
        }

        function showError(message) {
            var notice = document.getElementById("notice");
            notice.textContent = message;
            notice.className = "notice visible error";
        }

        async function request(path, options) {
            options = options || {};
            options.headers = Object.assign({}, options.headers, { "X-LXR-Roadmap-Token": apiToken });
            var response = await fetch(path, options);
            var value = await response.json();
            if (!response.ok) throw new Error(value.error || "Request failed.");
            return value;
        }

        function renderSummary() {
            var progress = state.progress;
            var inProgressCount = progress.inProgress || 0;
            var donePercent = progress.total ? progress.done / progress.total * 100 : 0;
            var failedPercent = progress.total ? progress.failed / progress.total * 100 : 0;
            var inProgressPercent = progress.total ? inProgressCount / progress.total * 100 : 0;
            var plannedPercent = 100 - donePercent - failedPercent - inProgressPercent;
            document.getElementById("summary").innerHTML =
                '<div class="card"><span class="muted">Progress</span><strong>' + progress.done + ' / ' + progress.total + '</strong>' +
                '<div class="progress"><span class="done-bg" style="width:' + donePercent + '%"></span><span class="failed-bg" style="width:' + failedPercent + '%"></span><span class="in-progress-bg" style="width:' + inProgressPercent + '%"></span><span class="planned-bg" style="width:' + plannedPercent + '%"></span></div></div>' +
                '<div class="card"><span class="muted">In progress</span><strong>' + inProgressCount + '</strong></div>' +
                '<div class="card"><span class="muted">Planned</span><strong>' + progress.planned + '</strong></div>' +
                '<div class="card"><span class="muted">Failed</span><strong>' + progress.failed + '</strong></div>' +
                '<div class="card"><span class="muted">Checkpoints</span><strong>' + state.benchmarks.checkpoints.length + '</strong></div>';
        }

        function statusLabel(status) {
            return status === "in_progress" ? "in progress" : status;
        }

        function renderBoard() {
            document.getElementById("board").innerHTML = state.phases.map(function (phase) {
                var done = phase.steps.filter(function (step) { return step.status === "done"; }).length;
                return '<section class="phase-column"><header class="phase-header"><div class="phase-heading">' +
                    '<h3>' + escapeHtml(phase.id + " — " + phase.title) + '</h3>' +
                    '<span class="phase-count muted">' + done + '/' + phase.steps.length + '</span></div></header>' +
                    '<div class="phase-list">' + phase.steps.map(function (step) {
                        return '<button class="step-card" data-step="' + escapeHtml(step.id) + '">' +
                            '<span class="status-dot ' + escapeHtml(step.status) + '" title="' + escapeHtml(statusLabel(step.status)) + '"></span>' +
                            '<span class="step-name">' + escapeHtml(step.title) +
                            (step.runtimeChanges ? '<span class="runtime-badge">Runtime changes</span>' : '') +
                            '<span class="step-id">' + escapeHtml(step.id + " · " + statusLabel(step.status)) + '</span></span></button>';
                    }).join("") + '</div></section>';
            }).join("");
            document.querySelectorAll(".step-card").forEach(function (button) {
                button.addEventListener("click", function () { openStep(button.dataset.step); });
            });
        }

        function findStep(id) {
            return state.phases.flatMap(function (phase) { return phase.steps; })
                .find(function (step) { return step.id === id; });
        }

        function openStep(id) {
            var step = findStep(id);
            if (!step) return;
            openStepId = id;
            var status = document.getElementById("modal-status");
            status.textContent = statusLabel(step.status);
            status.className = "badge " + step.status;
            document.getElementById("modal-title").textContent = step.id + " — " + step.title;
            document.getElementById("modal-summary").textContent = step.summary || "No summary recorded.";
            document.getElementById("modal-correctness").textContent = step.correctness || "No correctness evidence recorded.";
            document.getElementById("modal-benchmarks").textContent = step.benchmarks || "No benchmark evidence recorded.";
            var runtimeBlock = document.getElementById("modal-runtime-block");
            runtimeBlock.hidden = !step.runtimeChanges;
            document.getElementById("modal-runtime").textContent = step.runtimeChanges || "";
            var evidenceBlock = document.getElementById("modal-evidence-block");
            evidenceBlock.hidden = !step.evidence;
            document.getElementById("modal-evidence").textContent = step.evidence || "";
            var extraHost = document.getElementById("modal-extra-blocks");
            extraHost.textContent = "";
            (step.extra || []).forEach(function (field) {
                var block = document.createElement("div");
                block.className = "detail-block";
                var label = document.createElement("strong");
                label.textContent = field.label;
                var value = document.createElement("p");
                value.textContent = field.value;
                block.appendChild(label);
                block.appendChild(value);
                extraHost.appendChild(block);
            });
            document.getElementById("modal-dependencies").textContent = step.dependencies || "None.";
            document.getElementById("modal-references").textContent = step.references || "None.";
            var implement = document.getElementById("implement-step");
            implement.dataset.step = step.id;
            implement.disabled = step.status === "done" || step.status === "in_progress";
            implement.textContent = step.status === "done"
                ? "Implemented"
                : step.status === "in_progress" ? "In progress" : "Implement";
            document.getElementById("modal-action-status").textContent = step.status === "done"
                ? "This roadmap step is already complete."
                : step.status === "in_progress"
                    ? "A child session is already working on this step."
                    : "Creates a child App session in Plan mode.";
            var modal = document.getElementById("step-modal");
            if (!modal.open) modal.showModal();
        }

        function populateScenarios() {
            var selected = document.getElementById("scenario").value;
            var scenarios = Array.from(new Set(state.benchmarks.checkpoints.flatMap(function (checkpoint) {
                return checkpoint.results.map(function (result) { return result.scenario; });
            }))).sort();
            var select = document.getElementById("scenario");
            select.innerHTML = scenarios.map(function (scenario) {
                return '<option value="' + escapeHtml(scenario) + '">' + escapeHtml(scenario) + '</option>';
            }).join("");
            if (scenarios.includes(selected)) select.value = selected;
        }

        function svgElement(name, attributes, text) {
            var element = document.createElementNS("http://www.w3.org/2000/svg", name);
            Object.entries(attributes || {}).forEach(function (pair) { element.setAttribute(pair[0], pair[1]); });
            if (text !== undefined) element.textContent = text;
            return element;
        }

        function isChartable(result, metric) {
            return result.valid !== false
                && (result.status === undefined || result.status === "ok")
                && Number.isFinite(result[metric]);
        }

        function isExcluded(result) {
            return result.valid === false || (result.status !== undefined && result.status !== "ok");
        }

        function renderChart() {
            var svg = document.getElementById("chart");
            var scenario = document.getElementById("scenario").value;
            var metric = document.getElementById("metric").value;
            var checkpoints = state.benchmarks.checkpoints.filter(function (checkpoint) {
                return checkpoint.results.some(function (result) { return result.scenario === scenario && isChartable(result, metric); });
            });
            var values = checkpoints.flatMap(function (checkpoint) {
                return checkpoint.results.filter(function (result) { return result.scenario === scenario && isChartable(result, metric); })
                    .map(function (result) { return result[metric]; });
            });
            svg.replaceChildren();
            svg.setAttribute("viewBox", "0 0 1000 250");
            if (!values.length) {
                svg.appendChild(svgElement("text", { x: 500, y: 125, "text-anchor": "middle", fill: "currentColor" }, "No benchmark results."));
                document.getElementById("benchmark-note").textContent = "";
                renderBenchmarkIssues(scenario);
                return;
            }
            var margin = { left: 76, right: 28, top: 20, bottom: 54 };
            var width = 1000 - margin.left - margin.right;
            var height = 250 - margin.top - margin.bottom;
            var max = Math.max.apply(null, values) * 1.12 || 1;
            function x(index) { return margin.left + (checkpoints.length === 1 ? width / 2 : index * width / (checkpoints.length - 1)); }
            function y(value) { return margin.top + height - value / max * height; }
            for (var tick = 0; tick <= 4; tick++) {
                var tickValue = max * tick / 4;
                var tickY = y(tickValue);
                svg.appendChild(svgElement("line", { x1: margin.left, x2: 1000 - margin.right, y1: tickY, y2: tickY, stroke: "var(--border-color-default, #d0d7de)", "stroke-width": 1 }));
                svg.appendChild(svgElement("text", { x: margin.left - 10, y: tickY + 4, "text-anchor": "end", fill: "currentColor", "font-size": 12 }, tickValue >= 1000 ? (tickValue / 1000).toFixed(1) + "k" : tickValue.toFixed(tickValue < 10 ? 1 : 0)));
            }
            checkpoints.forEach(function (checkpoint, index) {
                svg.appendChild(svgElement("text", { x: x(index), y: 222, "text-anchor": "middle", fill: "currentColor", "font-size": 11 }, checkpoint.stepId));
                svg.appendChild(svgElement("text", { x: x(index), y: 238, "text-anchor": "middle", fill: "var(--text-color-muted, #656d76)", "font-size": 10 }, checkpoint.date));
            });
            ["lxr", "workstation", "server"].forEach(function (collector) {
                var points = checkpoints.map(function (checkpoint, index) {
                    var result = checkpoint.results.find(function (item) { return item.scenario === scenario && item.collector === collector; });
                    return result && isChartable(result, metric) ? { x: x(index), y: y(result[metric]), value: result[metric] } : null;
                }).filter(Boolean);
                if (points.length > 1) {
                    svg.appendChild(svgElement("polyline", { points: points.map(function (point) { return point.x + "," + point.y; }).join(" "), fill: "none", stroke: colors[collector], "stroke-width": 3 }));
                }
                points.forEach(function (point) {
                    svg.appendChild(svgElement("circle", { cx: point.x, cy: point.y, r: 5, fill: colors[collector] }));
                    svg.appendChild(svgElement("text", { x: point.x, y: point.y - 9, "text-anchor": "middle", fill: colors[collector], "font-size": 11, "font-weight": 600 }, Number(point.value).toLocaleString(undefined, { maximumFractionDigits: 2 })));
                });
            });
            document.getElementById("benchmark-note").textContent = checkpoints.map(function (checkpoint) {
                return checkpoint.stepId + ": " + (checkpoint.notes || checkpoint.id);
            }).join(" · ");
            renderBenchmarkIssues(scenario);
        }

        function renderBenchmarkIssues(scenario) {
            var host = document.getElementById("benchmark-issues");
            host.replaceChildren();
            var problems = state.benchmarks.problems || [];
            var excluded = state.benchmarks.checkpoints.flatMap(function (checkpoint) {
                return checkpoint.results
                    .filter(function (result) { return result.scenario === scenario && isExcluded(result); })
                    .map(function (result) {
                        return checkpoint.stepId + " " + result.collector + ": "
                            + (result.invalidReason || result.skipReason || result.status || "marked invalid");
                    });
            });
            if (!problems.length && !excluded.length) {
                return;
            }
            problems.forEach(function (item) {
                var line = document.createElement("div");
                line.className = "issue error";
                line.textContent = "Ignored file " + item.file + " — it " + item.problem + ".";
                host.appendChild(line);
            });
            excluded.forEach(function (text) {
                var line = document.createElement("div");
                line.className = "issue";
                line.textContent = "Listed but not charted — " + text;
                host.appendChild(line);
            });
        }

        function renderAll() {
            renderSummary();
            renderBoard();
            populateScenarios();
            renderChart();
            if (openStepId) openStep(openStepId);
        }

        async function load(options) {
            var quiet = options && options.quiet;
            try {
                var next = await request("/api/state");
                var payload = JSON.stringify(next);
                if (quiet && payload === lastPayload) return;
                lastPayload = payload;
                state = next;
                renderAll();
                if (!quiet) {
                    var focus = new URLSearchParams(location.search).get("focus");
                    if (focus) openStep(focus);
                }
            } catch (error) {
                if (!quiet) showError(error.message);
            }
        }

        document.getElementById("refresh").addEventListener("click", function () { load(); });
        document.getElementById("scenario").addEventListener("change", renderChart);
        document.getElementById("metric").addEventListener("change", renderChart);
        document.getElementById("modal-close").addEventListener("click", function () { document.getElementById("step-modal").close(); });
        document.getElementById("implement-step").addEventListener("click", async function (event) {
            var button = event.currentTarget;
            button.disabled = true;
            button.textContent = "Starting…";
            var status = document.getElementById("modal-action-status");
            status.textContent = "Queueing the Plan-mode child session…";
            try {
                var result = await request("/api/implement", {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ stepId: button.dataset.step }),
                });
                button.textContent = "Queued";
                status.textContent = result.message;
            } catch (error) {
                button.disabled = false;
                button.textContent = "Implement";
                status.textContent = error.message;
            }
        });
        document.getElementById("step-modal").addEventListener("click", function (event) {
            if (event.target === event.currentTarget) event.currentTarget.close();
        });
        document.getElementById("step-modal").addEventListener("close", function () { openStepId = null; });
        document.addEventListener("visibilitychange", function () {
            if (!document.hidden) load({ quiet: true });
        });
        setInterval(function () {
            if (!document.hidden) load({ quiet: true });
        }, 4000);
        load();
    </script>
</body>
</html>`;
}
