// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CommandLine;
using System.IO;
using System.Text;
using Internal.TypeSystem;

namespace SuperPmi.Replay;

internal sealed class Options
{
    public string nameOfJit;
    public string nameOfJit2;
    public string nameOfInputMethodContextFile;
    public string verbosity;
    public string writeLogFile;
    public string reproName;
    public bool breakOnError;
    public bool breakOnAssert;
    public bool breakOnException;
    public bool ignoreStoredConfig;
    public bool applyDiff;
    public bool parallel;
    public string streamFile;
    public bool skipCleanup;
    public int workerCount = -1; // -1 (or 1) means not to use parallel mode.
    public int indexCount = -1;  // With indexCount == -1 and no hash, everything is compiled.
    public int failureLimit = -1;
    public int repeatCount = 1;
    public int[] indexes;
    public string hash;
    public string methodStatsTypes;
    public string details;
    public string mclFilename;
    public string targetArchitecture;
    public string compileList;
    public int offset = -1;
    public int increment = -1;
    public LightWeightMap<uint, uint> forceJitOptions;
    public LightWeightMap<uint, uint> forceJit2Options;
    public LightWeightMap<uint, uint> jitOptions;
    public LightWeightMap<uint, uint> jit2Options;
}

// The SuperPMI command line: switches are matched case-insensitively, in order, mostly by any prefix
// of their name (so "-v" is "-verbosity" and "-p" is "-parallel"), and files are told apart by extension.
internal static class CommandLine
{
    // Parallel mode parses this to detect usage errors in the child processes.
    public const string UsageFirstLine = "SuperPMI is a JIT compiler testing tool.";

    // CoreCLR's MAXIMUM_WAIT_OBJECTS.
    private const int MaximumWaitObjects = 64;

    private const int MM3_HASH_BUFFER_SIZE = 33;

    private static readonly string s_sharedLibSuffix = OperatingSystem.IsWindows() ? ".dll" : OperatingSystem.IsMacOS() ? ".dylib" : ".so";

    private static string DllName(string name) => OperatingSystem.IsWindows() ? name + ".dll" : "lib" + name + s_sharedLibSuffix;

