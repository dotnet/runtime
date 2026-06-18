#!/usr/bin/env dotnet
#:property PublishAot=false
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// CoreCLR-WASI test runner.
//
// Discovers per-test .sh wrappers under the configured test-bin root,
// invokes each under bash (which in turn shells wasmtime via the
// generated CLRTest wrapper), and aggregates pass/fail/timeout counts.
// Each wrapper reports success via Expected/Actual lines + a non-zero
// CLRTestExitCode is treated as failure. A test is considered to have
// passed when its bash wrapper exits with code 0.
//
// Usage (from repo root):
//
//   ./.dotnet/dotnet src/coreclr/wasi/tests/run.cs                  # all built tests
//   ./.dotnet/dotnet src/coreclr/wasi/tests/run.cs -- --tree=JIT/jit64/opt/cse
//   ./.dotnet/dotnet src/coreclr/wasi/tests/run.cs -- --list
//   ./.dotnet/dotnet src/coreclr/wasi/tests/run.cs -- --jobs=1 --timeout=120
//
// Env overrides:
//   CONFIG        - Release (default) | Debug | Checked
//   CORE_ROOT     - explicit path to a CORE_ROOT layout (default: derived)
//   TESTROOT      - explicit path to a test-bin root (default: derived)
//   WASMTIME      - directory containing wasmtime (default: probe $PATH then ~/.wasmtime/bin)

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

string config = Environment.GetEnvironmentVariable("CONFIG") ?? "Release";

// Repo root: walk up from cwd looking for .git or global.json.
string repoRoot = FindRepoRoot();
string testRoot = Environment.GetEnvironmentVariable("TESTROOT")
    ?? Path.Combine(repoRoot, "artifacts", "tests", "coreclr", $"wasi.wasm.{config}");
string coreRoot = Environment.GetEnvironmentVariable("CORE_ROOT")
    ?? Path.Combine(testRoot, "Tests", "Core_Root");

string? tree = null;
int jobs = Math.Max(1, Environment.ProcessorCount / 2);
int perTestTimeoutSec = 300;
bool listOnly = false;
string? knownFailuresPath = null;

for (int i = 0; i < args.Length; i++)
{
    string a = args[i];
    if (a == "--list") { listOnly = true; }
    else if (a.StartsWith("--tree=")) { tree = a["--tree=".Length..]; }
    else if (a == "--tree" && i + 1 < args.Length) { tree = args[++i]; }
    else if (a.StartsWith("--jobs=")) { jobs = Math.Max(1, int.Parse(a["--jobs=".Length..])); }
    else if (a.StartsWith("--timeout=")) { perTestTimeoutSec = int.Parse(a["--timeout=".Length..]); }
    else if (a.StartsWith("--known-failures=")) { knownFailuresPath = a["--known-failures=".Length..]; }
    else if (a is "-h" or "--help")
    {
        Console.Error.WriteLine("Usage: run.cs [--list] [--tree=<rel>] [--jobs=N] [--timeout=SECONDS] [--known-failures=PATH]");
        return 2;
    }
    else
    {
        Console.Error.WriteLine($"Unknown argument: {a}");
        return 2;
    }
}

if (!Directory.Exists(testRoot))
{
    Console.Error.WriteLine($"error: test root '{testRoot}' does not exist.");
    Console.Error.WriteLine("       Build tests first with:");
    Console.Error.WriteLine("       BuildAllTestsAsStandalone=true ./src/tests/build.sh wasi {config} -priority1 -skipnative");
    return 2;
}
if (!File.Exists(Path.Combine(coreRoot, "corerun")))
{
    Console.Error.WriteLine($"error: CORE_ROOT '{coreRoot}' is missing corerun.");
    Console.Error.WriteLine("       Generate the layout with:");
    Console.Error.WriteLine($"       ./src/tests/build.sh -GenerateLayoutOnly wasi {config}");
    return 2;
}

string searchRoot = tree is null ? testRoot : Path.Combine(testRoot, tree.Replace('/', Path.DirectorySeparatorChar));
if (!Directory.Exists(searchRoot))
{
    Console.Error.WriteLine($"error: subtree '{tree}' not found under {testRoot}");
    return 2;
}

// Find all per-test .sh wrappers. The marker for a standalone-runnable test
// is the matching .StandaloneTestRunner sentinel dropped under obj/, but the
// .sh under the test-bin root is the canonical entry point and is enough on
// its own (CLRTest.Execute.Bash.targets only emits .sh for runnable tests).
// We filter out the runner-script files installed under Tests/Core_Root.
string coreRootAbs = Path.GetFullPath(coreRoot);
List<string> tests = Directory.EnumerateFiles(searchRoot, "*.sh", SearchOption.AllDirectories)
    .Where(p => !Path.GetFullPath(p).StartsWith(coreRootAbs, StringComparison.Ordinal))
    .OrderBy(p => p, StringComparer.Ordinal)
    .ToList();

