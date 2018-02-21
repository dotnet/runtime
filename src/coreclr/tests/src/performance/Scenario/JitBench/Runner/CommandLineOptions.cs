using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace JitBench
{
    // Licensed to the .NET Foundation under one or more agreements.
    // The .NET Foundation licenses this file to you under the MIT license.
    // See the LICENSE file in the project root for more information.



    /// <summary>
    /// Provides an interface to parse the command line arguments passed to the TieredJitBench harness.
    /// </summary>
    internal sealed class CommandLineOptions
    {
        public CommandLineOptions() { }

        [Option("use-existing-setup", Required = false, HelpText = "Use existing setup for all benchmarks.")]
        public Boolean UseExistingSetup { get; set; }

        [Option("coreclr-bin-dir", Required = false, HelpText = "Copy private CoreCLR binaries from this directory. (The binaries must match target-architecture)")]
        public string CoreCLRBinaryDir { get; set; }

        [Option("dotnet-framework-version", Required = false, HelpText = "The version of dotnet on which private CoreCLR binaries will be overlayed")]
        public string DotnetFrameworkVersion { get; set; }

        [Option("dotnet-sdk-version", Required = false, HelpText = "The version of dotnet SDK to install for this test")]
        public string DotnetSdkVersion { get; set; }

        [Option("configs", Required = false, Separator=',', HelpText = "A comma list of all configurations that the benchmarks will be run with. The options are: Default, Tiering, Minopts, NoR2R, and NoNgen. " +
                                                                       "If not specified this defaults to a list containing only Default.")]
        public IEnumerable<string> Configs { get; set; }

        [Option("iterations", Required = false, HelpText = "Number of iterations to run.")]
        public uint Iterations { get; set; }

        [Option("perf:outputdir", Required = false, HelpText = "Specifies the output directory name.")]
        public string OutputDirectory { get; set; }

        [Option("target-architecture", Required = false, HelpText = "The architecture of the binaries being tested.")]
        public string TargetArchitecture { get; set; }

        [Option("benchmark", Required=false, HelpText = "A semicolon seperated list of benchmarks to run")]
        public string BenchmarkName { get; set; }

        [Option("perf:runid", Required = false, HelpText = "User defined id given to the performance harness.")]
        public string RunId
        {
            get { return _runid; }

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new Exception("The RunId cannot be null, empty or white space.");
                }

                if (value.Any(c => Path.GetInvalidFileNameChars().Contains(c)))
                {
                    throw new Exception("Specified RunId contains invalid file name characters.");
                }

                _runid = value;
            }
        }

        /*
         * Provider & Reader
         * 
         *  --perf:collect [metric1[+metric2[+...]]]
         *  
         *    default
         *      Set by the test author (This is the default behavior if no option is specified. It will also enable ETW to capture some of the Microsoft-Windows-DotNETRuntime tasks).
         *  
         *    stopwatch
         *      Capture elapsed time using a Stopwatch (It does not require ETW).
         *  
         *    BranchMispredictions|CacheMisses|InstructionRetired
         *      These are performance metric counters and require ETW.
         *  
         *    gcapi
         *      It currently enable "Allocation Size on Benchmark Execution Thread" and it is only available through ETW.
         *  
         *  Examples
         *    --perf:collect default
         *      Collect metrics specified in the test source code by using xUnit Performance API attributes
         *  
         *    --perf:collect BranchMispredictions+CacheMisses+InstructionRetired
         *      Collects PMC metrics
         *  
         *    --perf:collect stopwatch
         *      Collects duration
         *  
         *    --perf:collect default+BranchMispredictions+CacheMisses+InstructionRetired+gcapi
         *      '+' implies union of all specified options
         */
        [Option("perf:collect", Required = false, Separator = '+', Hidden = true,
            HelpText = "The metrics to be collected.")]
        public IEnumerable<string> MetricNames { get; set; }

        public static CommandLineOptions Parse(string[] args)
        {
            using (var parser = new Parser((settings) =>
            {
                settings.CaseInsensitiveEnumValues = true;
                settings.CaseSensitive = false;
                settings.HelpWriter = new StringWriter();
                settings.IgnoreUnknownArguments = true;
            }))
            {
                CommandLineOptions options = null;
                parser.ParseArguments<CommandLineOptions>(args)
                    .WithParsed(parsed => options = parsed)
                    .WithNotParsed(errors =>
                    {
                        foreach (Error error in errors)
                        {
                            switch (error.Tag)
                            {
                                case ErrorType.MissingValueOptionError:
                                    throw new ArgumentException(
                                            $"Missing value option for command line argument '{(error as MissingValueOptionError).NameInfo.NameText}'");
                                case ErrorType.HelpRequestedError:
                                    Console.WriteLine(Usage());
                                    Environment.Exit(0);
                                    break;
                                case ErrorType.VersionRequestedError:
                                    Console.WriteLine(new AssemblyName(typeof(CommandLineOptions).GetTypeInfo().Assembly.FullName).Version);
                                    Environment.Exit(0);
                                    break;
                                case ErrorType.BadFormatTokenError:
                                case ErrorType.UnknownOptionError:
                                case ErrorType.MissingRequiredOptionError:
                                    throw new ArgumentException(
                                            $"Missing required  command line argument '{(error as MissingRequiredOptionError).NameInfo.NameText}'");
                                case ErrorType.MutuallyExclusiveSetError:
                                case ErrorType.BadFormatConversionError:
                                case ErrorType.SequenceOutOfRangeError:
                                case ErrorType.RepeatedOptionError:
                                case ErrorType.NoVerbSelectedError:
                                case ErrorType.BadVerbSelectedError:
                                case ErrorType.HelpVerbRequestedError:
                                    break;
                            }
                        }
                    });
                return options;
            }
        }

        public static string Usage()
        {
            var parser = new Parser((parserSettings) =>
            {
                parserSettings.CaseInsensitiveEnumValues = true;
                parserSettings.CaseSensitive = false;
                parserSettings.EnableDashDash = true;
                parserSettings.HelpWriter = new StringWriter();
                parserSettings.IgnoreUnknownArguments = true;
            });

            var helpTextString = new HelpText
            {
                AddDashesToOption = true,
                AddEnumValuesToHelpText = true,
                AdditionalNewLineAfterOption = false,
                Heading = "JitBench",
                MaximumDisplayWidth = 80,
            }.AddOptions(parser.ParseArguments<CommandLineOptions>(new string[] { "--help" })).ToString();
            return helpTextString;
        }

        private string _runid;
    }
}
