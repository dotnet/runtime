using CommandLine;
using CommandLine.Text;
using Newtonsoft.Json;
using Microsoft.Xunit.Performance.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SoDBench
{
    // A simple tree node for tracking file and directory names and sizes
    // Does not have to accurately represent the true file system; only what we care about
    class SizeReportingNode
    {
        public SizeReportingNode(string name, long? size=null, bool expand=true)
        {
            Name = name;
            _size = size;
            Expanded = expand;
        }

        public SizeReportingNode(FileInfo file, bool expand=true)
        {
            Name = file.Name;
            _size = file.Length;
            Expanded = expand;
        }

        // Builds out the tree starting from a directory
        public SizeReportingNode(DirectoryInfo dir, int? reportingDepth=null)
        {
            Name = dir.Name;

            foreach (var childDir in dir.EnumerateDirectories())
            {
                AddChild(new SizeReportingNode(childDir));
            }

            foreach (var childFile in dir.EnumerateFiles())
            {
                AddChild(new SizeReportingNode(childFile));
            }

            if (reportingDepth != null)
            {
                LimitReportingDepth(reportingDepth ?? 0);
            }
        }


        // The directory containing this node
        public SizeReportingNode Parent { get; set; }

        // All the directories and files this node contains
        public List<SizeReportingNode> Children {get; private set;} = new List<SizeReportingNode>();

        // The file or directory name
        public string Name { get; set; }

        public bool Expanded { get; set; } = true;

        // A list version of the path up to the root level we care about
        public List<string> SegmentedPath {
            get
            {
                if (Parent != null)
                {
                    var path = Parent.SegmentedPath;
                    path.Add(Name);
                    return path;
                }
                return new List<string> { Name };
            }
        }

        // The size of the file or directory
        public long Size {
            get
            {
                if (_size == null)
                {
                    _size = 0;
                    foreach (var node in Children)
                    {
                        _size += node.Size;
                    }
                }
                return _size ?? 0;
            }

            private set
            {
                _size = value;
            }
        }


        // Add the adoptee node as a child and set the adoptee's parent
        public void AddChild(SizeReportingNode adoptee)
        {
            Children.Add(adoptee);
            adoptee.Parent = this;
            _size = null;
        }

        public void LimitReportingDepth(int depth)
        {
            if (depth <= 0)
            {
                Expanded = false;
            }

            foreach (var childNode in Children)
            {
                childNode.LimitReportingDepth(depth-1);
            }
        }

        // Return a CSV formatted string representation of the tree
        public string FormatAsCsv()
        {
            return FormatAsCsv(new StringBuilder()).ToString();
        }

        // Add to the string build a csv formatted representation of the tree
        public StringBuilder FormatAsCsv(StringBuilder builder)
        {
            string path = String.Join(",", SegmentedPath.Select(s => Csv.Escape(s)));
            builder.AppendLine($"{path},{Size}");

            if (Expanded)
            {
                foreach (var childNode in Children)
                {
                    childNode.FormatAsCsv(builder);
                }
            }

            return builder;
        }

        private long? _size = null;
    }

    class Program
    {
        public static readonly string NugetConfig =
        @"<?xml version='1.0' encoding='utf-8'?>
        <configuration>
        <packageSources>
            <add key='dotnet-public' value='https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json' protocolVersion='3' />
            <add key='myget-legacy' value='https://pkgs.dev.azure.com/dnceng/public/_packaging/myget-legacy/nuget/v3/index.json' protocolVersion='3' />
        </packageSources>
        </configuration>";

        public static readonly string[] NewTemplates = new string[] {
            "console",
            "classlib",
            "mstest",
            "xunit",
            "web",
            "mvc",
            "razor",
            "webapi",
            "nugetconfig",
            "webconfig",
            "sln",
            "page",
            "viewimports",
            "viewstart"
        };

        public static readonly string[] OperatingSystems = new string[] {
            "win10-x64",
            "win10-x86",
            "ubuntu.16.10-x64",
            "rhel.7-x64"
        };

        static FileInfo s_dotnetExe;
        static DirectoryInfo s_sandboxDir;
        static DirectoryInfo s_fallbackDir;
        static DirectoryInfo s_corelibsDir;
        static bool s_keepArtifacts;
        static string s_targetArchitecture;
        static string s_dotnetChannel;

        static void Main(string[] args)
        {
            try
            {
                var options = SoDBenchOptions.Parse(args);

                s_targetArchitecture = options.TargetArchitecture;
                s_dotnetChannel = options.DotnetChannel;
                s_keepArtifacts = options.KeepArtifacts;

                if (!String.IsNullOrWhiteSpace(options.DotnetExecutable))
                {
                    s_dotnetExe = new FileInfo(options.DotnetExecutable);
                }

                if (s_sandboxDir == null)
                {
                    // Truncate the Guid used for anti-collision because a full Guid results in expanded paths over 260 chars (the Windows max)
                    s_sandboxDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), $"sod{Guid.NewGuid().ToString().Substring(0,13)}"));
                    s_sandboxDir.Create();
                    Console.WriteLine($"** Running inside sandbox directory: {s_sandboxDir}");
                }

                if (s_dotnetExe == null)
                {
                    if(!String.IsNullOrEmpty(options.CoreLibariesDirectory))
                    {
                        Console.WriteLine($"** Using core libraries found at {options.CoreLibariesDirectory}");
                        s_corelibsDir = new DirectoryInfo(options.CoreLibariesDirectory);
                    }
                    else
                    {
                        var coreroot = Environment.GetEnvironmentVariable("CORE_ROOT");
                        if (!String.IsNullOrEmpty(coreroot) && Directory.Exists(coreroot))
                        {
                            Console.WriteLine($"** Using core libraries from CORE_ROOT at {coreroot}");
                            s_corelibsDir = new DirectoryInfo(coreroot);
                        }
                        else
                        {
                            Console.WriteLine("** Using default dotnet-cli core libraries");
                        }
                    }

                    PrintHeader("** Installing Dotnet CLI");
                    s_dotnetExe = SetupDotnet();
                }

                if (s_fallbackDir == null)
                {
                    s_fallbackDir = new DirectoryInfo(Path.Combine(s_sandboxDir.FullName, "fallback"));
                    s_fallbackDir.Create();
                }

                Console.WriteLine($"** Path to dotnet executable: {s_dotnetExe.FullName}");

                PrintHeader("** Starting acquisition size test");
                var acquisition = GetAcquisitionSize();

                PrintHeader("** Running deployment size test");
                var deployment = GetDeploymentSize();

                var root = new SizeReportingNode("Dotnet Total");
                root.AddChild(acquisition);
                root.AddChild(deployment);

                var formattedStr = root.FormatAsCsv();

                File.WriteAllText(options.OutputFilename, formattedStr);

                if (options.Verbose)
                    Console.WriteLine($"** CSV Output:\n{formattedStr}");
            }
            finally
            {
                if (!s_keepArtifacts && s_sandboxDir != null)
                {
                    PrintHeader("** Cleaning up sandbox directory");
                    DeleteDirectory(s_sandboxDir);
                    s_sandboxDir = null;
                }
            }
        }

        private static void PrintHeader(string message)
        {
            Console.WriteLine();
            Console.WriteLine("**********************************************************************");
            Console.WriteLine($"** {message}");
            Console.WriteLine("**********************************************************************");
        }

        private static SizeReportingNode GetAcquisitionSize()
        {
            var result = new SizeReportingNode("Acquisition Size");

            // Arbitrary command to trigger first time setup
            ProcessStartInfo dotnet = new ProcessStartInfo()
            {
                WorkingDirectory = s_sandboxDir.FullName,
                FileName = s_dotnetExe.FullName,
                Arguments = "new"
            };

            // Used to set where the packages will be unpacked to.
            // There is a no gaurentee that this is a stable method, but is the only way currently to set the fallback folder location
            dotnet.Environment["DOTNET_CLI_TEST_FALLBACKFOLDER"] = s_fallbackDir.FullName;

            LaunchProcess(dotnet, 180000);

            Console.WriteLine("\n** Measuring total size of acquired files");

            result.AddChild(new SizeReportingNode(s_fallbackDir, 1));

            var dotnetNode = new SizeReportingNode(s_dotnetExe.Directory);
            var reportingDepths = new Dictionary<string, int>
            {
                {"additionalDeps", 1},
                {"host", 0},
                {"sdk", 2},
                {"shared", 2},
                {"store", 3}
            };
            foreach (var childNode in dotnetNode.Children)
            {
                int depth = 0;
                if (reportingDepths.TryGetValue(childNode.Name, out depth))
                {
                    childNode.LimitReportingDepth(depth);
                }
            }
            result.AddChild(dotnetNode);

            return result;
        }

        private static SizeReportingNode GetDeploymentSize()
        {
            // Write the NuGet.Config file
            var nugetConfFile = new FileInfo(Path.Combine(s_sandboxDir.FullName, "NuGet.Config"));
            File.WriteAllText(nugetConfFile.FullName, NugetConfig);

            var result = new SizeReportingNode("Deployment Size");
            foreach (string template in NewTemplates)
            {
                var templateNode = new SizeReportingNode(template);
                result.AddChild(templateNode);

                foreach (var os in OperatingSystems)
                {
                    Console.WriteLine($"\n\n** Deploying {template}/{os}");

                    var deploymentSandbox = new DirectoryInfo(Path.Combine(s_sandboxDir.FullName, template, os));
                    var publishDir = new DirectoryInfo(Path.Combine(deploymentSandbox.FullName, "publish"));
                    deploymentSandbox.Create();

                    ProcessStartInfo dotnetNew = new ProcessStartInfo()
                    {
                        FileName = s_dotnetExe.FullName,
                        Arguments = $"new {template}",
                        UseShellExecute = false,
                        WorkingDirectory = deploymentSandbox.FullName
                    };
                    dotnetNew.Environment["DOTNET_CLI_TEST_FALLBACKFOLDER"] = s_fallbackDir.FullName;

                    ProcessStartInfo dotnetRestore = new ProcessStartInfo()
                    {
                        FileName = s_dotnetExe.FullName,
                        Arguments = $"restore --runtime {os}",
                        UseShellExecute = false,
                        WorkingDirectory = deploymentSandbox.FullName
                    };
                    dotnetRestore.Environment["DOTNET_CLI_TEST_FALLBACKFOLDER"] = s_fallbackDir.FullName;

                    ProcessStartInfo dotnetPublish = new ProcessStartInfo()
                    {
                        FileName = s_dotnetExe.FullName,
                        // The UserSharedCompiler flag is set to false to prevent handles from being held that will later cause deletion of the installed SDK to fail.
                        Arguments = $"publish -c Release --runtime {os} --output {publishDir.FullName} /p:UseSharedCompilation=false /p:UseRazorBuildServer=false",
                        UseShellExecute = false,
                        WorkingDirectory = deploymentSandbox.FullName
                    };
                    dotnetPublish.Environment["DOTNET_CLI_TEST_FALLBACKFOLDER"] = s_fallbackDir.FullName;

                    try
                    {
                        LaunchProcess(dotnetNew, 180000);
                        if (deploymentSandbox.EnumerateFiles().Any(f => f.Name.EndsWith("proj")))
                        {
                            LaunchProcess(dotnetRestore, 180000);
                            LaunchProcess(dotnetPublish, 180000);
                        }
                        else
                        {
                            Console.WriteLine($"** {template} does not have a project file to restore or publish");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine(e.Message);
                        continue;
                    }

                    // If we published this project, only report it's published size
                    if (publishDir.Exists)
                    {
                        var publishNode = new SizeReportingNode(publishDir, 0);
                        publishNode.Name = deploymentSandbox.Name;
                        templateNode.AddChild(publishNode);

                        if (publishNode.Size <= 0) {
                            throw new InvalidOperationException($"{publishNode.Name} reports as invalid size {publishNode.Size}");
                        }
                    }
                    else
                    {
                        templateNode.AddChild(new SizeReportingNode(deploymentSandbox, 0));
                    }
                }
            }
            return result;
        }

        private static void DownloadDotnetInstaller()
        {
            var psi = new ProcessStartInfo() {
                WorkingDirectory = s_sandboxDir.FullName,
                FileName = @"powershell.exe",
                Arguments = $"-NoProfile wget https://raw.githubusercontent.com/dotnet/cli/master/scripts/obtain/dotnet-install.ps1 -OutFile Dotnet-Install.ps1"
            };
            LaunchProcess(psi, 180000);
        }

        private static void InstallSharedRuntime()
        {
            var psi = new ProcessStartInfo() {
                WorkingDirectory = s_sandboxDir.FullName,
                FileName = @"powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File .\\Dotnet-Install.ps1 -Runtime dotnet -InstallDir .dotnet -Channel {s_dotnetChannel} -Architecture {s_targetArchitecture}"
            };
            LaunchProcess(psi, 180000);
        }

        private static void InstallDotnet()
        {
            var psi = new ProcessStartInfo() {
                WorkingDirectory = s_sandboxDir.FullName,
                FileName = @"powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File .\\Dotnet-Install.ps1 -InstallDir .dotnet -Channel {s_dotnetChannel} -Architecture {s_targetArchitecture}"
            };
            LaunchProcess(psi, 180000);
        }

        private static void ModifySharedFramework()
        {
            // Current working directory is the <coreclr repo root>/sandbox directory.
            Console.WriteLine($"** Modifying the shared framework.");

            var sourcedi = s_corelibsDir;

            // Get the directory containing the newest version of Microsodt.NETCore.App libraries
            var targetdi = new DirectoryInfo(
                new DirectoryInfo(Path.Combine(s_sandboxDir.FullName, ".dotnet", "shared", "Microsoft.NETCore.App"))
                .GetDirectories("*")
                .OrderBy(s => s.Name)
                .Last()
                .FullName);

            Console.WriteLine($"| Source : {sourcedi.FullName}");
            Console.WriteLine($"| Target : {targetdi.FullName}");

            var compiledBinariesOfInterest = new string[] {
                "clretwrc.dll",
                "clrjit.dll",
                "coreclr.dll",
                "mscordaccore.dll",
                "mscordbi.dll",
                "mscorrc.dll",
                "sos.dll",
                "SOS.NETCore.dll",
                "System.Private.CoreLib.dll"
            };

            foreach (var compiledBinaryOfInterest in compiledBinariesOfInterest)
            {
                foreach (FileInfo fi in targetdi.GetFiles(compiledBinaryOfInterest))
                {
                    var sourceFilePath = Path.Combine(sourcedi.FullName, fi.Name);
                    var targetFilePath = Path.Combine(targetdi.FullName, fi.Name);

                    if (File.Exists(sourceFilePath))
                    {
                        File.Copy(sourceFilePath, targetFilePath, true);
                        Console.WriteLine($"|   Copied file - '{fi.Name}'");
                    }
                }
            }
        }

        private static FileInfo SetupDotnet()
        {
            DownloadDotnetInstaller();
            InstallSharedRuntime();
            InstallDotnet();
            if (s_corelibsDir != null)
            {
                ModifySharedFramework();
            }

            var dotnetExe = new FileInfo(Path.Combine(s_sandboxDir.FullName, ".dotnet", "dotnet.exe"));
            Debug.Assert(dotnetExe.Exists);

            return dotnetExe;
        }

        private static void LaunchProcess(ProcessStartInfo processStartInfo, int timeoutMilliseconds, IDictionary<string, string> environment = null)
        {
            Console.WriteLine();
            Console.WriteLine($"{System.Security.Principal.WindowsIdentity.GetCurrent().Name}@{Environment.MachineName} \"{processStartInfo.WorkingDirectory}\"");
            Console.WriteLine($"[{DateTime.Now}] $ {processStartInfo.FileName} {processStartInfo.Arguments}");

            if (environment != null)
            {
                foreach (KeyValuePair<string, string> pair in environment)
                {
                    if (!processStartInfo.Environment.ContainsKey(pair.Key))
                        processStartInfo.Environment.Add(pair.Key, pair.Value);
                    else
                        processStartInfo.Environment[pair.Key] = pair.Value;
                }
            }

            using (var p = new Process() { StartInfo = processStartInfo })
            {
                p.Start();
                if (p.WaitForExit(timeoutMilliseconds) == false)
                {
                    // FIXME: What about clean/kill child processes?
                    p.Kill();
                    throw new TimeoutException($"The process '{processStartInfo.FileName} {processStartInfo.Arguments}' timed out.");
                }

                if (p.ExitCode != 0)
                    throw new Exception($"{processStartInfo.FileName} exited with error code {p.ExitCode}");
            }
        }

        /// <summary>
        /// Provides an interface to parse the command line arguments passed to the SoDBench.
        /// </summary>
        private sealed class SoDBenchOptions
        {
            public SoDBenchOptions() { }

            private static string NormalizePath(string path)
            {
                if (String.IsNullOrWhiteSpace(path))
                    throw new InvalidOperationException($"'{path}' is an invalid path: cannot be null or whitespace");

                if (path.Any(c => Path.GetInvalidPathChars().Contains(c)))
                    throw new InvalidOperationException($"'{path}' is an invalid path: contains invalid characters");

                return Path.IsPathRooted(path) ? path : Path.GetFullPath(path);
            }

            [Option('o', Required = false, HelpText = "Specifies the output file name for the csv document")]
            public string OutputFilename
            {
                get { return _outputFilename; }

                set
                {
                    _outputFilename = NormalizePath(value);
                }
            }

            [Option("dotnet", Required = false, HelpText = "Specifies the location of dotnet cli to use.")]
            public string DotnetExecutable
            {
                get { return _dotnetExe; }

                set
                {
                    _dotnetExe = NormalizePath(value);
                }
            }

            [Option("corelibs", Required = false, HelpText = "Specifies the location of .NET Core libaries to patch into dotnet. Cannot be used with --dotnet")]
            public string CoreLibariesDirectory
            {
                get { return _corelibsDir; }

                set
                {
                    _corelibsDir = NormalizePath(value);
                }
            }

            [Option("architecture", Required = false, Default = "x64", HelpText = "JitBench target architecture (It must match the built product that was copied into sandbox).")]
            public string TargetArchitecture { get; set; }

            [Option("channel", Required = false, Default = "master", HelpText = "Specifies the channel to use when installing the dotnet-cli")]
            public string DotnetChannel { get; set; }

            [Option('v', Required = false, HelpText = "Sets output to verbose")]
            public bool Verbose { get; set; }

            [Option("keep-artifacts", Required = false, HelpText = "Specifies that artifacts of this run should be kept")]
            public bool KeepArtifacts { get; set; }

            public static SoDBenchOptions Parse(string[] args)
            {
                using (var parser = new Parser((settings) => {
                    settings.CaseInsensitiveEnumValues = true;
                    settings.CaseSensitive = false;
                    settings.HelpWriter = new StringWriter();
                    settings.IgnoreUnknownArguments = true;
                }))
                {
                    SoDBenchOptions options = null;
                    parser.ParseArguments<SoDBenchOptions>(args)
                        .WithParsed(parsed => options = parsed)
                        .WithNotParsed(errors => {
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
                                        Console.WriteLine(new AssemblyName(typeof(SoDBenchOptions).GetTypeInfo().Assembly.FullName).Version);
                                        Environment.Exit(0);
                                        break;
                                    case ErrorType.BadFormatTokenError:
                                    case ErrorType.UnknownOptionError:
                                    case ErrorType.MissingRequiredOptionError:
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

                    if (options != null && !String.IsNullOrEmpty(options.DotnetExecutable) && !String.IsNullOrEmpty(options.CoreLibariesDirectory))
                    {
                        throw new ArgumentException("--dotnet and --corlibs cannot be used together");
                    }

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
                    Heading = "SoDBench",
                    MaximumDisplayWidth = 80,
                }.AddOptions(parser.ParseArguments<SoDBenchOptions>(new string[] { "--help" })).ToString();
                return helpTextString;
            }

            private string _dotnetExe;
            private string _corelibsDir;
            private string _outputFilename = "measurement.csv";
        }

        private static void DeleteDirectory(DirectoryInfo dir, uint maxWait=10000)
        {
            foreach (var subdir in dir.GetDirectories())
            {
                DeleteDirectory(subdir);
            }

            // Give it time to actually delete all the files
            var files = dir.GetFiles();
            bool wait = true;
            uint waitTime = 0;
            while (wait)
            {
                wait = false;

                foreach (var f in files)
                {
                    if (File.Exists(f.FullName))
                    {
                        try
                        {
                            File.Delete(f.FullName);
                        }
                        catch (IOException) { if (waitTime > maxWait) throw; }
                        catch (UnauthorizedAccessException) { if (waitTime > maxWait) throw; }

                        if (File.Exists(f.FullName))
                        {
                            wait = true;

                            // Print a message every 3 seconds if the thread is stuck
                            if (waitTime != 0 && waitTime % 3000 == 0)
                            {
                                Console.WriteLine($"Waiting to delete {f.FullName}");
                            }
                        }
                    }
                }

                // Try again in 100ms
                if (wait)
                {
                    Thread.Sleep(100);
                    waitTime += 100;
                }
            }

            Directory.Delete(dir.FullName);
        }
    }

    // A simple class for escaping strings for CSV writing
    // https://stackoverflow.com/a/769713
    // Used instead of a package because only these < 20 lines of code are needed
    public static class Csv
    {
        public static string Escape( string s )
        {
            if ( s.Contains( QUOTE ) )
                s = s.Replace( QUOTE, ESCAPED_QUOTE );

            if ( s.IndexOfAny( CHARACTERS_THAT_MUST_BE_QUOTED ) > -1 )
                s = QUOTE + s + QUOTE;

            return s;
        }

        private const string QUOTE = "\"";
        private const string ESCAPED_QUOTE = "\"\"";
        private static char[] CHARACTERS_THAT_MUST_BE_QUOTED = { ',', '"', '\n' };
    }
}
