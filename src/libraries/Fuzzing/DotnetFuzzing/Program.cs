// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SharpFuzz;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace DotnetFuzzing;

public static class Program
{
    public static async Task Main(string[] args)
    {
        IFuzzer[] fuzzers = typeof(Program).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Contains(typeof(IFuzzer)))
            .Select(t => (IFuzzer)Activator.CreateInstance(t)!)
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        void PrintUsage()
        {
            Console.Error.WriteLine(
                $"""
                Usage:
                    DotnetFuzzing <Fuzzer name> [input file/directory]
                    DotnetFuzzing prepare-onefuzz <output directory>

                Fuzzers available: {string.Join(", ", fuzzers.Select(t => t.Name))}
                """);
        }

        if (args.Length == 0)
        {
            PrintUsage();
            return;
        }

        if (args[0].Equals("prepare-onefuzz", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length != 2)
            {
                PrintUsage();
                return;
            }

            string publishDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location) ?? Environment.CurrentDirectory;

            await PrepareOneFuzzDeploymentAsync(fuzzers, publishDirectory, args[1]);
            return;
        }

        IFuzzer? fuzzer = fuzzers
            .Where(f => f.Name.Equals(args[0], StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

        if (fuzzer is null)
        {
            Console.Error.WriteLine($"Fuzzer '{args[0]}' not found. Available: {string.Join(", ", fuzzers.Select(t => t.Name))}");
            return;
        }

        RunFuzzer(fuzzer, inputFiles: args.Length > 1 ? args[1] : null);
    }

    private static unsafe void RunFuzzer(IFuzzer fuzzer, string? inputFiles)
    {
        if (!string.IsNullOrEmpty(inputFiles))
        {
            string[] files = Directory.Exists(inputFiles)
                ? Directory.GetFiles(inputFiles)
                : [inputFiles];

            foreach (string inputFile in files)
            {
                fuzzer.FuzzTarget(File.ReadAllBytes(inputFile));
            }

            return;
        }

        Fuzzer.LibFuzzer.Run(bytes =>
        {
            // Some fuzzers assume that the input is at least 2-byte aligned.
            ArgumentOutOfRangeException.ThrowIfNotEqual((nuint)Unsafe.AsPointer(ref MemoryMarshal.GetReference(bytes)) % 8, 0U);

            fuzzer.FuzzTarget(bytes);
        });
    }

    private static async Task PrepareOneFuzzDeploymentAsync(IFuzzer[] fuzzers, string publishDirectory, string outputDirectory)
    {
        string[] dictionaries = Directory.GetFiles(Path.Combine(publishDirectory, "Dictionaries"))
            .Select(Path.GetFileName)
            .ToArray()!;

        if (dictionaries.FirstOrDefault(dict => !fuzzers.Any(f => f.Dictionary == dict)) is { } unusedDictionary)
        {
            throw new Exception($"Dictionary '{unusedDictionary}' is not referenced by any fuzzer.");
        }

        Directory.CreateDirectory(outputDirectory);

        await DownloadArtifactAsync(
            Path.Combine(publishDirectory, "libfuzzer-dotnet.exe"),
            "https://github.com/Metalnem/libfuzzer-dotnet/releases/download/v2023.06.26.1359/libfuzzer-dotnet-windows.exe",
            "cbc1f510caaec01b17b5e89fc780f426710acee7429151634bbf4d0c57583458");

        foreach (IFuzzer fuzzer in fuzzers)
        {
            Console.WriteLine();
            Console.WriteLine($"Preparing {fuzzer.Name} ...");

            string fuzzerDirectory = Path.Combine(outputDirectory, fuzzer.Name);
            Directory.CreateDirectory(fuzzerDirectory);

            Console.WriteLine($"Copying artifacts to {fuzzerDirectory}");
            // NOTE: The expected fuzzer directory structure is currently flat.
            // If we ever need to support subdirectories, OneFuzzConfig.json must also be updated to use PreservePathsJobDependencies.
            foreach (string file in Directory.GetFiles(publishDirectory))
            {
                File.Copy(file, Path.Combine(fuzzerDirectory, Path.GetFileName(file)), overwrite: true);
            }

            if (fuzzer.Dictionary is string dict)
            {
                if (!dictionaries.Contains(dict, StringComparer.Ordinal))
                {
                    throw new Exception($"Fuzzer '{fuzzer.Name}' is referencing a dictionary '{fuzzer.Dictionary}' that does not exist in the publish directory.");
                }

                File.Copy(Path.Combine(publishDirectory, "Dictionaries", dict), Path.Combine(fuzzerDirectory, "dictionary"), overwrite: true);
            }

            InstrumentAssemblies(fuzzer, fuzzerDirectory);

            Console.WriteLine("Generating OneFuzzConfig.json");
            File.WriteAllText(Path.Combine(fuzzerDirectory, "OneFuzzConfig.json"), GenerateOneFuzzConfigJson(fuzzer));

            Console.WriteLine("Generating local-run.bat");
            File.WriteAllText(Path.Combine(fuzzerDirectory, "local-run.bat"), GenerateLocalRunHelperScript(fuzzer));
        }

        WorkaroundOneFuzzTaskNotAcceptingMultipleJobs(fuzzers);
    }

    private static IEnumerable<(string Assembly, string? Prefixes)> GetInstrumentationTargets(IFuzzer fuzzer)
    {
        bool instrumentCoreLib = fuzzer.TargetCoreLibPrefixes.Length > 0;

        if (!instrumentCoreLib && fuzzer.TargetAssemblies.Length == 0)
        {
            throw new Exception($"Specify at least one target in {nameof(IFuzzer.TargetAssemblies)} or {nameof(IFuzzer.TargetCoreLibPrefixes)}.");
        }

        foreach (string assembly in fuzzer.TargetAssemblies)
        {
            string path = assembly;
            if (!path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                path += ".dll";
            }

            if (path == "System.Private.CoreLib.dll")
            {
                if (!instrumentCoreLib)
                {
                    throw new Exception($"To instrument System.Private.CoreLib, specify {nameof(IFuzzer.TargetCoreLibPrefixes)}.");
                }

                continue;
            }

            yield return (path, null);
        }

        if (instrumentCoreLib)
        {
            yield return ("System.Private.CoreLib.dll", string.Join(' ', fuzzer.TargetCoreLibPrefixes));
        }
    }

    private static void InstrumentAssemblies(IFuzzer fuzzer, string fuzzerDirectory)
    {
        foreach (var (assembly, prefixes) in GetInstrumentationTargets(fuzzer))
        {
            Console.WriteLine($"Instrumenting {assembly} {(prefixes is null ? "" : $"({prefixes})")}");

            string path = Path.Combine(fuzzerDirectory, assembly);
            if (!File.Exists(path))
            {
                throw new Exception($"Assembly {path} not found. Make sure to run the tool from the publish directory.");
            }

            byte[] current = File.ReadAllBytes(path);
            string previousOriginal = $"{path}.original";
            string previousInstrumented = $"{path}.instrumented";

            if (!string.IsNullOrEmpty(prefixes))
            {
                // Don't use the cached assembly if the prefixes have changed.
                previousInstrumented += Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(prefixes)));
            }

            if (File.Exists(previousOriginal) &&
                File.Exists(previousInstrumented) &&
                File.ReadAllBytes(previousOriginal).AsSpan().SequenceEqual(current))
            {
                // The assembly hasn't changed since the previous invocation of SharpFuzz.
                File.Copy(previousInstrumented, path, overwrite: true);
                continue;
            }

            File.Delete(previousOriginal);
            File.Delete(previousInstrumented);

            using Process sharpfuzz = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sharpfuzz",
                    Arguments = $"{path} {prefixes}",
                    UseShellExecute = false,
                }
            };

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // https://github.com/Metalnem/sharpfuzz/blob/9e44048d8821da942d00c2c125bb59d039d55673/src/SharpFuzz/Options.cs#L37-L41
                throw new Exception("SHARPFUZZ_INSTRUMENT_MIXED_MODE_ASSEMBLIES is only supported on Windows.");
            }

            sharpfuzz.StartInfo.EnvironmentVariables.Add("SHARPFUZZ_INSTRUMENT_MIXED_MODE_ASSEMBLIES", "1");

            sharpfuzz.Start();
            sharpfuzz.WaitForExit();

            if (sharpfuzz.ExitCode != 0)
            {
                throw new Exception($"Failed to instrument {path}");
            }

            File.WriteAllBytes(previousOriginal, current);
            File.Copy(path, previousInstrumented);
        }
    }

    private static async Task DownloadArtifactAsync(string path, string url, string hash)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine($"Downloading {Path.GetFileName(path)}");

            using var client = new HttpClient();
            byte[] bytes = await client.GetByteArrayAsync(url);

            if (!Convert.ToHexString(SHA256.HashData(bytes)).Equals(hash, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"{path} checksum mismatch");
            }

            File.WriteAllBytes(path, bytes);
        }
    }

    private static string GenerateOneFuzzConfigJson(IFuzzer fuzzer)
    {
        // {setup_dir} is replaced by OneFuzz with the path to the fuzzer directory.
        string? dictionaryArgument = fuzzer.Dictionary is not null
            ? "\"-dict={setup_dir}/dictionary\""
            : null;

        // Make it easier to distinguish between long-running CI jobs and short-lived test submissions.
        string nameSuffix = Environment.GetEnvironmentVariable("TF_BUILD") is null ? "-local" : "";

        return
            $$"""
            {
              "ConfigVersion": 3,
              "Entries": [
                {
                  "JobNotificationEmail": "dotnet-fuzz-updates@microsoft.com",
                  "Skip": false,
                  "Fuzzer": {
                    "$type": "libfuzzer",
                    "FuzzingHarnessExecutableName": "libfuzzer-dotnet.exe",
                    "FuzzingTargetBinaries": [
                      {{string.Join(", ", GetInstrumentationTargets(fuzzer).Select(t => $"\"{t.Assembly}\""))}}
                    ],
                    "CheckFuzzerHelp": false
                  },
                  "FuzzerTimeoutInSeconds": 60,
                  "OneFuzzJobs": [
                    {
                      "ProjectName": "DotnetFuzzing",
                      "TargetName": "{{fuzzer.Name}}{{nameSuffix}}",
                      "TargetOptions": [
                        "--target_path=DotnetFuzzing.exe",
                        "--target_arg={{fuzzer.Name}}"
                      ],
                      "FuzzingTargetOptions": [
                        {{dictionaryArgument}}
                      ]
                    }
                  ],
                  "JobDependencies": [
                    ".\\*"
                  ],
                  "AdoTemplate": {
                    "Org": "dnceng",
                    "Project": "internal",
                    "AssignedTo": "mizupan@microsoft.com",
                    "AreaPath": "internal\\.NET Libraries",
                    "IterationPath": "internal"
                  }
                }
              ]
            }
            """;
    }

    private static string GenerateLocalRunHelperScript(IFuzzer fuzzer)
    {
        string script = $"%~dp0/libfuzzer-dotnet.exe --target_path=%~dp0/DotnetFuzzing.exe --target_arg={fuzzer.Name}";

        if (fuzzer.Dictionary is not null)
        {
            script += " -dict=%~dp0dictionary";
        }

        // Pass any additional arguments to the fuzzer.
        script += " %*";

        return script;
    }

    private static void WorkaroundOneFuzzTaskNotAcceptingMultipleJobs(IFuzzer[] fuzzers)
    {
        string yamlPath = Environment.CurrentDirectory;
        while (!File.Exists(Path.Combine(yamlPath, "DotnetFuzzing.sln")))
        {
            yamlPath = Path.GetDirectoryName(yamlPath) ?? throw new Exception("Couldn't find DotnetFuzzing.sln");
        }

        yamlPath = Path.Combine(yamlPath, "../../../eng/pipelines/libraries/fuzzing/deploy-to-onefuzz.yml");

        string yaml = File.ReadAllText(yamlPath);

        // At the moment OneFuzz can't handle a single deployment where multiple jobs share similar assemblies/pdbs.
        // Generate a separate step for each fuzzer instead as a workaround.
        string tasks = string.Join("\n\n", fuzzers.Select(fuzzer =>
        {
            return
                $$"""
                        - task: onefuzz-task@0
                          inputs:
                            onefuzzOSes: 'Windows'
                          env:
                            onefuzzDropDirectory: $(fuzzerProject)/deployment/{{fuzzer.Name}}
                            SYSTEM_ACCESSTOKEN: $(System.AccessToken)
                          displayName: Send {{fuzzer.Name}} to OneFuzz
                """;
        }));

        const string StartMarker = "# ONEFUZZ_TASK_WORKAROUND_START";
        const string EndMarker = "# ONEFUZZ_TASK_WORKAROUND_END";

        int start = yaml.IndexOf(StartMarker, StringComparison.Ordinal) + StartMarker.Length;
        int end = yaml.IndexOf(EndMarker, start, StringComparison.Ordinal);

        yaml = string.Concat(yaml.AsSpan(0, start), $"\n{tasks}\n", yaml.AsSpan(end));
        yaml = yaml.ReplaceLineEndings("\r\n");

        File.WriteAllText(yamlPath, yaml);
    }
}
