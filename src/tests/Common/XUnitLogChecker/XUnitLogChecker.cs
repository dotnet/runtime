using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

public class XUnitLogChecker
{
    private static class Patterns
    {
        public const string OpenTag = @"(\B<\w+[-]?(\w+)?)|(\B<!\[CDATA\[)";
        public const string CloseTag = @"(\B</\w+[-]?(\w+)?)|(\]\]>)";
    }

    private readonly struct TagResult
    {
        public TagResult(string matchValue, TagCategory matchCategory)
        {
            Value = matchValue;
            Category = matchCategory;
        }

        public string Value { get; init; }
        public TagCategory Category { get; init; }
    }

    private struct LogCheckerConfigParameters
    {
        public LogCheckerConfigParameters()
        {
            ResultsPath = string.Empty;
            TestWrapperName = string.Empty;
            DumpsPath = string.Empty;
        }

        public string ResultsPath { get; set; }
        public string TestWrapperName { get; set; }
        public string DumpsPath { get; set; }

        public bool HasResultsPath() => !string.IsNullOrEmpty(ResultsPath);
        public bool HasTestWrapper() => !string.IsNullOrEmpty(TestWrapperName);
        public bool HasDumpsPath()   => !string.IsNullOrEmpty(DumpsPath);
    }

    private enum TagCategory { OPENING, CLOSING }

    private const int SUCCESS = 0;
    private const int MISSING_ARGS = -1;
    private const int FAILURE = -2;

    private static LogCheckerConfigParameters s_configuration;

    public static int Main(string[] args)
    {
        s_configuration = new LogCheckerConfigParameters();

        // We start with a 'FAILURE' exit code because if something goes wrong
        // later on and the log checker fails, we want to know it.
        int exitCode = FAILURE;

        ParseCommandLineArgs(args);

        // In CoreCLR tests, we record each test's results in an XML file, which
        // is stored in the same directory as said test's script. So, the
        // XUnitLogChecker looks there for said results log to do its job of
        // fixing it if necessary. Afterwards, it checks if there are any dumps
        // to read and display. This is the log checker's full run.
        //
        // In the Libraries tests however, the test logs are stored and created
        // in a different way, currently outside the scope of the XUnitLogChecker,
        // and therefore only the dump checking functionality is required in
        // those cases, so we skip directly to that step.

        if (s_configuration.HasResultsPath() && s_configuration.HasTestWrapper())
        {
            WriteLineTimestamp("The full run will be done.");
            exitCode = DoFullRun();
        }
        else
        {
            WriteLineTimestamp("Only dumps checking will be done.");
            DoDumpsCheck(string.Empty);
            exitCode = SUCCESS;
        }

        return exitCode;
    }

    private static void ParseCommandLineArgs(string[] args)
    {
        // The command-line arguments will always come in pairs of '--flag value'.
        // Hence, we process two elements of the args array at a time.

        for (int i = 0; i < args.Length; i+=2)
        {
            string nextOption = args[i];
            string nextValue = (i+1) < args.Length ? args[i+1] : string.Empty;

            switch (nextOption)
            {
                case "--results-path":
                    s_configuration.ResultsPath = nextValue;
                    break;

                case "--test-wrapper":
                    s_configuration.TestWrapperName = nextValue;
                    break;

                case "--dumps-path":
                    s_configuration.DumpsPath = nextValue;
                    break;

                default:
                    throw new ArgumentException($"Unrecognized option {nextOption}.");
                    break;
            }
        }
    }

