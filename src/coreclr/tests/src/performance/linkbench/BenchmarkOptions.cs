using CommandLine;
using CommandLine.Text;
using Microsoft.Xunit.Performance.Api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LinkBench
{
    internal sealed class BenchmarkOptions
    {
        [Option("nosetup", Default = true, Required = false, HelpText = "Do not clone and fixup benchmark repositories.")]
        public bool DoSetup { get; set; } = true;

        [Option("nobuild", Default = true, Required = false, HelpText = "Do not build and link benchmarks.")]
        public bool DoBuild { get; set; } = true;

        [Option("benchmarks", Required = false, Separator = ',',
            HelpText = "Any of: HelloWorld, WebAPI, MusicStore, MusicStore_R2R, CoreFX, Roslyn. Default is to run all the above benchmarks.")]
        public IEnumerable<string> BenchmarkNames
        {
            get => _benchmarkNames;
            set
            {
                if (value == null)
                    throw new ArgumentNullException("Missing benchmark names.");

                if (value.Count() == 0)
                {
                    _benchmarkNames = ValidBenchmarkNames;
                    return;
                }

                var setDifference = value
                    .Except(ValidBenchmarkNames, StringComparer.OrdinalIgnoreCase);
                if (setDifference.Count() != 0)
                    throw new ArgumentException($"Invalid Benchmark name(s) specified: {string.Join(", ", setDifference)}");
                _benchmarkNames = value;
            }
        }

        private IEnumerable<string> ValidBenchmarkNames => LinkBench.Benchmarks.Select(benchmark => benchmark.Name);

        public static BenchmarkOptions Parse(string[] args)
        {
            using (var parser = new Parser((settings) => {
                settings.CaseInsensitiveEnumValues = true;
                settings.CaseSensitive = false;
                settings.HelpWriter = new StringWriter();
                settings.IgnoreUnknownArguments = true;
            }))
            {
                BenchmarkOptions options = null;
                parser.ParseArguments<BenchmarkOptions>(args)
                    .WithParsed(parsed => options = parsed)
                    .WithNotParsed(errors => {
                        foreach (var error in errors)
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
                                    Console.WriteLine(new AssemblyName(typeof(BenchmarkOptions).GetTypeInfo().Assembly.FullName).Version);
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
            var parser = new Parser((parserSettings) => {
                parserSettings.CaseInsensitiveEnumValues = true;
                parserSettings.CaseSensitive = false;
                parserSettings.EnableDashDash = true;
                parserSettings.HelpWriter = new StringWriter();
                parserSettings.IgnoreUnknownArguments = true;
            });

            var helpTextString = new HelpText {
                AddDashesToOption = true,
                AddEnumValuesToHelpText = true,
                AdditionalNewLineAfterOption = false,
                Heading = "LinkBenchHarness",
                MaximumDisplayWidth = 80,
            }.AddOptions(parser.ParseArguments<BenchmarkOptions>(new string[] { "--help" })).ToString();

            var sb = new StringBuilder(helpTextString);
            sb.AppendLine();
            sb.AppendLine(XunitPerformanceHarness.Usage());
            return sb.ToString();
        }

        private IEnumerable<string> _benchmarkNames;
    }
}