if (tests.Count == 0)
{
    Console.Error.WriteLine($"error: no test wrappers under {searchRoot}.");
    Console.Error.WriteLine("       Did you build with BuildAllTestsAsStandalone=true?");
    return 2;
}

string Rel(string p) => Path.GetRelativePath(testRoot, p);

if (listOnly)
{
    foreach (string t in tests)
    {
        Console.WriteLine(Rel(t));
    }
    return 0;
}

HashSet<string> knownFailures = LoadKnownFailures(knownFailuresPath);

// Ensure wasmtime is on PATH for child bash processes.
string wasmtimePath = ProbeWasmtime();
Console.WriteLine($"wasmtime:    {wasmtimePath}");
Console.WriteLine($"test root:   {testRoot}");
Console.WriteLine($"core root:   {coreRoot}");
Console.WriteLine($"tree filter: {tree ?? "(all)"}");
Console.WriteLine($"tests:       {tests.Count}");
Console.WriteLine($"jobs:        {jobs}");
Console.WriteLine($"timeout:     {perTestTimeoutSec}s");
if (knownFailures.Count > 0)
{
    Console.WriteLine($"known fail:  {knownFailures.Count} entries loaded from {knownFailuresPath}");
}
Console.WriteLine();

ConcurrentBag<TestResult> results = new();
int completed = 0;

await Parallel.ForEachAsync(tests, new ParallelOptions { MaxDegreeOfParallelism = jobs }, async (test, ct) =>
{
    string rel = Rel(test);
    TestResult result = await RunOneAsync(test, rel, coreRoot, wasmtimePath, perTestTimeoutSec, ct);
    bool expectedFailure = knownFailures.Contains(rel);
    int n = Interlocked.Increment(ref completed);
    string mark = (result.Outcome, expectedFailure) switch
    {
        (Outcome.Pass, _) => "PASS",
        (Outcome.Fail, true) => "FAIL*",
        (Outcome.Fail, false) => "FAIL",
        (Outcome.Timeout, true) => "TIME*",
        (Outcome.Timeout, false) => "TIME",
        _ => "????",
    };
    Console.WriteLine($"[{n,4}/{tests.Count}] {mark} {result.ElapsedMs,6}ms  {rel}");
    results.Add(result);
});

Console.WriteLine();
int pass = results.Count(r => r.Outcome == Outcome.Pass);
int failExpected = results.Count(r => r.Outcome == Outcome.Fail && knownFailures.Contains(Rel(r.Path)));
int failUnexpected = results.Count(r => r.Outcome == Outcome.Fail && !knownFailures.Contains(Rel(r.Path)));
int timeoutExpected = results.Count(r => r.Outcome == Outcome.Timeout && knownFailures.Contains(Rel(r.Path)));
int timeoutUnexpected = results.Count(r => r.Outcome == Outcome.Timeout && !knownFailures.Contains(Rel(r.Path)));
int unexpectedPass = results.Count(r => r.Outcome == Outcome.Pass && knownFailures.Contains(Rel(r.Path)));

Console.WriteLine($"=== summary ===");
Console.WriteLine($"  pass:                 {pass}");
Console.WriteLine($"  fail (expected):      {failExpected}");
Console.WriteLine($"  fail (unexpected):    {failUnexpected}");
Console.WriteLine($"  timeout (expected):   {timeoutExpected}");
Console.WriteLine($"  timeout (unexpected): {timeoutUnexpected}");
Console.WriteLine($"  unexpected pass:      {unexpectedPass}");

if (failUnexpected + timeoutUnexpected > 0)
{
    Console.WriteLine();
    Console.WriteLine("unexpected failures:");
    foreach (TestResult r in results
        .Where(r => (r.Outcome is Outcome.Fail or Outcome.Timeout) && !knownFailures.Contains(Rel(r.Path)))
        .OrderBy(r => r.Path, StringComparer.Ordinal))
    {
        Console.WriteLine($"  {Rel(r.Path)}  (outcome={r.Outcome}, exit={r.ExitCode}, {r.ElapsedMs}ms)");
    }
}

if (unexpectedPass > 0)
{
    Console.WriteLine();
    Console.WriteLine("unexpected passes (remove from known-failures.txt):");
    foreach (TestResult r in results
        .Where(r => r.Outcome == Outcome.Pass && knownFailures.Contains(Rel(r.Path)))
        .OrderBy(r => r.Path, StringComparer.Ordinal))
    {
        Console.WriteLine($"  {Rel(r.Path)}");
    }
}