    private static int DoFullRun()
    {
        string tempLogName = $"{s_configuration.TestWrapperName}.tempLog.xml";
        string finalLogName = $"{s_configuration.TestWrapperName}.testResults.xml";
        string statsCsvName = $"{s_configuration.TestWrapperName}.testStats.csv";

        string tempLogPath = Path.Combine(s_configuration.ResultsPath, tempLogName);
        string finalLogPath = Path.Combine(s_configuration.ResultsPath, finalLogName);
        string statsCsvPath = Path.Combine(s_configuration.ResultsPath, statsCsvName);

        // If the final results log file is present, then we can assume everything
        // went fine, and it's ready to go without any further processing.

        if (File.Exists(finalLogPath))
        {
            WriteLineTimestamp($"Item '{s_configuration.TestWrapperName}' did"
                              + " complete successfully!");
            return SUCCESS;
        }

        // If there are no logs, then this work item was probably entirely skipped.
        // This can happen under certain specific circumstances, such as with the
        // JIT Hardware Intrinsics tests with DOTNET_GCStress enabled. See Github
        // Issue dotnet/runtime #82143 for more info.
        //
        // The other possibility would be that something went very badly with
        // the work item in question. It will need a developer/engineer to look
        // at it urgently.

        if (!File.Exists(tempLogPath))
        {
            WriteLineTimestamp("No logs were found. This work"
                               + " item was skipped.");

            WriteLineTimestamp($"If this is a mistake, then"
                               + " something went very wrong. The expected temp"
                               + $" log name would be: '{tempLogName}'");
            return SUCCESS;
        }

        // If we're here, then that means we've got something to fix.
        // First, read the stats csv file. If it doesn't exist, then we can
        // assume something went very badly and will likely cause more issues
        // later on, so we exit now.

        if (!File.Exists(statsCsvPath))
        {
            WriteLineTimestamp("An error occurred. No stats csv"
                             + $" was found. The expected name would be '{statsCsvPath}'.");
            return FAILURE;
        }

        // Read the tests run stats csv.
        IEnumerable<string>? workItemStats = TryReadFile(statsCsvPath);

        if (workItemStats is null)
        {
            WriteLineTimestamp("Timed out trying to read the"
                             + $" stats file '{statsCsvPath}'.");
            return FAILURE;
        }

        // The first value at the top of the csv represents the amount of tests
        // that were expected to be run.
        //
        // NOTE: We know for certain the csv only includes numbers. Therefore,
        // we're fine in using Int32.Parse() directly.

        int numExpectedTests = Int32.Parse(workItemStats.First().Split(',').First());

        // The last line of the csv represents the status when the work item
        // finished, successfully or not. It has the following format:
        //     (Tests Run, Tests Passed, Tests Failed, Tests Skipped)

        int[] workItemEndStatus = workItemStats.Last()
                                               .Split(',')
                                               .Select(x => Int32.Parse(x))
                                               .ToArray();

        WriteLineTimestamp($"Item '{s_configuration.TestWrapperName}' did not"
                          + " finish running. Checking and fixing the log...");

        bool success = FixTheXml(tempLogPath);
        if (!success)
        {
            WriteLineTimestamp("Fixing the log failed.");
            return FAILURE;
        }

        PrintWorkItemSummary(numExpectedTests, workItemEndStatus);

        if (s_configuration.HasDumpsPath())
            DoDumpsCheck(tempLogPath);

        // Rename the temp log to the final log, so that Helix can use it without
        // knowing what transpired here.
        File.Move(tempLogPath, finalLogPath);
        WriteLineTimestamp("Finished!");
        return SUCCESS;
    }

    private static void DoDumpsCheck(string testLogPath)
    {
        // If we received a dumps path, then search it accordingly. Otherwise,
        // just skip this function and return.
        //
        // NOTE: In the case of the libraries test, 'testLogPath' will always
        //       be empty.

        if (s_configuration.HasDumpsPath()
            && Directory.Exists(s_configuration.DumpsPath))
        {
            PrintStackTracesFromDumps(testLogPath);
        }
        else
        {
            WriteLineTimestamp($"The provided dumps path '{s_configuration.DumpsPath}'"
                             + " was not found or was not able to be read. Skipping"
                             + " traces search...");
        }
    }

    static void WriteLineTimestamp(string message) =>
        Console.WriteLine($"[XUnitLogChecker]: {System.DateTime.Now:HH:mm:ss.ff}: {message}");

