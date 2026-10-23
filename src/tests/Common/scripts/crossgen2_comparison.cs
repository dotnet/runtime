// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The runtime test build disables these outputs, but a file-based app needs them to run via dotnet.
#:property GenerateDependencyFile=true
#:property GenerateRuntimeConfigurationFiles=true

using System.Diagnostics;
using System.Formats.Tar;
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml;

return await CrossgenComparison.RunAsync(args);

internal static partial class CrossgenComparison
{
    private const string NativeOrReadyToRunImage = nameof(NativeOrReadyToRunImage);
    private const string DebuggingFile = nameof(DebuggingFile);

    private static readonly string[] s_frameworkAssemblies = """
        Microsoft.Bcl.AsyncInterfaces.dll
        Microsoft.CSharp.dll
        Microsoft.Extensions.Caching.Abstractions.dll
        Microsoft.Extensions.Caching.Memory.dll
        Microsoft.Extensions.Configuration.Abstractions.dll
        Microsoft.Extensions.Configuration.Binder.dll
        Microsoft.Extensions.Configuration.CommandLine.dll
        Microsoft.Extensions.Configuration.dll
        Microsoft.Extensions.Configuration.EnvironmentVariables.dll
        Microsoft.Extensions.Configuration.FileExtensions.dll
        Microsoft.Extensions.Configuration.Ini.dll
        Microsoft.Extensions.Configuration.Json.dll
        Microsoft.Extensions.Configuration.UserSecrets.dll
        Microsoft.Extensions.Configuration.Xml.dll
        Microsoft.Extensions.DependencyInjection.Abstractions.dll
        Microsoft.Extensions.DependencyInjection.dll
        Microsoft.Extensions.DependencyModel.dll
        Microsoft.Extensions.FileProviders.Abstractions.dll
        Microsoft.Extensions.FileProviders.Composite.dll
        Microsoft.Extensions.FileProviders.Physical.dll
        Microsoft.Extensions.FileSystemGlobbing.dll
        Microsoft.Extensions.Hosting.Abstractions.dll
        Microsoft.Extensions.Hosting.dll
        Microsoft.Extensions.Http.dll
        Microsoft.Extensions.Logging.Abstractions.dll
        Microsoft.Extensions.Logging.Configuration.dll
        Microsoft.Extensions.Logging.Console.dll
        Microsoft.Extensions.Logging.Debug.dll
        Microsoft.Extensions.Logging.dll
        Microsoft.Extensions.Logging.EventLog.dll
        Microsoft.Extensions.Logging.EventSource.dll
        Microsoft.Extensions.Logging.TraceSource.dll
        Microsoft.Extensions.Options.ConfigurationExtensions.dll
        Microsoft.Extensions.Options.DataAnnotations.dll
        Microsoft.Extensions.Options.dll
        Microsoft.Extensions.Primitives.dll
        Microsoft.VisualBasic.Core.dll
        Microsoft.VisualBasic.dll
        Microsoft.Win32.Primitives.dll
        Microsoft.Win32.Registry.AccessControl.dll
        Microsoft.Win32.Registry.dll
        Microsoft.Win32.SystemEvents.dll
        mscorlib.dll
        netstandard.dll
        System.AppContext.dll
        System.Buffers.dll
        System.CodeDom.dll
        System.Collections.Concurrent.dll
        System.Collections.dll
        System.Collections.Immutable.dll
        System.Collections.NonGeneric.dll
        System.Collections.Specialized.dll
        System.ComponentModel.Annotations.dll
        System.ComponentModel.Composition.dll
        System.ComponentModel.Composition.Registration.dll
        System.ComponentModel.DataAnnotations.dll
        System.ComponentModel.dll
        System.ComponentModel.EventBasedAsync.dll
        System.ComponentModel.Primitives.dll
        System.ComponentModel.TypeConverter.dll
        System.Composition.AttributedModel.dll
        System.Composition.Convention.dll
        System.Composition.Hosting.dll
        System.Composition.Runtime.dll
        System.Composition.TypedParts.dll
        System.Configuration.ConfigurationManager.dll
        System.Configuration.dll
        System.Console.dll
        System.Core.dll
        System.Data.Common.dll
        System.Data.DataSetExtensions.dll
        System.Data.dll
        System.Data.Odbc.dll
        System.Data.OleDb.dll
        System.Diagnostics.Contracts.dll
        System.Diagnostics.Debug.dll
        System.Diagnostics.DiagnosticSource.dll
        System.Diagnostics.EventLog.dll
        System.Diagnostics.FileVersionInfo.dll
        System.Diagnostics.PerformanceCounter.dll
        System.Diagnostics.Process.dll
        System.Diagnostics.StackTrace.dll
        System.Diagnostics.TextWriterTraceListener.dll
        System.Diagnostics.Tools.dll
        System.Diagnostics.TraceSource.dll
        System.Diagnostics.Tracing.dll
        System.DirectoryServices.AccountManagement.dll
        System.DirectoryServices.dll
        System.DirectoryServices.Protocols.dll
        System.dll
        System.Drawing.dll
        System.Drawing.Primitives.dll
        System.Dynamic.Runtime.dll
        System.Formats.Asn1.dll
        System.Formats.Cbor.dll
        System.Globalization.Calendars.dll
        System.Globalization.dll
        System.Globalization.Extensions.dll
        System.IO.Compression.Brotli.dll
        System.IO.Compression.dll
        System.IO.Compression.FileSystem.dll
        System.IO.Compression.ZipFile.dll
        System.IO.dll
        System.IO.FileSystem.AccessControl.dll
        System.IO.FileSystem.dll
        System.IO.FileSystem.DriveInfo.dll
        System.IO.FileSystem.Primitives.dll
        System.IO.FileSystem.Watcher.dll
        System.IO.IsolatedStorage.dll
        System.IO.MemoryMappedFiles.dll
        System.IO.Packaging.dll
        System.IO.Pipelines.dll
        System.IO.Pipes.AccessControl.dll
        System.IO.Pipes.dll
        System.IO.Ports.dll
        System.IO.UnmanagedMemoryStream.dll
        System.Linq.dll
        System.Linq.Expressions.dll
        System.Linq.Parallel.dll
        System.Linq.Queryable.dll
        System.Management.dll
        System.Memory.dll
        System.Net.dll
        System.Net.Http.dll
        System.Net.Http.Json.dll
        System.Net.Http.WinHttpHandler.dll
        System.Net.HttpListener.dll
        System.Net.Mail.dll
        System.Net.NameResolution.dll
        System.Net.NetworkInformation.dll
        System.Net.Ping.dll
        System.Net.Primitives.dll
        System.Net.Requests.dll
        System.Net.Security.dll
        System.Net.ServicePoint.dll
        System.Net.Sockets.dll
        System.Net.WebClient.dll
        System.Net.WebHeaderCollection.dll
        System.Net.WebProxy.dll
        System.Net.WebSockets.Client.dll
        System.Net.WebSockets.dll
        System.Numerics.dll
        System.Numerics.Tensors.dll
        System.Numerics.Vectors.dll
        System.ObjectModel.dll
        System.Private.CoreLib.dll
        System.Private.DataContractSerialization.dll
        System.Private.Uri.dll
        System.Private.Xml.dll
        System.Private.Xml.Linq.dll
        System.Reflection.Context.dll
        System.Reflection.DispatchProxy.dll
        System.Reflection.dll
        System.Reflection.Emit.dll
        System.Reflection.Emit.ILGeneration.dll
        System.Reflection.Emit.Lightweight.dll
        System.Reflection.Extensions.dll
        System.Reflection.Metadata.dll
        System.Reflection.MetadataLoadContext.dll
        System.Reflection.Primitives.dll
        System.Reflection.TypeExtensions.dll
        System.Resources.Extensions.dll
        System.Resources.Reader.dll
        System.Resources.ResourceManager.dll
        System.Resources.Writer.dll
        System.Runtime.Caching.dll
        System.Runtime.CompilerServices.Unsafe.dll
        System.Runtime.CompilerServices.VisualC.dll
        System.Runtime.dll
        System.Runtime.Extensions.dll
        System.Runtime.Handles.dll
        System.Runtime.InteropServices.dll
        System.Runtime.InteropServices.RuntimeInformation.dll
        System.Runtime.Intrinsics.dll
        System.Runtime.Loader.dll
        System.Runtime.Numerics.dll
        System.Runtime.Serialization.dll
        System.Runtime.Serialization.Formatters.dll
        System.Runtime.Serialization.Json.dll
        System.Runtime.Serialization.Primitives.dll
        System.Runtime.Serialization.Xml.dll
        System.Security.AccessControl.dll
        System.Security.Claims.dll
        System.Security.Cryptography.Algorithms.dll
        System.Security.Cryptography.Cng.dll
        System.Security.Cryptography.Csp.dll
        System.Security.Cryptography.Encoding.dll
        System.Security.Cryptography.OpenSsl.dll
        System.Security.Cryptography.Pkcs.dll
        System.Security.Cryptography.Primitives.dll
        System.Security.Cryptography.ProtectedData.dll
        System.Security.Cryptography.X509Certificates.dll
        System.Security.Cryptography.Xml.dll
        System.Security.dll
        System.Security.Permissions.dll
        System.Security.Principal.dll
        System.Security.Principal.Windows.dll
        System.Security.SecureString.dll
        System.ServiceModel.Syndication.dll
        System.ServiceModel.Web.dll
        System.ServiceProcess.dll
        System.ServiceProcess.ServiceController.dll
        System.Text.Encoding.CodePages.dll
        System.Text.Encoding.dll
        System.Text.Encoding.Extensions.dll
        System.Text.Encodings.Web.dll
        System.Text.Json.dll
        System.Text.RegularExpressions.dll
        System.Threading.AccessControl.dll
        System.Threading.Channels.dll
        System.Threading.dll
        System.Threading.Overlapped.dll
        System.Threading.Tasks.Dataflow.dll
        System.Threading.Tasks.dll
        System.Threading.Tasks.Extensions.dll
        System.Threading.Tasks.Parallel.dll
        System.Threading.Thread.dll
        System.Threading.ThreadPool.dll
        System.Threading.Timer.dll
        System.Transactions.dll
        System.Transactions.Local.dll
        System.ValueTuple.dll
        System.Web.dll
        System.Web.HttpUtility.dll
        System.Windows.dll
        System.Windows.Extensions.dll
        System.Xml.dll
        System.Xml.Linq.dll
        System.Xml.ReaderWriter.dll
        System.Xml.Serialization.dll
        System.Xml.XDocument.dll
        System.Xml.XmlDocument.dll
        System.Xml.XmlSerializer.dll
        System.Xml.XPath.dll
        System.Xml.XPath.XDocument.dll
        WindowsBase.dll
        """.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help")
        {
            PrintHelp();
            return args.Length == 0 ? 2 : 0;
        }