return (failUnexpected + timeoutUnexpected) > 0 ? 1 : 0;

// ---- helpers ----

static string FindRepoRoot()
{
    // Walk up from cwd looking for a `.git` dir or `global.json`.
    string dir = Directory.GetCurrentDirectory();
    while (true)
    {
        if (Directory.Exists(Path.Combine(dir, ".git")) || File.Exists(Path.Combine(dir, "global.json")))
        {
            return dir;
        }
        string? parent = Path.GetDirectoryName(dir);
        if (parent is null || parent == dir)
        {
            throw new InvalidOperationException("Could not find repo root (no .git / global.json above cwd).");
        }
        dir = parent;
    }
}

static string ProbeWasmtime()
{
    foreach (string candidate in new[]
    {
        Environment.GetEnvironmentVariable("WASMTIME"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".wasmtime", "bin", "wasmtime"),
    })
    {
        if (!string.IsNullOrEmpty(candidate) && File.Exists(candidate))
        {
            string dir = Path.GetDirectoryName(candidate)!;
            string path = Environment.GetEnvironmentVariable("PATH") ?? "";
            if (!path.Split(Path.PathSeparator).Contains(dir, StringComparer.Ordinal))
            {
                Environment.SetEnvironmentVariable("PATH", $"{dir}{Path.PathSeparator}{path}");
            }
            return candidate;
        }
    }

    // Look on PATH.
    string? viaPath = (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator)
        .Select(d => Path.Combine(d, "wasmtime"))
        .FirstOrDefault(File.Exists);
    if (viaPath is not null)
    {
        return viaPath;
    }

    throw new FileNotFoundException(
        "wasmtime not found. Install from https://wasmtime.dev/install.sh or set $WASMTIME.");
}

static HashSet<string> LoadKnownFailures(string? path)
{
    HashSet<string> set = new(StringComparer.Ordinal);
    if (string.IsNullOrEmpty(path))
    {
        return set;
    }

    if (!File.Exists(path))
    {
        Console.Error.WriteLine($"warning: known-failures file '{path}' not found; ignoring.");
        return set;
    }

    foreach (string raw in File.ReadAllLines(path))
    {
        string line = raw.Split('#', 2)[0].Trim();
        if (line.Length > 0)
        {
            set.Add(line);
        }
    }

    return set;
}

static async Task<TestResult> RunOneAsync(
    string scriptPath,
    string relPath,
    string coreRoot,
    string wasmtimePath,
    int timeoutSec,
    CancellationToken outerCt)
{
    Stopwatch sw = Stopwatch.StartNew();
    using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(outerCt);
    cts.CancelAfter(TimeSpan.FromSeconds(timeoutSec));

    ProcessStartInfo psi = new()
    {
        FileName = "/bin/bash",
        WorkingDirectory = Path.GetDirectoryName(scriptPath)!,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
    };
    psi.ArgumentList.Add(scriptPath);
    psi.ArgumentList.Add($"-coreroot={coreRoot}");
    // Ensure CORE_ROOT is also set as env (some tests probe it).
    psi.Environment["CORE_ROOT"] = coreRoot;

    using Process p = Process.Start(psi)!;
    StringBuilder stdout = new(), stderr = new();
    Task tStdout = ReadAllAsync(p.StandardOutput, stdout);
    Task tStderr = ReadAllAsync(p.StandardError, stderr);

    try
    {
        await p.WaitForExitAsync(cts.Token);
    }
    catch (OperationCanceledException)
    {
        try { p.Kill(entireProcessTree: true); } catch { }
        await Task.WhenAny(Task.WhenAll(tStdout, tStderr), Task.Delay(2000));
        sw.Stop();
        return new TestResult(scriptPath, Outcome.Timeout, -1, (int)sw.ElapsedMilliseconds, stdout.ToString(), stderr.ToString());
    }

    await Task.WhenAll(tStdout, tStderr);
    sw.Stop();
    Outcome outcome = p.ExitCode == 0 ? Outcome.Pass : Outcome.Fail;
    return new TestResult(scriptPath, outcome, p.ExitCode, (int)sw.ElapsedMilliseconds, stdout.ToString(), stderr.ToString());

    static async Task ReadAllAsync(StreamReader r, StringBuilder sb)
    {
        char[] buf = new char[4096];
        int n;
        while ((n = await r.ReadAsync(buf).ConfigureAwait(false)) > 0)
        {
            sb.Append(buf, 0, n);
        }
    }
}

enum Outcome { Pass, Fail, Timeout }
record TestResult(string Path, Outcome Outcome, int ExitCode, int ElapsedMs, string Stdout, string Stderr);