    static IEnumerable<string> TryReadFile(string filePath)
    {
        // Declaring the enumerable to contain the log lines first because we
        // might not be able to read on the first try due to locked resources
        // on Windows. We will retry for up to one minute when this case happens.

        IEnumerable<string>? fileContents = null;
        Stopwatch fileReadStopwatch = Stopwatch.StartNew();

        while (fileReadStopwatch.ElapsedMilliseconds < 60000)
        {
            // We were able to read the file, so we can finish this loop.
            if (fileContents is not null)
                break;

            try
            {
                fileContents = File.ReadLines(filePath);
            }
            catch (IOException ioEx)
            {
                WriteLineTimestamp("Could not read the"
                                 + $" file {filePath}. Retrying...");

                // Give it a couple seconds before trying again.
                Thread.Sleep(2000);
            }
        }
        return fileContents;
    }

    static void PrintWorkItemSummary(int numExpectedTests, int[] workItemEndStatus)
    {
        Console.WriteLine($"\n{workItemEndStatus[0]}/{numExpectedTests} tests run.");
        Console.WriteLine($"* {workItemEndStatus[1]} tests passed.");
        Console.WriteLine($"* {workItemEndStatus[2]} tests failed.");
        Console.WriteLine($"* {workItemEndStatus[3]} tests skipped.\n");
    }

    static bool FixTheXml(string xFile)
    {
        var tags = new Stack<string>();
        string tagText = string.Empty;
        IEnumerable<string>? logLines = TryReadFile(xFile);

        if (logLines is null)
        {
            WriteLineTimestamp("Timed out trying to read the"
                             + $" log file '{xFile}'.");
            return false;
        }

        // Flag to ensure we don't process tag-like-looking things while reading through
        // a test's output.
        bool inOutput = false;
        bool inCData = false;

        foreach (string line in logLines)
        {
            // Get all XML tags found in the current line and sort them in order
            // of appearance.
            Match[] opens = Regex.Matches(line, Patterns.OpenTag).ToArray();
            Match[] closes = Regex.Matches(line, Patterns.CloseTag).ToArray();
            TagResult[] allTags = GetOrderedTagMatches(opens, closes);

            foreach (TagResult tr in allTags)
            {
                // Found an opening tag. Push into the stack and move on to the next one.
                if (tr.Category == TagCategory.OPENING)
                {
                    // Get the name of the next tag. We need solely the text, so we
                    // ask LINQ to lend us a hand in removing the symbols from the string.
                    tagText = new String(tr.Value.Where(c => char.IsLetter(c)).ToArray());

                    // We are beginning to process a test's output. Set the flag to
                    // treat everything as such, until we get the closing output tag.
                    if (tagText.Equals("output") && !inOutput && !inCData)
                    {
                        inOutput = true;
                    }
                    else if (tagText.Equals("CDATA") && !inCData)
                    {
                        inCData = true;
                        tags.Push(tagText);
                        continue;
                    }

                    // CDATA tags store plain output, which can include tag-like
                    // looking strings. So, we skip those until we're done processing
                    // the current CDATA tag.
                    if (!inCData)
                        tags.Push(tagText);
                }

                // Found a closing tag. If we're currently in an output state, then
                // check whether it's the output closing tag. Otherwise, ignore it.
                // This is because in that case, it's just part of the output's text,
                // rather than an actual XML log tag.
                if (tr.Category == TagCategory.CLOSING)
                {
                    // As opposed to the usual XML tags we can find in the logs,
                    // the CDATA closing one doesn't have letters, so we treat it
                    // as a special case.
                    tagText = tr.Value.Equals("]]>")
                              ? "CDATA"
                              : new String(tr.Value
                                           .Where(c => char.IsLetter(c))
                                           .ToArray());

                    if (inCData)
                    {
                        if (tagText.Equals("CDATA") && tagText.Equals(tags.Peek()))
                        {
                            tags.Pop();
                            inCData = false;
                        }
                        continue;
                    }

                    if (inOutput)
                    {
                        if (tagText.Equals("output") && tagText.Equals(tags.Peek()))
                        {
                            tags.Pop();
                            inOutput = false;
                        }
                        continue;
                    }

                    if (tagText.Equals(tags.Peek()))
                        tags.Pop();
                }
            }
        }

        if (tags.Count == 0)
        {
            WriteLineTimestamp($"XUnit log file '{xFile}' was A-OK!");
            return true;
        }

        // Write the missing closings for all the opened tags we found.
        using (StreamWriter xsw = File.AppendText(xFile))
        while (tags.Count > 0)
        {
            string tag = tags.Pop();
            if (tag.Equals("CDATA"))
                xsw.WriteLine("]]>");
            else
                xsw.WriteLine($"</{tag}>");
        }

        WriteLineTimestamp("XUnit log file has been fixed!");
        return true;
    }

