# Verbatim Copy-Paste Rules

**Error messages and stack traces MUST be verbatim copy-paste from the
console log file.** This is the most important rule for data quality.

- **Copy-paste exactly** — every character, dot, cross, newline, whitespace,
  indentation, xUnit formatting, `[FAIL]` markers, assertion output,
  `Expected:`/`Actual:` lines, `Stack Trace:` sections, native frame
  addresses, `---End of stack trace---` markers, `[Long Running Test]`
  warnings, `[createdump]` lines, and exit code lines.
- **Never summarize, paraphrase, rephrase, or truncate** the error message
  or stack trace. If the console log says `"Assert.Equal() Failure: Values
  differ\n                  Expected: 5\n                  Actual:   9"`,
  store exactly that — including the whitespace alignment.
- **Never reformat** — do not remove newlines, collapse whitespace, change
  indentation, strip ANSI codes, add markdown formatting, or wrap in
  backticks. Store the raw text as it appears in the file.
- **Include sufficient context** — for the error_message, include from the
  first line that describes the error through the last relevant line
  (typically before `[createdump]` for crashes, or the full xUnit failure
  block for test assertions). Do not include unrelated log noise before/after.
  **⚠️ Never cut a line in the middle.** If you include a line, include the
  entire line. For SIGSEGV/crash exits, include the full shell error line
  (with the command that segfaulted), the exit code line, and the exit code
  explanation line (e.g., `exit code 139 means SIGSEGV ...`).
- **Include the complete stack trace** — every `at ...` line, every
  `--- End of stack trace from previous location ---` line, every
  `?? at ??:0:0` unresolved frame. Do not stop at 5 frames when there are 20.
  **⚠️ Err on the side of grabbing MORE lines, not fewer.** A 2-frame stack
  trace is almost always incomplete — look for the full chain from the
  assertion/exception through the test entry point. Include frames with file
  paths (e.g., `in /_/src/libraries/.../Tests.cs:line 584`) and reflection
  invoker frames (`MethodBaseInvoker.InterpretedInvoke_Method`,
  `InvokeWithNoArgs`). Never cut a stack trace in the middle of a line.
- **For crash errors** (SIGSEGV, SIGABRT, AV): the error_message is the
  **managed .NET exception** (e.g., `Fatal error.\nSystem.AccessViolationException:
  ...`) found near the test start (`Discovered:` / `Starting:` lines), NOT
  the native debugger summary at the end of the log. Extract the managed
  stack trace (`at Namespace.Class.Method(...)` frames). Native debugger
  output (`Access violation - code c0000005`, `KERNELBASE!...`,
  `coreclr!...`) should be noted in the `analysis` field only. If no managed
  exception exists, then extract from **above** `[createdump]` lines — look
  backwards for C++ assertions, native stack frames, `Segmentation fault`,
  or diagnostic output.
- **Stack traces may be unresolved** (`?? at ??:0:0`) — copy verbatim anyway.
  Note "(unresolved)" after the stack trace block.
- Use actual test leg names from AzDO (e.g.,
  `net11.0-linux-Release-arm64-jitstress2_jitstressregs0x2000-...`),
  not a summarized form like `coreclr linux arm64 Checked jitstress2`.
- Use fully qualified `automatedTestName` — never shorten.
- Report ALL failed test legs — if a test fails in 3 legs, list all 3.
- Never file issues or add comments automatically — log for manual action.
- Always check for existing issues before flagging as NEW.
- **Verify issue matches by error pattern, not just test name** — open each
  candidate issue and confirm its described error/stack trace matches the
  actual console log. Same test + different error = not a match. Conversely,
  one issue may cover multiple test names with the same root cause — match
  it to all failures sharing that error pattern.
- **Always read issue comments** — umbrella issues accumulate new crash
  manifestations in comments. The body may describe failure A, but a
  comment may list the exact tests you are triaging. Use
  `github-mcp-server-issue_read` with method `get_comments` on candidate
  issues when the body doesn't confirm a match, or before marking as NEW.
- **After triage, verify completeness:** `SELECT COUNT(*) FROM test_results
  WHERE failure_id IS NULL` must return 0. Every row must be assigned to a
  failure group.