    public static void DumpHelp(string program)
    {
        var help = new StringBuilder();
        void P(string line = "") => help.Append(line).Append('\n');

        P(UsageFirstLine);
        P();
        P($"Usage: {program} [options] [jitname] [jitname2] filename.mc");
        P($" jitname{s_sharedLibSuffix} - optional path of jit to be tested (default is JIT in same directory as {program})");
        P($" jitname2{s_sharedLibSuffix} - optional path of second jit to be tested");
        P(" filename.mc - load method contexts from filename.mc");
        P(" -j[it] Name - optionally -jit can be used to specify jits");
        P(" -l[oad] filename - optionally -load can be used to specify method contexts");
        P();
        P("Options:");
        P();
        P(" -boe");
        P("     Break on error return from compileMethod");
        P();
        P(" -boa");
        P("     Break on assert from the JIT");
        P();
        P(" -box");
        P("     Break on exception thrown, such as for missing data during replay");
        P();
        P(" -ignoreStoredConfig");
        P("     On replay, ignore any stored configuration variables. Useful for Checked/Release asm diffs.");
        P();
        P(" -v[erbosity] messagetypes");
        P("     Controls which types of messages SuperPMI logs. Specify a string of");
        P("     characters representing message categories to enable, where:");
        P("         e - errors (internal fatal errors that are non-recoverable)");
        P("         w - warnings (internal conditions that are unusual, but not serious)");
        P("         m - missing (failures due to missing JIT-EE interface details)");
        P("         i - issues (issues found with the JIT, e.g. asm diffs, asserts)");
        P("         n - information (notifications/summaries, e.g. 'Loaded 5  Jitted 4  FailedCompile 1')");
        P("         v - verbose (status messages, e.g. 'Jit startup took '151.12ms')");
        P("         d - debug (lots of detailed output)");
        P("         a - all (enable all message types; overrides other enable message types)");
        P("         q - quiet (disable all output; overrides all others)");
        P("     e.g. '-v ew' only writes error and warning messages to the console.");
        P("     'q' takes precedence over any other message type specified.");
        P("     Default set of messages enabled is 'ewminv'.");
        P();
        P(" -w[riteLogFile] logfile");
        P("     Write log messages to the specified file.");
        P();
        P(" -c[ompile] <indices>");
        P("     Compile only those method contexts whose indices are specified.");
        P("     Indices can be either a single index, comma separated values,");
        P("     a range, or the name of a .MCL file with newline delimited indices.");
        P("     e.g. -compile 20");
        P("     e.g. -compile 20,25,30,32");
        P("     e.g. -compile 10-99");
        P("     e.g. -compile 5,10-99,101,201-300");
        P("     e.g. -compile failed.mcl");
        P();
        P(" -m[atchHash] <MD5 Hash>");
        P("     Compile only method context with specific MD5 hash");
        P();
        P(" -e[mitMethodStats] <stats-types>");
        P("     Emit method statistics in CSV format to filename.mc.stats.");
        P("     Specify a string of characters representing statistics to emit, where:");
        P("         i - method IL code size");
        P("         a - method compiled ASM code size");
        P("         h - method hash to uniquely identify a method across MCH files");
        P("         n - method number inside the source MCH");
        P("         t - method throughput time");
        P("         * - all available method stats");
        P();
        P(" -details <file name.csv>");
        P("     Emit detailed information about the replay/diff of each context into the specified file");
        P();
        P(" -a[pplyDiff]");
        P("     Compare the compile result generated from the provided JIT with the");
        P("     compile result stored with the MC. If two JITs are provided, this");
        P("     compares the compile results generated by the two JITs.");
        P();
        P(" -r[eproName] prefix");
        P("     Write out failing methods to prefix-n.mc");
        P();
        P(" -f[ailingMCList] mclfilename");
        P("     Write out failing methods to mclfilename.");
        P("     If using -applyDiff and no -diffMCList is specified,");
        P("     comparison failures also get written to mclfilename.");
        P();
        P(" -diffMCList diffMCLfilename");
        P("     Write out methods that differ between compilations to diffMCLfilename.");
        P("     This only works with -applyDiff.");
        P();
        P(" -p[arallel] [workerCount]");
        P("     Run in parallel mode by spawning 'workerCount' processes to do processing.");
        P("     If 'workerCount' is not specified, the number of workers used is");
        P("     the number of processors on the machine.");
        P();
        P(" -streaming filename");
        P("     Streaming mode. Read and execute work requests from indicated file (can be 'stdin').");
        P("     Each line is a method context number and additional force jit options for that method.");
        P("     Blank line or EOF terminates");
        P();
        P(" -failureLimit <limit>");
        P("     For a positive 'limit' number, replay and asm diffs will exit if it sees more than 'limit' failures.");
        P("     Otherwise, all methods will be compiled.");
        P();
        P(" -skipCleanup");
        P("     Skip deletion of temporary files created by child SuperPMI processes with -parallel.");
        P();
        P(" -repeatCount <repetition count>");
        P("     Number of times compilation should repeat for each method context. Usually used when");
        P("     trying to measure JIT throughput for a specific set of methods. Default=1.");
        P();
        P(" -target <target>");
        P("     Specifies the target architecture if doing cross-compilation.");
        P("     Allowed <target> values: x64, x86, arm, arm64, loongarch64, riscv64, wasm");
        P("     Used by the assembly differences calculator; to determine a default JIT dll name;");
        P("     and to avoid treating mismatched cross-compilation replay as failure.");
        P();
        P(" -coredistools");
        P("     Use disassembly tools from the CoreDisTools library");
        P("     Ignored: MSVCDIS is not available, so CoreDisTools will be used.");
        P();
        P(" -jitoption [force] key=value");
        P("     Set the JIT option named \"key\" to \"value\" for JIT 1 if the option was not set.");
        P("     With optional force flag overwrites the existing value if it was already set.");
        P("     NOTE: do not use a \"DOTNET_\" prefix. \"key\" and \"value\" are case sensitive.");
        P("     \"key#value\" is also accepted.");
        P();
        P(" -jit2option [force] key=value");
        P("     Set the JIT option named \"key\" to \"value\" for JIT 2 if the option was not set.");
        P("     With optional force flag overwrites the existing value if it was already set.");
        P("     NOTE: do not use a \"DOTNET_\" prefix. \"key\" and \"value\" are case sensitive.");
        P("     \"key#value\" is also accepted.");
        P();
        P("Inputs are case sensitive.");
        P();
        P("SuperPMI method contexts are stored in files with extension .MC, implying");
        P("a single method context, or .MCH, implying a set of method contexts. Either");
        P("extension works equivalently.");
        P();
        P("Exit codes:");
        P("0  : success");
        P("-1 : general fatal error (e.g., failed to initialize, failed to read files)");
        P("-2 : JIT failed to initialize");
        P("1  : there were compilation failures");
        P("2  : there were assembly diffs");
        P("3  : there were missing values in method context");
        P();
        P("Examples:");
        P($" {program} {DllName("clrjit")} test.mch");
        P($"     ; compile all functions in test.mch using {DllName("clrjit")}");
        P($" {program} -p {DllName("clrjit")} test.mch");
        P("     ; same as above, but use all available processors to compile in parallel");
        P($" {program} -f fail.mcl {DllName("clrjit")} test.mch");
        P("     ; if there are any failures, record their MC numbers in the file fail.mcl");

        Logger.Out.Write(help.ToString());
    }