    static TagResult[] GetOrderedTagMatches(Match[] openingTags, Match[] closingTags)
    {
        var result = new TagResult[openingTags.Length + closingTags.Length];
        int resIndex = 0;
        int opIndex = 0;
        int clIndex = 0;

        // Fill up the result array with the tags found, in order of appearance
        // in the original log file line.
        //
        // As long as the result array hasn't been filled, then we know for certain
        // that there's at least one unprocessed tag.

        while (resIndex < result.Length)
        {
            if (opIndex < openingTags.Length)
            {
                // We still have pending tags on both lists, opening and closing.
                // So, we add the one that appears first in the given log line.
                if (clIndex < closingTags.Length)
                {
                    if (openingTags[opIndex].Index < closingTags[clIndex].Index)
                    {
                        result[resIndex++] = new TagResult(openingTags[opIndex].Value,
                                                           TagCategory.OPENING);
                        opIndex++;
                    }
                    else
                    {
                        result[resIndex++] = new TagResult(closingTags[clIndex].Value,
                                                           TagCategory.CLOSING);
                        clIndex++;
                    }
                }

                // Only opening tags remaining, so just add them.
                else
                {
                    result[resIndex++] = new TagResult(openingTags[opIndex].Value,
                                                       TagCategory.OPENING);
                    opIndex++;
                }
            }

            // Only closing tags remaining, so just add them.
            else
            {
                result[resIndex++] = new TagResult(closingTags[clIndex].Value,
                                                   TagCategory.CLOSING);
                clIndex++;
            }
        }
        return result;
    }

    static void PrintStackTracesFromDumps(string testLogPath)
    {
        WriteLineTimestamp("Checking for dumps...");
        IEnumerable<string>? dumpsFound = null;

        // In CoreCLR, the test results log has the test's run time recorded.
        // We extract it and use it to only retrieve the dumps that were created
        // after that point in time. Any other dump present there is certainly
        // unrelated to the currently being analyzed test.
        //
        // In Libraries, since we can't get said timestamp, we just retrieve
        // all of the dumps.

        if (!string.IsNullOrEmpty(testLogPath))
        {
            // Read our newly fixed log to retrieve the time and date when the
            // test was run. This is to exclude potentially existing older dumps
            // that are not related to this test run.
            XElement fixedLogTree = XElement.Load(testLogPath);

            // We know from the XUnitWrapperGenerator that the top element
            // is the 'assembly' tag we're looking for.
            var testRunDateTime = DateTime.ParseExact
            (
                fixedLogTree.Attribute("run-date-time").Value,
                "yyyy-MM-dd HH:mm:ss",
                System.Globalization.CultureInfo.InvariantCulture
            );

            dumpsFound = Directory
                         .GetFiles(s_configuration.DumpsPath, "*.dmp")
                         .Where(dmp =>
                                    DateTime.Compare(File.GetCreationTime(dmp),
                                                     testRunDateTime) >= 0);
        }
        else
        {
            dumpsFound = Directory.GetFiles(s_configuration.DumpsPath, "*.dmp");
        }

        if (dumpsFound.Count() == 0)
        {
            WriteLineTimestamp("No crash dumps found. Continuing...");
            return ;
        }

        foreach (string dumpPath in dumpsFound)
        {
            if (OperatingSystem.IsWindows())
            {
                WriteLineTimestamp($"Reading crash dump '{dumpPath}'...");
                WriteLineTimestamp("Stack Trace Found:\n");

                TryPrintStackTraceFromWindowsDmp(dumpPath, Console.Out);
            }
            else
            {
                string crashReportPath = $"{dumpPath}.crashreport.json";

                if (!File.Exists(crashReportPath))
                {
                    WriteLineTimestamp("There was no crash report for the"
                                    + $" dump '{dumpPath}'. Skipping...");
                    continue;
                }

                WriteLineTimestamp($"Reading crash report '{crashReportPath}'...");
                WriteLineTimestamp("Stack Trace Found:\n");

                TryPrintStackTraceFromCrashReport(crashReportPath, Console.Out);
            }
        }
    }