        string command = args[0];
        if (args.Skip(1).Any(static argument => argument is "-h" or "--help"))
        {
            PrintHelp(command);
            return 0;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            Dictionary<string, string> options = ParseOptions(command, args.AsSpan(1));
            int exitCode = command switch
            {
                "crossgen_corelib" => await CrossgenCorelibAsync(options),
                "crossgen_framework" => await CrossgenFrameworkAsync(options),
                "crossgen_dotnet_sdk" => await CrossgenDotnetSdkAsync(options),
                "compare" => CompareResults(options),
                _ => throw new CommandLineException($"Unknown command '{command}'."),
            };

            return exitCode;
        }
        catch (CommandLineException exception)
        {
            Console.Error.WriteLine(exception.Message);
            PrintHelp(IsKnownCommand(command) ? command : null);
            return 2;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            stopwatch.Stop();
            Console.WriteLine($"Elapsed time: {stopwatch.Elapsed.TotalSeconds.ToString(CultureInfo.InvariantCulture)}");
        }
    }

    private static Dictionary<string, string> ParseOptions(string command, ReadOnlySpan<string> arguments)
    {
        string[] requiredOptions;
        string[] optionalOptions = [];

        switch (command)
        {
            case "crossgen_corelib":
                requiredOptions = ["crossgen", "il_corelib", "result_dir"];
                break;
            case "crossgen_framework":
                requiredOptions = ["crossgen", "target_os", "target_arch", "core_root", "result_dir", "compiler_arch_os"];
                optionalOptions = ["dotnet"];
                break;
            case "crossgen_dotnet_sdk":
                requiredOptions = ["crossgen", "il_corelib", "dotnet_sdk", "result_dir"];
                break;
            case "compare":
                requiredOptions = ["base_dir", "diff_dir", "testresults", "target_arch_os"];
                break;
            default:
                throw new CommandLineException($"Unknown command '{command}'.");
        }

        HashSet<string> allowedOptions = new(requiredOptions, StringComparer.Ordinal);
        allowedOptions.UnionWith(optionalOptions);
        Dictionary<string, string> options = new(StringComparer.Ordinal);

        for (int index = 0; index < arguments.Length; index++)
        {
            string argument = arguments[index];
            if (!argument.StartsWith("--", StringComparison.Ordinal))
            {
                throw new CommandLineException($"Unexpected argument '{argument}'.");
            }

            int equalsIndex = argument.IndexOf('=');
            string optionName;
            string optionValue;
            if (equalsIndex >= 0)
            {
                optionName = argument[2..equalsIndex];
                optionValue = argument[(equalsIndex + 1)..];
            }
            else
            {
                optionName = argument[2..];
                if (++index >= arguments.Length)
                {
                    throw new CommandLineException($"Option '--{optionName}' requires a value.");
                }

                optionValue = arguments[index];
            }

            if (!allowedOptions.Contains(optionName))
            {
                throw new CommandLineException($"Unknown option '--{optionName}' for command '{command}'.");
            }

            if (!options.TryAdd(optionName, optionValue))
            {
                throw new CommandLineException($"Option '--{optionName}' was specified more than once.");
            }
        }

        foreach (string requiredOption in requiredOptions)
        {
            if (!options.ContainsKey(requiredOption))
            {
                throw new CommandLineException($"Required option '--{requiredOption}' was not provided.");
            }
        }

        return options;
    }

    private static bool IsKnownCommand(string command) =>
        command is "crossgen_corelib" or "crossgen_framework" or "crossgen_dotnet_sdk" or "compare";

    private static async Task<int> CrossgenCorelibAsync(Dictionary<string, string> options)
    {
        string ilCorelibFilename = options["il_corelib"];
        if (!File.Exists(ilCorelibFilename))
        {
            Console.WriteLine("IL Corelib path does not exist.");
            return 1;
        }

        string resultDirectory = options["result_dir"];
        Directory.CreateDirectory(resultDirectory);
        string nativeImageDirectory = CreateTemporaryDirectory(resultDirectory);

        try
        {
            string nativeImageFilename = Path.Combine(nativeImageDirectory, Path.GetFileName(ilCorelibFilename));
            CrossgenResult result = await RunCrossgenAsync(
                dotnet: null,
                options["crossgen"],
                ilCorelibFilename,
                nativeImageFilename,
                [Path.GetDirectoryName(ilCorelibFilename) ?? string.Empty],
                targetOS: null,
                targetArchitecture: null,
                compilerArchitectureAndOS: null);

            await SaveCrossgenResultAsync(result, resultDirectory);
            return 0;
        }
        finally
        {
            DeleteDirectory(nativeImageDirectory);
        }
    }

    private static async Task<int> CrossgenFrameworkAsync(Dictionary<string, string> options)
    {
        string resultDirectory = options["result_dir"];
        Directory.CreateDirectory(resultDirectory);
        using SemaphoreSlim semaphore = new(Environment.ProcessorCount);
        int compilationFailed = 0;

        Task[] tasks = s_frameworkAssemblies.Select((assemblyName, index) => CompileAssemblyAsync(assemblyName, index)).ToArray();
        await Task.WhenAll(tasks);

        return compilationFailed;

        async Task CompileAssemblyAsync(string assemblyName, int index)
        {
            await semaphore.WaitAsync();
            try
            {
                string printPrefix = $"[{index + 1}:{s_frameworkAssemblies.Length}]: ";
                Console.WriteLine($"{printPrefix}{options["crossgen"]} {assemblyName}");

                string ilFilename = Path.Combine(options["core_root"], assemblyName);
                string nativeImageFilename = Path.Combine(resultDirectory, AddNativeImageExtension(assemblyName));
                CrossgenResult result = await RunCrossgenAsync(
                    options.GetValueOrDefault("dotnet"),
                    options["crossgen"],
                    ilFilename,
                    nativeImageFilename,
                    [options["core_root"]],
                    options["target_os"],
                    options["target_arch"],
                    options["compiler_arch_os"]);

                if (result.ReturnCode != 0)
                {
                    Interlocked.Exchange(ref compilationFailed, 1);
                    Console.WriteLine(
                        $"{printPrefix}{options["crossgen"]} {assemblyName} return code={result.ReturnCode} " +
                        $"args'{result.Arguments}' stdout '{string.Join(Environment.NewLine, result.StandardOutput)}' " +
                        $"stderr'{string.Join(Environment.NewLine, result.StandardError)}'");
                }

                await SaveCrossgenResultAsync(result, resultDirectory);
            }
            finally
            {
                semaphore.Release();
            }
        }
    }

    private static async Task<int> CrossgenDotnetSdkAsync(Dictionary<string, string> options)
    {
        string resultDirectory = options["result_dir"];
        Directory.CreateDirectory(resultDirectory);
        string extractedSdkDirectory = CreateTemporaryDirectory(resultDirectory);
        string nativeImageDirectory = CreateTemporaryDirectory(resultDirectory);

        try
        {
            ExtractTarArchive(options["dotnet_sdk"], extractedSdkDirectory);

            string ilCorelibFilename = options["il_corelib"];
            string nativeCorelibFilename = Path.Combine(nativeImageDirectory, Path.GetFileName(ilCorelibFilename));
            CrossgenResult corelibResult = await RunCrossgenAsync(
                dotnet: null,
                options["crossgen"],
                ilCorelibFilename,
                nativeCorelibFilename,
                [Path.GetDirectoryName(ilCorelibFilename) ?? string.Empty],
                targetOS: null,
                targetArchitecture: null,
                compilerArchitectureAndOS: null);
            await SaveCrossgenResultAsync(corelibResult, resultDirectory);

            List<string> assemblyDirectories = EnumerateDotnetSdkAssemblyDirectories(extractedSdkDirectory);
            List<string> referenceDirectories = [nativeImageDirectory, .. assemblyDirectories];

            foreach (string assemblyDirectory in assemblyDirectories)
            {
                foreach (string ilFilename in Directory.EnumerateFiles(assemblyDirectory)
                    .Where(static filename => SdkAssemblyRegex().IsMatch(Path.GetFileName(filename)))
                    .Where(static filename => !Path.GetFileName(filename).Equals("System.Private.CoreLib.dll", StringComparison.Ordinal)))
                {
                    string nativeImageFilename = Path.Combine(nativeImageDirectory, AddNativeImageExtension(Path.GetFileName(ilFilename)));
                    CrossgenResult result = await RunCrossgenAsync(
                        dotnet: null,
                        options["crossgen"],
                        ilFilename,
                        nativeImageFilename,
                        referenceDirectories,
                        targetOS: null,
                        targetArchitecture: null,
                        compilerArchitectureAndOS: null);
                    await SaveCrossgenResultAsync(result, resultDirectory);
                }
            }

            return 0;
        }
        finally
        {
            DeleteDirectory(nativeImageDirectory);
            DeleteDirectory(extractedSdkDirectory);
        }
    }

    private static async Task<CrossgenResult> RunCrossgenAsync(
        string? dotnet,
        string crossgenExecutableFilename,
        string ilFilename,
        string nativeImageFilename,
        IReadOnlyCollection<string> platformAssemblyPaths,
        string? targetOS,
        string? targetArchitecture,
        string? compilerArchitectureAndOS)
    {
        List<string> arguments = [];
        string executable;
        if (string.IsNullOrEmpty(dotnet))
        {
            executable = crossgenExecutableFilename;
        }
        else
        {
            executable = dotnet;
            arguments.Add(crossgenExecutableFilename);
        }

        foreach (string platformAssemblyPath in platformAssemblyPaths)
        {
            arguments.Add("-r");
            arguments.Add(Path.Combine(platformAssemblyPath, "*.dll"));
        }

        arguments.Add("-O");
        arguments.Add("--determinism-stress");
        arguments.Add("6");
        arguments.Add("--map");
        arguments.Add("--out");
        arguments.Add(nativeImageFilename);

        if (targetOS is not null)
        {
            arguments.Add("--targetos");
            arguments.Add(targetOS);
        }

        if (targetArchitecture is not null)
        {
            arguments.Add("--targetarch");
            arguments.Add(targetArchitecture);
        }

        arguments.Add(ilFilename);
        ProcessResult processResult = await RunProcessAsync(executable, arguments);

        string? outputFileHash = null;
        long? outputFileSizeInBytes = null;
        if (processResult.ReturnCode == 0)
        {
            outputFileHash = await ComputeFileHashAsync(nativeImageFilename);
            outputFileSizeInBytes = new FileInfo(nativeImageFilename).Length;
        }

        return new CrossgenResult
        {
            CompilerArchitectureAndOS = compilerArchitectureAndOS,
            AssemblyName = Path.GetFileNameWithoutExtension(ilFilename),
            ReturnCode = processResult.ReturnCode,
            StandardOutput = SplitLines(processResult.StandardOutput),
            StandardError = SplitLines(processResult.StandardError),
            OutputFileHash = outputFileHash,
            OutputFileSizeInBytes = outputFileSizeInBytes,
            OutputFileType = NativeOrReadyToRunImage,
            Arguments = processResult.Arguments,
        };
    }

    private static async Task<ProcessResult> RunProcessAsync(string executable, IReadOnlyCollection<string> arguments)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = executable,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = new() { StartInfo = startInfo };
        process.Start();
        process.StandardInput.Close();

        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
        Task<string> standardError = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new ProcessResult(
            process.ExitCode,
            await standardOutput,
            await standardError,
            FormatCommand(executable, arguments));
    }

    private static async Task<string> ComputeFileHashAsync(string filename)
    {
        await using FileStream stream = File.OpenRead(filename);
        byte[] hash = await SHA256.HashDataAsync(stream);
        return Convert.ToHexStringLower(hash);
    }

    private static async Task SaveCrossgenResultAsync(CrossgenResult result, string resultDirectory)
    {
        string jsonFilename = Path.Combine(resultDirectory, $"{result.AssemblyName}-{result.OutputFileType}.json");
        await File.WriteAllTextAsync(jsonFilename, SerializeCrossgenResult(result));
    }

    private static List<CrossgenResult> LoadCrossgenResults(string directory, string outputFileType)
    {
        List<CrossgenResult> results = [];
        foreach (string filename in Directory.EnumerateFiles(directory, "*.json"))
        {
            CrossgenResult result = JsonSerializer.Deserialize(
                File.ReadAllText(filename),
                CrossgenResultJsonContext.Default.CrossgenResult)
                ?? throw new InvalidDataException($"Could not deserialize crossgen result '{filename}'.");

            if (result.OutputFileType == outputFileType)
            {
                results.Add(result);
            }
        }

        return results;
    }

    private static int CompareResults(Dictionary<string, string> options)
    {
        string baseDirectory = options["base_dir"];
        string diffDirectory = options["diff_dir"];
        string targetArchitectureAndOS = options["target_arch_os"];
        bool allResultsEqual = true;
        bool didCompare = false;

        foreach (string outputFileType in new[] { NativeOrReadyToRunImage, DebuggingFile })
        {
            Console.WriteLine(
                $"Comparing crossgen results in \"{baseDirectory}\" and \"{diffDirectory}\" directories " +
                $"for files of type \"{outputFileType}\":");

            List<CrossgenResult> baseResults = LoadCrossgenResults(baseDirectory, outputFileType);
            List<CrossgenResult> diffResults = LoadCrossgenResults(diffDirectory, outputFileType);
            Dictionary<string, CrossgenResult> baseResultsByName = ToResultsByName(baseResults);
            Dictionary<string, CrossgenResult> diffResultsByName = ToResultsByName(diffResults);
            HashSet<string> baseAssemblies = new(baseResultsByName.Keys, StringComparer.Ordinal);
            HashSet<string> diffAssemblies = new(diffResultsByName.Keys, StringComparer.Ordinal);
            string[] bothAssemblies = baseAssemblies.Intersect(diffAssemblies, StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray();
            string[] omittedFromBaseDirectory = diffAssemblies.Except(baseAssemblies, StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray();
            string[] omittedFromDiffDirectory = baseAssemblies.Except(diffAssemblies, StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToArray();

            int omittedResultCount = omittedFromBaseDirectory.Length + omittedFromDiffDirectory.Length;
            if (omittedFromBaseDirectory.Length != 0)
            {
                allResultsEqual = false;
                PrintOmittedAssembliesMessage(omittedFromBaseDirectory, baseDirectory);
            }

            if (omittedFromDiffDirectory.Length != 0)
            {
                allResultsEqual = false;
                PrintOmittedAssembliesMessage(omittedFromDiffDirectory, diffDirectory);
            }

            int mismatchedResultCount = 0;
            foreach (string assemblyName in bothAssemblies)
            {
                didCompare = true;
                if (!CompareAndPrintMessage(
                    baseResultsByName[assemblyName],
                    diffResultsByName[assemblyName],
                    baseDirectory,
                    diffDirectory))
                {
                    allResultsEqual = false;
                    mismatchedResultCount++;
                }
            }

            Console.WriteLine($"Number of omitted results: {omittedResultCount}");
            Console.WriteLine($"Number of mismatched results: {mismatchedResultCount}");
            Console.WriteLine($"Total number of files compared: {bothAssemblies.Length}");

            XmlDocument document = CreateTestResultsDocument(
                targetArchitectureAndOS,
                outputFileType,
                baseDirectory,
                diffDirectory,
                bothAssemblies,
                omittedFromBaseDirectory,
                omittedFromDiffDirectory,
                baseResultsByName,
                diffResultsByName,
                omittedResultCount,
                mismatchedResultCount,
                ref allResultsEqual);

            if (outputFileType == NativeOrReadyToRunImage)
            {
                SaveXmlDocument(document, options["testresults"]);
            }
        }

        return didCompare && allResultsEqual ? 0 : 1;
    }

    private static XmlDocument CreateTestResultsDocument(
        string targetArchitectureAndOS,
        string outputFileType,
        string baseDirectory,
        string diffDirectory,
        IReadOnlyCollection<string> bothAssemblies,
        IReadOnlyCollection<string> omittedFromBaseDirectory,
        IReadOnlyCollection<string> omittedFromDiffDirectory,
        IReadOnlyDictionary<string, CrossgenResult> baseResultsByName,
        IReadOnlyDictionary<string, CrossgenResult> diffResultsByName,
        int omittedResultCount,
        int mismatchedResultCount,
        ref bool allResultsEqual)
    {
        XmlDocument document = new();
        document.AppendChild(document.CreateXmlDeclaration("1.0", null, null));
        XmlElement assembliesElement = document.CreateElement("assemblies");
        document.AppendChild(assembliesElement);

        int passedCount = bothAssemblies.Count - omittedResultCount - mismatchedResultCount;
        string testGroupName = $"crossgen2_comparison_job_targeting_{targetArchitectureAndOS}";
        XmlElement assemblyElement = AppendElement(document, assembliesElement, "assembly");
        SetSummaryAttributes(assemblyElement, testGroupName, bothAssemblies.Count, passedCount, omittedResultCount + mismatchedResultCount);

        XmlElement collectionElement = AppendElement(document, assemblyElement, "collection");
        SetSummaryAttributes(collectionElement, testGroupName, bothAssemblies.Count, passedCount, omittedResultCount + mismatchedResultCount);

        foreach (string assemblyName in omittedFromBaseDirectory)
        {
            CrossgenResult diffResult = diffResultsByName[assemblyName];
            string message = $"Expected nothing, got {SerializeCrossgenResult(diffResult)}";
            XmlElement testElement = AppendTestElement(
                document,
                collectionElement,
                $"CrossgenCompile_{assemblyName}_Target_{targetArchitectureAndOS}_Omitted_vs_{diffResult.CompilerArchitectureAndOS}",
                $"Target_{targetArchitectureAndOS}",
                diffResult.CompilerArchitectureAndOS,
                "Fail");
            AppendFailure(document, testElement, "OmittedFromBase", message, message);
        }

        foreach (string assemblyName in omittedFromDiffDirectory)
        {
            CrossgenResult baseResult = baseResultsByName[assemblyName];
            string message = $"Expected {SerializeCrossgenResult(baseResult)} got nothing";
            XmlElement testElement = AppendTestElement(
                document,
                collectionElement,
                $"CrossgenCompile_{assemblyName}_Target_{targetArchitectureAndOS}_{baseResult.CompilerArchitectureAndOS}_vs__Omitted",
                $"Target_{targetArchitectureAndOS}",
                baseResult.CompilerArchitectureAndOS,
                "Fail");
            AppendFailure(document, testElement, "OmittedFromDiff", message, message);
        }

        foreach (string assemblyName in bothAssemblies)
        {
            CrossgenResult baseResult = baseResultsByName[assemblyName];
            CrossgenResult diffResult = diffResultsByName[assemblyName];
            bool resultsEqual = ResultsEqualForTest(baseResult, diffResult);
            if (!resultsEqual)
            {
                allResultsEqual = false;
            }

            string message =
                $"Expected {SerializeCrossgenResult(baseResult)} got {SerializeCrossgenResult(diffResult)} " +
                "Attached to this helix job will be the binary produced during the helix run and you can find " +
                "the binary to compare to in the artifacts for the pipeline.";
            XmlElement testElement = AppendTestElement(
                document,
                collectionElement,
                $"CrossgenCompile_{assemblyName}_Target_{targetArchitectureAndOS}_{baseResult.CompilerArchitectureAndOS}_vs_{diffResult.CompilerArchitectureAndOS}",
                $"Target_{targetArchitectureAndOS}",
                $"{baseResult.CompilerArchitectureAndOS}_{diffResult.CompilerArchitectureAndOS}",
                resultsEqual ? "Pass" : "Fail");

            if (!resultsEqual)
            {
                string outputMessage = message;
                if (outputFileType == NativeOrReadyToRunImage)
                {
                    List<string> uploadedFilenames = CopyArtifactsToUploadRoot(assemblyName, baseDirectory, diffDirectory);
                    if (uploadedFilenames.Count != 0)
                    {
                        outputMessage +=
                            " The following files were uploaded to this Helix work item and are available for download " +
                            $"from the work item file list: {string.Join(", ", uploadedFilenames)}.";
                    }
                }

                AppendFailure(document, testElement, "MismatchOrReturnCodeFail", message, outputMessage);
            }
        }

        return document;
    }

    private static bool CompareAndPrintMessage(
        CrossgenResult baseResult,
        CrossgenResult diffResult,
        string baseDirectory,
        string diffDirectory)
    {
        if (baseResult.AssemblyName != diffResult.AssemblyName ||
            baseResult.OutputFileType != diffResult.OutputFileType)
        {
            throw new InvalidDataException("Crossgen result keys do not match.");
        }

        bool resultsEqual = true;
        if (baseResult.ReturnCode != diffResult.ReturnCode)
        {
            resultsEqual = false;
            PrintCompareResultMessage(
                $"Return code mismatch for \"{baseResult.AssemblyName}\" assembly for files of type \"{baseResult.OutputFileType}\":",
                baseResult.ReturnCode,
                diffResult.ReturnCode,
                baseDirectory,
                diffDirectory);
        }
        else if (baseResult.ReturnCode == 0)
        {
            if (baseResult.OutputFileHash is null || baseResult.OutputFileSizeInBytes is null ||
                diffResult.OutputFileHash is null || diffResult.OutputFileSizeInBytes is null)
            {
                throw new InvalidDataException("Successful crossgen results must contain an output hash and size.");
            }

            if (baseResult.OutputFileHash != diffResult.OutputFileHash)
            {
                resultsEqual = false;
                PrintCompareResultMessage(
                    $"File hash sum mismatch for \"{baseResult.AssemblyName}\" assembly for files of type \"{baseResult.OutputFileType}\":",
                    baseResult.OutputFileHash,
                    diffResult.OutputFileHash,
                    baseDirectory,
                    diffDirectory);
            }

            if (baseResult.OutputFileSizeInBytes != diffResult.OutputFileSizeInBytes)
            {
                resultsEqual = false;
                PrintCompareResultMessage(
                    $"File size mismatch for \"{baseResult.AssemblyName}\" assembly for files of type \"{baseResult.OutputFileType}\":",
                    baseResult.OutputFileSizeInBytes,
                    diffResult.OutputFileSizeInBytes,
                    baseDirectory,
                    diffDirectory);
            }
        }

        return resultsEqual;
    }

    private static bool ResultsEqualForTest(CrossgenResult baseResult, CrossgenResult diffResult) =>
        baseResult.ReturnCode == 0 &&
        diffResult.ReturnCode == 0 &&
        baseResult.OutputFileHash == diffResult.OutputFileHash &&
        baseResult.OutputFileSizeInBytes == diffResult.OutputFileSizeInBytes;

    private static void PrintCompareResultMessage(
        string messageHeader,
        object? baseValue,
        object? diffValue,
        string baseDirectory,
        string diffDirectory)
    {
        Console.WriteLine(messageHeader);
        Console.WriteLine($" - \"{baseDirectory}\" has \"{baseValue}\"");
        Console.WriteLine($" - \"{diffDirectory}\" has \"{diffValue}\"");
    }

    private static void PrintOmittedAssembliesMessage(IEnumerable<string> omittedAssemblies, string directory)
    {
        Console.WriteLine($"The information for the following assemblies was omitted from \"{directory}\" directory:");
        foreach (string assemblyName in omittedAssemblies)
        {
            Console.WriteLine($" - {assemblyName}");
        }
    }

    private static List<string> CopyArtifactsToUploadRoot(string assemblyName, string baseDirectory, string diffDirectory)
    {
        string? uploadRoot = Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT");
        if (string.IsNullOrEmpty(uploadRoot))
        {
            return [];
        }

        if (assemblyName.Contains('/') || assemblyName.Contains('\\'))
        {
            Console.WriteLine($"Unexpected assembly name \"{assemblyName}\"; skipping artifact copy");
            return [];
        }

        string nativeImageFilename = AddNativeImageExtension($"{assemblyName}.dll");
        try
        {
            Directory.CreateDirectory(uploadRoot);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine($"Failed to create upload root \"{uploadRoot}\": {exception.Message}");
            return [];
        }

        List<string> uploadedFilenames = [];
        foreach ((string label, string sourceDirectory) in new[] { ("base", baseDirectory), ("diff", diffDirectory) })
        {
            string sourceFilename = Path.Combine(sourceDirectory, nativeImageFilename);
            if (!File.Exists(sourceFilename))
            {
                Console.WriteLine($"Crossgen output \"{sourceFilename}\" was not found; cannot copy it to the upload root");
                continue;
            }

            string destinationBasename = $"{label}_{nativeImageFilename}";
            string destinationFilename = Path.Combine(uploadRoot, destinationBasename);
            try
            {
                File.Copy(sourceFilename, destinationFilename, overwrite: true);
                File.SetLastWriteTimeUtc(destinationFilename, File.GetLastWriteTimeUtc(sourceFilename));
                Console.WriteLine($"Copied crossgen output \"{sourceFilename}\" to upload file \"{destinationFilename}\"");
                uploadedFilenames.Add(destinationBasename);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Console.WriteLine(
                    $"Failed to copy crossgen output \"{sourceFilename}\" to upload file \"{destinationFilename}\": {exception.Message}");
            }
        }

        return uploadedFilenames;
    }

    private static List<string> EnumerateDotnetSdkAssemblyDirectories(string dotnetSdkDirectory)
    {
        List<string> directories = [];
        foreach (string directory in Directory.EnumerateDirectories(dotnetSdkDirectory, "*", SearchOption.AllDirectories))
        {
            string? parentName = Directory.GetParent(directory)?.Name;
            if (parentName is "Microsoft.NETCore.App" or "Microsoft.AspNetCore.App" or "Microsoft.AspNetCore.All")
            {
                directories.Add(directory);
            }
        }

        return directories;
    }

    private static void ExtractTarArchive(string archiveFilename, string destinationDirectory)
    {
        using FileStream archiveStream = File.OpenRead(archiveFilename);
        if (archiveFilename.EndsWith(".gz", StringComparison.OrdinalIgnoreCase) ||
            archiveFilename.EndsWith(".tgz", StringComparison.OrdinalIgnoreCase))
        {
            using GZipStream gzipStream = new(archiveStream, CompressionMode.Decompress);
            TarFile.ExtractToDirectory(gzipStream, destinationDirectory, overwriteFiles: false);
        }
        else
        {
            TarFile.ExtractToDirectory(archiveStream, destinationDirectory, overwriteFiles: false);
        }
    }

    private static Dictionary<string, CrossgenResult> ToResultsByName(IEnumerable<CrossgenResult> results)
    {
        Dictionary<string, CrossgenResult> resultsByName = new(StringComparer.Ordinal);
        foreach (CrossgenResult result in results)
        {
            resultsByName[result.AssemblyName] = result;
        }

        return resultsByName;
    }

    private static List<string> SplitLines(string value)
    {
        List<string> lines = [];
        using StringReader reader = new(value);
        while (reader.ReadLine() is { } line)
        {
            lines.Add(line);
        }

        return lines;
    }

    private static string AddNativeImageExtension(string filename)
    {
        string? directory = Path.GetDirectoryName(filename);
        string nativeImageFilename = $"{Path.GetFileNameWithoutExtension(filename)}.ni{Path.GetExtension(filename)}";
        return string.IsNullOrEmpty(directory) ? nativeImageFilename : Path.Combine(directory, nativeImageFilename);
    }

    private static string CreateTemporaryDirectory(string parentDirectory)
    {
        string directory = Path.Combine(parentDirectory, $".crossgen2-comparison-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void DeleteDirectory(string directory)
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine($"Failed to remove temporary directory \"{directory}\": {exception.Message}");
        }
    }

    private static string SerializeCrossgenResult(CrossgenResult result) =>
        JsonSerializer.Serialize(result, CrossgenResultJsonContext.Default.CrossgenResult);

    private static string FormatCommand(string executable, IEnumerable<string> arguments) =>
        string.Join(" ", new[] { executable }.Concat(arguments).Select(QuoteArgument));

    private static string QuoteArgument(string argument) =>
        argument.Any(char.IsWhiteSpace) || argument.Contains('"')
            ? $"\"{argument.Replace("\"", "\\\"", StringComparison.Ordinal)}\""
            : argument;

    private static XmlElement AppendElement(XmlDocument document, XmlElement parent, string name)
    {
        XmlElement element = document.CreateElement(name);
        parent.AppendChild(element);
        return element;
    }

    private static XmlElement AppendTestElement(
        XmlDocument document,
        XmlElement collection,
        string name,
        string type,
        string? method,
        string result)
    {
        XmlElement test = AppendElement(document, collection, "test");
        test.SetAttribute("name", name);
        test.SetAttribute("type", type);
        test.SetAttribute("method", method ?? string.Empty);
        test.SetAttribute("time", "0");
        test.SetAttribute("result", result);
        return test;
    }

    private static void AppendFailure(
        XmlDocument document,
        XmlElement test,
        string exceptionType,
        string message,
        string output)
    {
        XmlElement failure = AppendElement(document, test, "failure");
        failure.SetAttribute("exception-type", exceptionType);

        XmlElement messageElement = AppendElement(document, failure, "message");
        messageElement.AppendChild(document.CreateTextNode(message));

        XmlElement outputElement = AppendElement(document, failure, "output");
        outputElement.AppendChild(document.CreateTextNode(output));
    }

    private static void SetSummaryAttributes(XmlElement element, string name, int total, int passed, int failed)
    {
        element.SetAttribute("name", name);
        element.SetAttribute("total", total.ToString(CultureInfo.InvariantCulture));
        element.SetAttribute("passed", passed.ToString(CultureInfo.InvariantCulture));
        element.SetAttribute("failed", failed.ToString(CultureInfo.InvariantCulture));
        element.SetAttribute("skipped", "0");
    }

    private static void SaveXmlDocument(XmlDocument document, string filename)
    {
        XmlWriterSettings settings = new()
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
            IndentChars = "\t",
        };
        using XmlWriter writer = XmlWriter.Create(filename, settings);
        document.Save(writer);
    }

    private static void PrintHelp(string? command = null)
    {
        if (command is null)
        {
            Console.WriteLine(
                """
                Runs crossgen on assemblies and compares the collected results.

                Usage:
                  dotnet crossgen2_comparison.cs -- <command> [options]

                Commands:
                  crossgen_corelib
                  crossgen_framework
                  crossgen_dotnet_sdk
                  compare

                Run a command with --help for its options.
                """);
            return;
        }

        string usage = command switch
        {
            "crossgen_corelib" =>
                "dotnet crossgen2_comparison.cs -- crossgen_corelib --crossgen <path> --il_corelib <path> --result_dir <path>",
            "crossgen_framework" =>
                "dotnet crossgen2_comparison.cs -- crossgen_framework [--dotnet <path>] --crossgen <path> " +
                "--target_os <os> --target_arch <arch> --core_root <path> --result_dir <path> --compiler_arch_os <value>",
            "crossgen_dotnet_sdk" =>
                "dotnet crossgen2_comparison.cs -- crossgen_dotnet_sdk --crossgen <path> --il_corelib <path> " +
                "--dotnet_sdk <path> --result_dir <path>",
            "compare" =>
                "dotnet crossgen2_comparison.cs -- compare --base_dir <path> --diff_dir <path> " +
                "--testresults <path> --target_arch_os <value>",
            _ => throw new CommandLineException($"Unknown command '{command}'."),
        };

        Console.WriteLine($"Usage: {usage}");
    }

    [GeneratedRegex(@"^(Microsoft|System)\..*dll$", RegexOptions.CultureInvariant)]
    private static partial Regex SdkAssemblyRegex();

    private sealed class CommandLineException(string message) : Exception(message);

    private sealed record ProcessResult(int ReturnCode, string StandardOutput, string StandardError, string Arguments);

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(CrossgenResult))]
    private sealed partial class CrossgenResultJsonContext : JsonSerializerContext;

    private sealed class CrossgenResult
    {
        [JsonPropertyName("CompilerArchOS")]
        [JsonPropertyOrder(0)]
        public string? CompilerArchitectureAndOS { get; init; }

        [JsonPropertyName("AssemblyName")]
        [JsonPropertyOrder(1)]
        public string AssemblyName { get; init; } = string.Empty;

        [JsonPropertyName("ReturnCode")]
        [JsonPropertyOrder(2)]
        public int ReturnCode { get; init; }

        [JsonPropertyName("StdOut")]
        [JsonPropertyOrder(3)]
        public List<string> StandardOutput { get; init; } = [];

        [JsonPropertyName("StdErr")]
        [JsonPropertyOrder(4)]
        public List<string> StandardError { get; init; } = [];

        [JsonPropertyName("OutputFileHash")]
        [JsonPropertyOrder(5)]
        public string? OutputFileHash { get; init; }

        [JsonPropertyName("OutputFileSizeInBytes")]
        [JsonPropertyOrder(6)]
        public long? OutputFileSizeInBytes { get; init; }

        [JsonPropertyName("OutputFileType")]
        [JsonPropertyOrder(7)]
        public string OutputFileType { get; init; } = string.Empty;

        [JsonIgnore]
        public string Arguments { get; init; } = string.Empty;
    }
}