    // Splits "key=value" (or "key#value").
    public static bool ParseJitOption(string optionString, out string key, out string value)
    {
        int i = optionString.IndexOfAny(['=', '#']);
        if (i < 0 || i >= 1023)
        {
            key = value = null;
            return false;
        }

        key = optionString.Substring(0, i);
        value = optionString.Substring(i + 1);
        return true;
    }

    public static void AddJitOption(LightWeightMap<uint, uint> map, string key, string value)
        => map.Add(map.AddBuffer(NulTerminated(key)), map.AddBuffer(NulTerminated(value)));

    private static byte[] NulTerminated(string s)
    {
        byte[] bytes = new byte[Encoding.UTF8.GetByteCount(s) + 1];
        Encoding.UTF8.GetBytes(s, bytes);
        return bytes;
    }

    private enum Match
    {
        Prefix, // the argument is any prefix of the name
        Full,   // the argument starts with the whole name
        Exact,
    }

    private static readonly (string Name, Match Match)[] s_switches =
    [
        ("help", Match.Prefix), ("HELP", Match.Prefix), ("?", Match.Prefix), ("load", Match.Prefix), ("jit", Match.Prefix),
        ("reproName", Match.Prefix), ("failingMCList", Match.Prefix), ("target", Match.Full), ("boe", Match.Full), ("boa", Match.Full),
        ("box", Match.Full), ("ignoreStoredConfig", Match.Full), ("verbosity", Match.Prefix), ("writeLogFile", Match.Prefix),
        ("emitMethodStats", Match.Prefix), ("details", Match.Prefix), ("applyDiff", Match.Prefix), ("compile", Match.Prefix),
        ("coredistools", Match.Prefix), ("matchHash", Match.Prefix), ("streaming", Match.Prefix), ("parallel", Match.Prefix),
        ("failureLimit", Match.Prefix), ("skipCleanup", Match.Exact), ("repeatCount", Match.Exact), ("stride", Match.Prefix),
        ("jitoption", Match.Prefix), ("jit2option", Match.Prefix),
    ];