    private const int DEFAULT_TIMEOUT_MS = 1000 * 60 * 10;
    private const string TEST_TARGET_ARCHITECTURE_ENVIRONMENT_VAR = "__TestArchitecture";
    private static readonly List<string> s_knownNativeModules = new List<string>() { "libcoreclr.so", "libclrjit.so" };
    private const string TO_BE_CONTINUE_TAG = "<TO_BE_CONTINUE>";
    private const string SKIP_LINE_TAG = "# <SKIP_LINE>";

    static bool RunProcess(string fileName, string arguments, TextWriter outputWriter)
    {
        Process proc = new Process()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }
        };

        outputWriter.WriteLine($"Invoking: {proc.StartInfo.FileName} {proc.StartInfo.Arguments}");
        proc.Start();

        Task<string> stdOut = proc.StandardOutput.ReadToEndAsync();
        Task<string> stdErr = proc.StandardError.ReadToEndAsync();
        if (!proc.WaitForExit(DEFAULT_TIMEOUT_MS))
        {
            proc.Kill(true);
            outputWriter.WriteLine($"Timedout: '{fileName} {arguments}");
            return false;
        }

        Task.WaitAll(stdOut, stdErr);
        string output = stdOut.Result;
        string error = stdErr.Result;
        if (!string.IsNullOrWhiteSpace(output))
        {
            outputWriter.WriteLine($"stdout: {output}");
        }
        if (!string.IsNullOrWhiteSpace(error))
        {
            outputWriter.WriteLine($"stderr: {error}");
        }
        return true;
    }

    /// <summary>
    ///     Parse crashreport.json file, use llvm-symbolizer to extract symbols
    ///     and recreate the stacktrace that is printed on the console.
    /// </summary>
    /// <param name="crashReportJsonFile">crash dump path</param>
    /// <param name="outputWriter">Stream for writing logs</param>
    /// <returns>true, if we can print the stack trace, otherwise false.</returns>
    static bool TryPrintStackTraceFromCrashReport(string crashReportJsonFile, TextWriter outputWriter)
    {
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            if (!RunProcess("sudo", $"ls -l {crashReportJsonFile}", Console.Out))
            {
                return false;
            }

            Console.WriteLine("=========================================");
            string? userName = Environment.GetEnvironmentVariable("USER");
            if (string.IsNullOrEmpty(userName))
            {
                userName = "helixbot";
            }

            if (!RunProcess("sudo", $"chmod a+rw {crashReportJsonFile}", Console.Out))
            {
                return false;
            }

            if (!RunProcess("sudo", $"chown {userName} {crashReportJsonFile}", Console.Out))
            {
                return false;
            }

            Console.WriteLine("=========================================");
            if (!RunProcess("sudo", $"ls -l {crashReportJsonFile}", Console.Out))
            {
                return false;
            }

            Console.WriteLine("=========================================");
            if (!RunProcess("ls", $"-l {crashReportJsonFile}", Console.Out))
            {
                return false;
            }
        }

        if (!File.Exists(crashReportJsonFile))
        {
            return false;
        }
        outputWriter.WriteLine($"Printing stacktrace from '{crashReportJsonFile}'");

        string contents;
        try
        {
            contents = File.ReadAllText(crashReportJsonFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading {crashReportJsonFile}: {ex.ToString()}");
            return false;
        }
        var crashReport = JsonNode.Parse(contents)!;
        var threads = (JsonArray)crashReport["payload"]!["threads"]!;

        // The logic happens in 3 steps:
        // 1. Read the crashReport.json file, locate all the addresses of interest and then build
        //    a string that will be passed to llvm-symbolizer. It is populated so that each address
        //    is in its separate line along with the file name, etc. Some TAGS are added in the
        //    string that is used in step 2.
        // 2. llvm-symbolizer is ran and above string is passed as input.
        // 3. After llvm-symbolizer completes, TAGS are used to format its output to print it in
        //    the way it will be printed by sos.

        StringBuilder addrBuilder = new StringBuilder();
        string coreRoot = Environment.GetEnvironmentVariable("CORE_ROOT") ?? string.Empty;
        foreach (var thread in threads)
        {

            if (thread!["native_thread_id"] is null)
            {
                continue;
            }

            addrBuilder.AppendLine();
            addrBuilder.AppendLine("----------------------------------");
            addrBuilder.AppendLine($"Thread Id: {thread["native_thread_id"]}");
            addrBuilder.AppendLine("      Child SP               IP Call Site");
            var stack_frames = (JsonArray)thread["stack_frames"]!;
            foreach (var frame in stack_frames)
            {
                addrBuilder.Append($"{SKIP_LINE_TAG} {frame!["stack_pointer"]} {frame["native_address"]} ");
                bool isNative = (string)frame["is_managed"]! == "false";

                if (isNative)
                {
                    var nativeModuleName = (string?)frame["native_module"];
                    var unmanagedName = (string?)frame["unmanaged_name"];

                    if ((nativeModuleName is not null) && (s_knownNativeModules.Contains(nativeModuleName)))
                    {
                        // Need to use llvm-symbolizer (only if module_address != 0)
                        AppendAddress(addrBuilder, coreRoot, nativeModuleName, (string)frame["native_address"]!, (string)frame["module_address"]!);
                    }
                    else if ((nativeModuleName is not null) || (unmanagedName is not null))
                    {
                        if (nativeModuleName is not null)
                        {
                            addrBuilder.Append($"{nativeModuleName}!");
                        }
                        if (unmanagedName is not null)
                        {
                            addrBuilder.Append($"{unmanagedName}");
                        }
                    }
                }
                else
                {
                    var fileName = (string?)frame["filename"];
                    var methodName = (string?)frame["method_name"];

                    if ((fileName is not null) || (methodName is not null))
                    {
                        // found the managed method name
                        if (fileName is not null)
                        {
                            addrBuilder.Append($"{fileName}!");
                        }
                        if (methodName is not null)
                        {
                            addrBuilder.Append($"{methodName}");
                        }
                    }
                    else
                    {
                        addrBuilder.Append($"{frame["native_address"]}");
                    }
                }
                addrBuilder.AppendLine();

            }
        }

        string? symbolizerOutput = null;

        Process llvmSymbolizer = new Process()
        {
            StartInfo = {
                FileName = "llvm-symbolizer",
                Arguments = $"--pretty-print",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
            }
        };

        outputWriter.WriteLine($"Invoking {llvmSymbolizer.StartInfo.FileName} {llvmSymbolizer.StartInfo.Arguments}");

        try
        {
            if (!llvmSymbolizer.Start())
            {
                outputWriter.WriteLine($"Unable to start {llvmSymbolizer.StartInfo.FileName}");
            }

            using (var symbolizerWriter = llvmSymbolizer.StandardInput)
            {
                symbolizerWriter.WriteLine(addrBuilder.ToString());
            }

            Task<string> stdout = llvmSymbolizer.StandardOutput.ReadToEndAsync();
            Task<string> stderr = llvmSymbolizer.StandardError.ReadToEndAsync();
            bool fSuccess = llvmSymbolizer.WaitForExit(DEFAULT_TIMEOUT_MS);

            Task.WaitAll(stdout, stderr);

            if (!fSuccess)
            {
                outputWriter.WriteLine("Errors while running llvm-symbolizer --pretty-print");
                string output = stdout.Result;
                string error = stderr.Result;

                Console.WriteLine("llvm-symbolizer stdout:");
                Console.WriteLine(output);
                Console.WriteLine("llvm-symbolizer stderr:");
                Console.WriteLine(error);

                llvmSymbolizer.Kill(true);

                return false;
            }

            symbolizerOutput = stdout.Result;

        }
        catch (Exception e)
        {
            outputWriter.WriteLine("Errors while running llvm-symbolizer --pretty-print");
            outputWriter.WriteLine(e.ToString());
            return false;
        }

        // Go through the output of llvm-symbolizer and strip all the markers we added initially.
        string[] contentsToSantize = symbolizerOutput.Split(Environment.NewLine);
        StringBuilder finalBuilder = new StringBuilder();
        for (int lineNum = 0; lineNum < contentsToSantize.Length; lineNum++)
        {
            string line = contentsToSantize[lineNum].Replace(SKIP_LINE_TAG, string.Empty);
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.EndsWith(TO_BE_CONTINUE_TAG))
            {
                finalBuilder.Append(line.Replace(TO_BE_CONTINUE_TAG, string.Empty));
                continue;
            }
            finalBuilder.AppendLine(line);
        }
        outputWriter.WriteLine("Stack trace:");
        outputWriter.WriteLine(finalBuilder.ToString());
        return true;
    }

    static void AppendAddress(StringBuilder sb, string coreRoot, string nativeModuleName, string native_address, string module_address)
    {
        if (module_address != "0x0")
        {
            sb.Append($"{nativeModuleName}!");
            sb.Append(TO_BE_CONTINUE_TAG);
            sb.AppendLine();
            //addrBuilder.AppendLine(frame.native_image_offset);
            ulong nativeAddress = ulong.Parse(native_address.Substring(2), System.Globalization.NumberStyles.HexNumber);
            ulong moduleAddress = ulong.Parse(module_address.Substring(2), System.Globalization.NumberStyles.HexNumber);
            string fullPathToModule = Path.Combine(coreRoot, nativeModuleName);
            sb.AppendFormat("{0} 0x{1:x}", fullPathToModule, nativeAddress - moduleAddress);
        }
    }

    static bool TryPrintStackTraceFromWindowsDmp(string dmpFile, TextWriter outputWriter)
    {
        string? targetArchitecture = Environment.GetEnvironmentVariable(TEST_TARGET_ARCHITECTURE_ENVIRONMENT_VAR);
        if (string.IsNullOrEmpty(targetArchitecture))
        {
            outputWriter.WriteLine($"Environment variable {TEST_TARGET_ARCHITECTURE_ENVIRONMENT_VAR} is not set.");
            return false;
        }

        string cdbPath = $@"C:\Program Files (x86)\Windows Kits\10\Debuggers\{targetArchitecture}\cdb.exe";
        if (!File.Exists(cdbPath))
        {
            outputWriter.WriteLine($"Unable to find cdb.exe at {cdbPath}");
            return false;
        }

        string sosPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet", "sos", "sos.dll");

        string corDllArg = string.Empty;
        string? coreRoot = Environment.GetEnvironmentVariable("CORE_ROOT");
        if (coreRoot is not null)
        {
            corDllArg = $".cordll -lp \"{coreRoot}\"";
        }

        var cdbScriptPath = Path.GetTempFileName();
        File.WriteAllText(cdbScriptPath, $$"""
            {{ corDllArg }}
            .load {{sosPath}}
            ~*k
            !clrstack -f -all
            q
            """);

        // cdb outputs the stacks directly, so we don't need to parse the output.
        if (!RunProcess(cdbPath, $@"-c ""$<{cdbScriptPath}"" -z ""{dmpFile}""", outputWriter))
        {
            outputWriter.WriteLine("Unable to run cdb.exe");
            return false;
        }
        return true;
    }
}

