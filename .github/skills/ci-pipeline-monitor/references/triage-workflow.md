# Triage Workflow

**Summary:** For each untriaged row in `test_results` (`WHERE failure_id IS NULL`):
1. Read the full console log file at `console_log_path`
2. Extract error_message and stack_trace verbatim (see [`verbatim-rules.md`](verbatim-rules.md))
3. Classify (timeout/crash/assertion) using BOTH exit code and error message
4. Group by root cause (compare error messages, not just exit codes)
5. Search GitHub for matching issues (multi-pass: test name → class/method → error signature)
6. Write analysis, INSERT into `failures`/`failure_pipelines`/`failure_tests`, UPDATE `test_results.failure_id`

**⚠️ INSERT into `failures` table immediately after triaging each failure group.**

**⚠️ Validation:** After all triage: `SELECT COUNT(*) FROM test_results WHERE failure_id IS NULL` must be 0.

---

## Detailed Instructions

**Input:** `test_results` table — every individual failure with its exit code,
`console_log_path` (path to the full Helix console log file on disk), and
API-provided `error_message`/`stack_trace` where available (populated by
`extract_failed_tests.py` and `fetch_helix_logs.py`). Some rows already
have useful error/stack from the API; others have empty values (crashes,
timeouts, generic Helix messages) and need the LLM to extract from the
console log.

For each row in `test_results` without a `failure_id` (query: `SELECT * FROM test_results WHERE failure_id IS NULL`):

1.  **Check if the API already provided a useful error_message.** If
    `error_message` is non-empty and non-generic, use it as the starting
    point. The API `errorMessage`/`stackTrace` is correct for most xUnit
    assertion failures (Assert.Equal, Assert.True, Assert.Throws, etc.).
2.  **Open and read the full console log file** at `console_log_path` using
    the `view` tool. Read the ENTIRE file — do not read only the tail.
    The error context may be anywhere in the log (beginning, middle, or end).
    **Even when the API provided an error**, read the console log to verify
    and enrich — the console log often has more context (e.g., full stack
    traces, assert details, [Long Running Test] warnings, stress mode info).
3.  **Extract or update error_message and stack_trace by copy-pasting
    verbatim** from the console log file. See
    [`verbatim-rules.md`](verbatim-rules.md) for detailed rules.
    The LLM should produce the most complete version — combining
    API data with console log context. For example, the API may have a
    3-frame stack trace while the console log has 10 frames.
    **⚠️ Extract the managed .NET exception, NOT native debugger output.**
    Search near where the test started (after `Discovered:` / `Starting:`
    lines) for `Fatal error.` + the .NET exception type (e.g.,
    `System.AccessViolationException`, `OutOfMemoryException`). The managed
    stack trace (with `at Namespace.Class.Method(...)` frames) is the
    error_message. Native debugger output at the end of the log (e.g.,
    `Access violation - code c0000005`, `KERNELBASE!RaiseFailFastException`)
    belongs in the `analysis` field, NOT in `error_message`.
4.  **UPDATE the `test_results` row** with the extracted `error_message` and
    `stack_trace`.
5.  **Review exit code AND error message together.** Same exit code does NOT
    always mean the same root cause (e.g., exit code 1 can be an assertion
    failure, an OOM, or a config error — only the error message tells you which).
5.  **Classify** the failure: timeout (137), crash (139/134), test assertion,
    etc. Use BOTH the exit code and the actual error message content.
6.  **Group by root cause.** Different test names CAN be the same failure
    (same error pattern). Same test name CAN be different failures in
    different pipelines (different error patterns). Compare actual error
    messages — not just exit codes.