    private static string FindSwitch(string arg)
    {
        foreach ((string name, Match match) in s_switches)
        {
            bool matches = match switch
            {
                Match.Prefix => name.StartsWith(arg, StringComparison.OrdinalIgnoreCase),
                Match.Full => arg.StartsWith(name, StringComparison.OrdinalIgnoreCase),
                _ => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase),
            };
            if (matches)
                return name;
        }
        return null;
    }

    // Like the C atoi: leading whitespace, an optional sign, then digits.
    public static int Atoi(string s)
    {
        int i = 0;
        while (i < s.Length && char.IsWhiteSpace(s[i]))
            i++;
        bool negative = i < s.Length && s[i] == '-';
        if (i < s.Length && (s[i] == '-' || s[i] == '+'))
            i++;
        long value = 0;
        for (; i < s.Length && char.IsAsciiDigit(s[i]); i++)
            value = unchecked(value * 10 + (s[i] - '0'));
        return unchecked((int)(negative ? -value : value));
    }

    private static bool AllDigits(string s)
    {
        foreach (char c in s)
        {
            if (!char.IsAsciiDigit(c))
                return false;
        }
        return true;
    }

    public static bool Parse(string[] argv, Options o)
    {
        string program = argv[0];
        bool foundJit = false;

        bool Fail(string error = null)
        {
            if (error != null)
                Logger.LogError(error);
            DumpHelp(program);
            return false;
        }

        if (argv.Length == 1) // Print the help when no arguments are passed.
            return Fail();

        for (int i = 1; i < argv.Length; i++)
        {
            string arg = argv[i];
            bool isASwitch = arg.StartsWith('-') || (OperatingSystem.IsWindows() && arg.StartsWith('/'));

            if (!isASwitch)
            {
                // Process an input file name. Extensions are compared case-insensitively, since this runs on Windows.
                int lastdot = arg.LastIndexOf('.');
                if (lastdot < 0)
                    return Fail();

                string extension = arg.Substring(lastdot);
                if (string.Equals(extension, s_sharedLibSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    if (!SetJit(arg))
                        return false;
                }
                else if (extension.Equals(".mc", StringComparison.OrdinalIgnoreCase) || extension.Equals(".mch", StringComparison.OrdinalIgnoreCase) ||
                         extension.Equals(".mct", StringComparison.OrdinalIgnoreCase))
                {
                    o.nameOfInputMethodContextFile = arg;
                }
                else
                {
                    return Fail($"Unknown file type passed as argument, '{arg}'.");
                }
                continue;
            }

            if (arg.Length == 1)
                return Fail();

            bool NextArg(out string value)
            {
                value = ++i < argv.Length ? argv[i] : null;
                return value != null;
            }

            string value;
            switch (FindSwitch(arg.Substring(1)))
            {
                case "help" or "HELP" or "?":
                    return Fail();

                case "load":
                    if (!NextArg(out value))
                        return Fail();
                    if (value.Length == 0)
                        return Fail($"Arg '{value}' is invalid, name of file missing.");
                    o.nameOfInputMethodContextFile = value;
                    break;

                case "jit":
                    if (!NextArg(out value))
                        return Fail();
                    if (!SetJit(value))
                        return false;
                    break;

                case "reproName":
                    if (!NextArg(out value))
                        return Fail();
                    if (value.Length == 0)
                        return Fail($"Arg '{value}' is invalid, name of prefix missing.");
                    o.reproName = value;
                    break;

                case "failingMCList":
                    if (!NextArg(out o.mclFilename))
                        return Fail();
                    break;

                case "target":
                    if (!NextArg(out o.targetArchitecture))
                        return Fail();
                    break;

                case "boe":
                    o.breakOnError = true;
                    break;

                case "boa":
                    o.breakOnAssert = true;
                    break;

                case "box":
                    o.breakOnException = true;
                    break;

                case "ignoreStoredConfig":
                    o.ignoreStoredConfig = true;
                    break;

                case "verbosity":
                    if (!NextArg(out o.verbosity))
                        return Fail();
                    Logger.SetLogLevel(Logger.ParseLogLevelString(o.verbosity));
                    break;

                case "writeLogFile":
                    if (!NextArg(out o.writeLogFile))
                        return Fail();
                    Logger.OpenLogFile(o.writeLogFile);
                    break;

                case "emitMethodStats":
                    if (!NextArg(out o.methodStatsTypes))
                        return Fail();
                    break;

                case "details":
                    if (!NextArg(out o.details))
                        return Fail();
                    break;

                case "applyDiff":
                    o.applyDiff = true;
                    break;

                case "compile":
                    if (!NextArg(out value))
                        return Fail();
                    if (!MCList.TryParse(value, out o.indexes, out string error))
                    {
                        if (error != null)
                            Logger.LogError(error);
                        return Fail($"Arg '{value}' is invalid, needed method context list.");
                    }
                    o.indexCount = o.indexes.Length;
                    if (o.hash != null)
                        return Fail("Cannot use both method context list and method context hash.");
                    if (o.offset > 0 && o.increment > 0)
                        return Fail("Cannot use method context list in parallel mode.");
                    o.compileList = value; // Saved for -parallel.
                    break;

                case "coredistools":
                    // CoreDisTools is always used, so the switch is allowed and ignored.
                    break;

                case "matchHash":
                    if (!NextArg(out value))
                        return Fail();
                    if (value.Length != MM3_HASH_BUFFER_SIZE - 1)
                        return Fail($"Arg '{value}' is invalid, needed a valid method context hash.");
                    if (o.indexCount > 0)
                        return Fail("Cannot use both method context list and method context hash.");
                    if (o.offset > 0 && o.increment > 0)
                        return Fail("Cannot use method context hash in parallel mode.");
                    o.hash = value;
                    break;

                case "streaming":
                    if (!NextArg(out o.streamFile))
                        return Fail("'-streaming' must be followed by a file name or 'stdin'.");
                    break;

                case "parallel":
                    o.parallel = true;

                    // Is the next argument a worker count?
                    if (i + 1 < argv.Length && AllDigits(argv[i + 1]))
                    {
                        o.workerCount = Atoi(argv[++i]);
                        if (o.workerCount < 1)
                            return Fail("Invalid workers count specified, workers count must be at least 1.");
                        if (o.workerCount > MaximumWaitObjects)
                            return Fail($"Invalid workers count specified, workers count cannot be more than {MaximumWaitObjects}.");
                    }
                    break;

                case "failureLimit":
                    if (!NextArg(out value))
                        return Fail();
                    o.failureLimit = Atoi(value);
                    if (o.failureLimit < 1)
                        return Fail("Incorrect limit specified for -failureLimit. Limit must be > 0.");
                    break;

                case "skipCleanup":
                    o.skipCleanup = true;
                    break;

                case "repeatCount":
                    if (!NextArg(out value))
                        return Fail();
                    o.repeatCount = AllDigits(value) ? Atoi(value) : 0;
                    if (o.repeatCount < 1)
                        return Fail("Invalid repeat count specified. Repeat count must be between 1 and INT_MAX.");
                    break;

                case "stride":
                    // An internal switch used by -parallel: "-stride offset increment" compiles the methods
                    // offset, offset+increment, offset+2*increment, ... until the end of the file.
                    if (!NextArg(out value))
                        return Fail();
                    o.offset = Atoi(value);
                    if (!NextArg(out value))
                        return Fail();
                    o.increment = Atoi(value);
                    if (o.offset < 1 || o.increment < 1)
                        return Fail("Incorrect offset/increment specified for -stride. Offset and increment both must be > 0.");
                    if (o.indexCount > 0)
                        return Fail("Cannot use method context list in parallel mode.");
                    if (o.hash != null)
                        return Fail("Cannot use method context hash in parallel mode.");
                    break;

                case "jitoption":
                    if (!AddJitOption(argv, ref i, ref o.jitOptions, ref o.forceJitOptions))
                        return Fail();
                    break;

                case "jit2option":
                    if (!AddJitOption(argv, ref i, ref o.jit2Options, ref o.forceJit2Options))
                        return Fail();
                    break;

                default:
                    return Fail($"Unknown switch '{arg}' passed as argument.");
            }
        }

        bool SetJit(string name)
        {
            if (name.Length == 0)
                return Fail($"Arg '{name}' is invalid, name of jit missing.");
            if (!foundJit)
            {
                o.nameOfJit = name;
                foundJit = true;
            }
            else
            {
                if (o.nameOfJit2 != null)
                    return Fail("Too many JITs specified.");
                o.nameOfJit2 = name;
            }
            return true;
        }

        // Do some argument validation.
        if (o.targetArchitecture != null && Helpers.TryGetTargetArchitecture(o.targetArchitecture) == null)
            return Fail("Illegal target architecture specified with -target.");

        if (o.streamFile != null)
        {
            if (o.parallel)
            {
                Logger.LogError("streaming mode and parallel mode are incompatible.");
                return false;
            }
            if (o.nameOfJit2 != null)
            {
                Logger.LogError("streaming mode and diff mode are incompatible.");
                return false;
            }
        }

        TargetArchitecture defaultSpmiTargetArchitecture = SpmiUtil.GetSpmiTargetArchitecture();
        if (o.targetArchitecture != null)
            SpmiUtil.SetSpmiTargetArchitecture(Helpers.TryGetTargetArchitecture(o.targetArchitecture).Value);

        if (o.nameOfInputMethodContextFile == null)
            return Fail("Missing name of an input file.");

        if (o.nameOfJit == null)
        {
            o.nameOfJit = GetDefaultJit(program, defaultSpmiTargetArchitecture);
            if (o.nameOfJit == null)
                return Fail("Missing name of a Jit.");
            Logger.LogInfo($"Using default JIT: {o.nameOfJit}");
        }

        if (o.skipCleanup && !o.parallel)
            return Fail("-skipCleanup requires -parallel.");

        return true;
    }

    // "-jitoption [force] key=value"
    private static bool AddJitOption(string[] argv, ref int i, ref LightWeightMap<uint, uint> jitOptions, ref LightWeightMap<uint, uint> forceJitOptions)
    {
        if (++i >= argv.Length)
            return false;

        LightWeightMap<uint, uint> target;
        if ("force".StartsWith(argv[i], StringComparison.OrdinalIgnoreCase))
        {
            target = forceJitOptions ??= new LightWeightMap<uint, uint>();
            i++;
        }
        else
        {
            target = jitOptions ??= new LightWeightMap<uint, uint>();
        }

        if (i >= argv.Length || !ParseJitOption(argv[i], out string key, out string value))
            return false;

        AddJitOption(target, key, value);
        return true;
    }

    // The JIT next to superpmi for the target: clrjit when it's the host, and otherwise a cross-targeting JIT
    // such as clrjit_universal_arm64_x64. SuperPMI doesn't know about the target OS, so the host's is assumed.
    private static string GetDefaultJit(string program, TargetArchitecture defaultTargetArchitecture)
    {
        TargetArchitecture target = SpmiUtil.GetSpmiTargetArchitecture();
        int lastSlash = program.LastIndexOf(Path.DirectorySeparatorChar);
        if (defaultTargetArchitecture == TargetArchitecture.Wasm32 || target == TargetArchitecture.Wasm32 || lastSlash < 0)
            return null;

        string jitName = "clrjit";
        if (defaultTargetArchitecture != target)
        {
            string jitOSName = target switch
            {
                TargetArchitecture.X64 or TargetArchitecture.X86 => OperatingSystem.IsWindows() ? "win" : "unix",
                TargetArchitecture.ARM or TargetArchitecture.ARM64 => "universal",
                _ => "unix",
            };
            // The JIT names have the TargetArchitecture names in lowercase, such as clrjit_universal_arm64_x64.
            jitName = $"clrjit_{jitOSName}_{target.ToString().ToLowerInvariant()}_{defaultTargetArchitecture.ToString().ToLowerInvariant()}";
        }

        return program.Substring(0, lastSlash) + Path.DirectorySeparatorChar + DllName(jitName);
    }
}