7.  **Search GitHub** for matching issues (`github-mcp-server-search_issues`
    in `dotnet/runtime`, open AND closed, no date restriction). Use multi-pass:

    - **Pass 1 — Full test name from ADO Test Results** (most important — most
      issues are filed with the exact test name as shown in ADO):
      Assembly-level: `System.Text.RegularExpressions.Tests`
      Path-style: `Interop/COM/NETClients/Events/NETClientEvents/NETClientEvents.cmd`
      FQN method: `System.Diagnostics.Metrics.Tests.MetricsTests.PassingVariableTagsParametersTest`
    - **Pass 2 — Class/method name:** `StackTraceTests.ToString_ShowILOffset`
    - **Pass 3 — Work item name:** `System.Collections.Tests jitstress`
    - **Pass 4 — Error signature:** `fgprofile.cpp histogramSchemaIndex`,
      `ACCESS_VIOLATION WinHttpHandler`, `OutOfMemoryException RegularExpressions`
    - **Pass 5 — Stress mode + pattern:** `jitstress SIGSEGV`
    - **Pass 6 — Error message in issue body:** Search for the .NET exception
      type or key error phrase (e.g., `AccessViolationException XxHash`,
      `System.AccessViolationException MergeAccumulators`). Issues often
      contain the error message and test name in their **body** text, not
      just the title. A single issue may cover many different test names
      that share the same crash signature — search by the error pattern to
      find these umbrella issues.

    Do NOT mark as NEW until all passes return 0 results. Prefer open over
    closed, newer over older. Always report closed matches.

    **⚠️ Do NOT restrict GitHub issue searches by pipeline name.** The same
    root cause (e.g., `AccessViolationException` in `XxHash`) can manifest
    in completely different pipelines (jitstressregs, libraries-pgo,
    jitosr_stress_random, etc.). An issue filed against one pipeline often
    covers the same crash in others. Search by error pattern and test name
    only — never filter by pipeline.

    **Verification — match by error pattern, not just test name:**
    - When a search returns candidate issues, open each one
      (`github-mcp-server-issue_read` with method `get`) and confirm the
      error message or stack trace described in the issue matches the actual
      console log. A test can fail for different reasons than what an existing
      issue describes (e.g., issue is about OOM but current failure is
      SIGSEGV). If the error patterns don't match, the issue is NOT a match
      — continue searching.
    - **⚠️ Read issue comments when the body alone doesn't confirm a match.**
      Use `github-mcp-server-issue_read` with method `get_comments` when:
      (a) the issue title/body describes a different test or stack trace but
      the same exception type under the same stress mode, or (b) all search
      passes returned 0 results and you are about to mark as NEW. Umbrella
      issues accumulate new failure manifestations in comments over time —
      the body may describe a WinHttpHandler AV, but a comment posted days
      later may list the exact same source-generator test assemblies you are
      triaging. If the body already contains a matching error message, stack
      trace, and test name, reading comments is unnecessary.
    - Conversely, a single GitHub issue may cover **multiple different test
      names** that share the same root cause (e.g., several tests all hitting
      the same JIT assertion or all timing out under the same stress mode).
      When an issue lists multiple test names, match it to ALL current
      failures that show the same error pattern — even if the failing test
      name differs from the one used in the search query.
    - **Match by shared root cause, not identical stack traces.** The same
      JIT codegen bug can produce AccessViolationException in completely
      different methods (e.g., WinHttpHandler in one pipeline, XxHash in
      another). If failures share the same exception type under the same
      stress mode and were introduced around the same time, they likely share
      a root cause — check whether an existing issue covers them even if the
      stack traces differ.

8.  **Write analysis** (1-3 paragraphs).
9.  **INSERT into DB:**
    - `failures`: one row per failure group (title, test_name, error_message,
      stack_trace verbatim, summary, analysis, github_issue_* fields).
      **Set `source_test_result_id`** to the `test_results.id` from which the
      error_message and stack_trace were extracted — the report and validator
      use this to trace back to the specific console log.
    - `failure_pipelines`: one row per (failure_id, pipeline_name, build_id,
      build_number). Populate `build_id` and `build_number` from the
      `pipelines` table — the report uses these to construct AzDO URLs.
    - `failure_tests`: one row per (failure_id, pipeline_name, run_name, test_name)
    - **UPDATE `test_results` SET `failure_id`** for every row assigned to
      this group.

**⚠️ Validation:** After all triage is complete, verify no failures were missed:
```sql
SELECT COUNT(*) FROM test_results WHERE failure_id IS NULL;
-- Must be 0. If not, those failures were skipped — go back and triage them.
```
