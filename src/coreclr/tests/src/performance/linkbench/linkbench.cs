using Microsoft.Xunit.Performance;
using Microsoft.Xunit.Performance.Api;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace LinkBench
{
    public class Benchmark
    {
        public string Name;
        public bool ShouldRun;
        public string ProjFile;
        public string UnlinkedDir;
        public string LinkedDir;
        public double UnlinkedMsilSize;
        public double LinkedMsilSize;
        public double UnlinkedDirSize;
        public double LinkedDirSize;
        public double MsilSizeReduction;
        public double DirSizeReduction;

        public delegate void SetupDelegate();
        public SetupDelegate Setup;

        private DirectoryInfo unlinkedDirInfo;
        private DirectoryInfo linkedDirInfo;
        private double certDiff;
        const double MB = 1024 * 1024;

        public Benchmark(string _Name, string _UnlinkedDir, string _LinkedDir, SetupDelegate _setup = null, bool _shouldRun = false)
        {
            Name = _Name;
            UnlinkedDir = _UnlinkedDir;
            LinkedDir = _LinkedDir;
            unlinkedDirInfo = new DirectoryInfo(UnlinkedDir);
            linkedDirInfo = new DirectoryInfo(LinkedDir);
            ShouldRun = _shouldRun;
            Setup = _setup;
        }

        public void SetToRun()
        {
            ShouldRun = true;
            Environment.SetEnvironmentVariable("__test_" + Name, "true");
        }

        public void Compute()
        {
            ComputeCertDiff();
            UnlinkedMsilSize = GetMSILSize(UnlinkedDir);
            LinkedMsilSize = GetMSILSize(LinkedDir);
            UnlinkedDirSize = GetDirSize(unlinkedDirInfo);
            LinkedDirSize = GetDirSize(linkedDirInfo);

            MsilSizeReduction = LinkedMsilSize / UnlinkedMsilSize;
            DirSizeReduction = LinkedDirSize / UnlinkedDirSize;
        }

        // Compute total size of a directory, in MegaBytes
        // Includes all files and subdirectories recursively
        private double GetDirSize(DirectoryInfo dir)
        {
            double size = 0;
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo fileInfo in files)
            {
                size += fileInfo.Length;
            }
            DirectoryInfo[] subDirs = dir.GetDirectories();
            foreach (DirectoryInfo dirInfo in subDirs)
            {
                size += GetDirSize(dirInfo);
            }

            return size / MB;
        }

        // Compute the size of MSIL files in a directory, in MegaBytes
        // Top level only, excludes crossgen files.
        private double GetMSILSize(string dir)
        {
            string[] files = Directory.GetFiles(dir);
            long msilSize = 0;

            foreach (string file in files)
            {
                if (IsMSIL(file))
                {
                    msilSize += new FileInfo(file).Length;
                }
            }

            return msilSize / MB;
        }

        // Gets the size of the Certificate header in a MSIL or ReadyToRun binary.
        private long GetCertSize(string file)
        {
            Process p = new Process();
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = LinkBench.ScriptDir + "GetCert.cmd";
            p.StartInfo.Arguments = file;
            p.Start();
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            long size = Int32.Parse(output.Substring(18, 8),
                NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.HexNumber);
            return size;
        }

        // Get the total size difference for all certificates in all managed binaries 
        // in the unlinked and linked directories.
        private double ComputeCertDiff()
        {
            string[] files = Directory.GetFiles(LinkedDir);
            long totalDiff = 0;

            foreach (string file in files)
            {
                if (IsMSIL(file))
                {
                    FileInfo fileInfo = new FileInfo(file);
                    long linkedCert = GetCertSize(file);
                    long unlinkedCert = GetCertSize(UnlinkedDir + "\\" + fileInfo.Name);
                    totalDiff += (unlinkedCert - linkedCert);
                }
            }

            return totalDiff / MB;
        }

        //Use AssemblyLoadContext.GetAssemblyName(file);
        private bool IsMSIL(string file)
        {
            if (file.EndsWith(".ni.dll") || file.EndsWith(".ni.exe"))
            {
                // Likely Native Image.
                return false;
            }

            try
            {
                AssemblyLoadContext.GetAssemblyName(file);
            }
            catch (Exception)
            {
                // We should check only for BadImageFormatException.
                // But Checking for any exception until the following
                // issue is fixed:
                // https://github.com/dotnet/coreclr/issues/11499 

                return false;
            }

            return true;
        }

        public static void AddLinkerReference(string csproj)
        {
            var xdoc = XDocument.Load(csproj);
            var ns = xdoc.Root.GetDefaultNamespace();
            bool added = false;
            foreach (var el in xdoc.Root.Elements(ns + "ItemGroup"))
            {
                if (el.Elements(ns + "PackageReference").Any())
                {
                    el.Add(new XElement(ns + "PackageReference",
                        new XAttribute("Include", "ILLink.Tasks"),
                        new XAttribute("Version", "0.1.4-preview")));
                    added = true;
                    break;
                }
            }
            if (!added)
            {
                xdoc.Root.Add(new XElement(ns + "ItemGroup",
                    new XElement(ns + "PackageReference",
                        new XAttribute("Include", "ILLink.Tasks"),
                        new XAttribute("Version", "0.1.4-preview"))));
                added = true;
            }
            using (var fs = new FileStream(csproj, FileMode.Create))
            {
                xdoc.Save(fs);
            }
        }

        // TODO: remove this once the linker is able to handle
        // ready-to-run assembies
        public static void SetRuntimeFrameworkVersion(string csproj)
        {
            var xdoc = XDocument.Load(csproj);
            var ns = xdoc.Root.GetDefaultNamespace();
            var versionElement = xdoc.Root.Descendants(ns + "RuntimeFrameworkVersion").First();
            string runtimeFrameworkVersion = "2.0.0-preview2-002093-00";
            versionElement.Value = runtimeFrameworkVersion;
            using (var fs = new FileStream(csproj, FileMode.Create))
            {
                xdoc.Save(fs);
            }
        }

        // TODO: Remove this once we figure out what to do about apps
        // that have the publish output filtered by a manifest
        // file. It looks like aspnet has made this the default. See
        // the bug at https://github.com/dotnet/sdk/issues/1160.
        public static void PreventPublishFiltering(string csproj)
        {
            var xdoc = XDocument.Load(csproj);
            var ns = xdoc.Root.GetDefaultNamespace();
            var propertygroup = xdoc.Root.Element(ns + "PropertyGroup");
            propertygroup.Add(new XElement(ns + "PublishWithAspNetCoreTargetManifest",
                                           "false"));
            using (var fs = new FileStream(csproj, FileMode.Create))
            {
                xdoc.Save(fs);
            }
        }
    }

    public class LinkBench
    {
        private static ScenarioConfiguration scenarioConfiguration = new ScenarioConfiguration(new TimeSpan(2000000));
        private static MetricModel SizeMetric = new MetricModel { Name = "Size", DisplayName = "File Size", Unit = "MB" };
        private static MetricModel PercMetric = new MetricModel { Name = "Ratio", DisplayName = "Reduction", Unit = "Linked/Unlinked" };
        public static string Workspace;
        public static string LinkBenchRoot;
        public static string ScriptDir;
        public static string AssetsDir;
        private static Benchmark CurrentBenchmark;

        private static Benchmark[] Benchmarks =
        {
            new Benchmark("HelloWorld",
                "HelloWorld\\bin\\release\\netcoreapp2.0\\win10-x64\\unlinked",
                "HelloWorld\\bin\\release\\netcoreapp2.0\\win10-x64\\linked",
                () => Benchmark.AddLinkerReference("HelloWorld\\HelloWorld.csproj")),
            new Benchmark("WebAPI",
                "WebAPI\\bin\\release\\netcoreapp2.0\\win10-x64\\unlinked",
                "WebAPI\\bin\\release\\netcoreapp2.0\\win10-x64\\linked",
                () => { Benchmark.AddLinkerReference("WebAPI\\WebAPI.csproj");
                        Benchmark.PreventPublishFiltering("WebAPI\\WebAPI.csproj"); }),
            //Remove the MusicStore from the run as the scenario is currently flaky and breaking performance runs
            /*new Benchmark("MusicStore",
                "JitBench\\src\\MusicStore\\bin\\release\\netcoreapp2.0\\win10-x64\\unlinked",
                "JitBench\\src\\MusicStore\\bin\\release\\netcoreapp2.0\\win10-x64\\linked",
                () => { Benchmark.AddLinkerReference("JitBench\\src\\MusicStore\\MusicStore.csproj");
                       Benchmark.SetRuntimeFrameworkVersion("JitBench\\src\\MusicStore\\MusicStore.csproj"); }),
            new Benchmark("MusicStore_R2R",
                "JitBench\\src\\MusicStore\\bin\\release\\netcoreapp2.0\\win10-x64\\R2R\\unlinked",
                "JitBench\\src\\MusicStore\\bin\\release\\netcoreapp2.0\\win10-x64\\R2R\\linked"),*/
            new Benchmark("Corefx",
                "corefx\\bin\\ILLinkTrimAssembly\\netcoreapp-Windows_NT-Release-x64\\pretrimmed",
                "corefx\\bin\\ILLinkTrimAssembly\\netcoreapp-Windows_NT-Release-x64\\trimmed"),
            /*new Benchmark("Roslyn",
                "roslyn\\Binaries\\Release\\Exes\\CscCore\\win7-x64\\publish",
                "roslyn\\Binaries\\Release\\Exes\\CscCore\\win7-x64\\Linked") */
        };

        static int UsageError()
        {
            Console.WriteLine("Usage: LinkBench [--nosetup] [--nobuild] [--perf:runid <id>] [<benchmarks>]");
            Console.WriteLine("  --nosetup: Don't clone and fixup benchmark repositories");
            Console.WriteLine("  --nosetup: Don't build and link benchmarks");
            Console.WriteLine("  --perf:runid: Specify the ID to append to benchmark result files");
            Console.WriteLine("    Benchmarks: HelloWorld, WebAPI, MusicStore, MusicStore_R2R, CoreFX, Roslyn");
            Console.WriteLine("                Default is to run all the above benchmarks.");
            return -4;
        }

        public static int Main(String [] args)
        {
            bool doSetup = true;
            bool doBuild = true;
            string runId = "";
            string runOne = null;
            bool benchmarkSpecified = false;

            for (int i = 0; i < args.Length; i++)
            {
                if (String.Compare(args[i], "--nosetup", true) == 0)
                {
                    doSetup = false;
                }
                else if (String.Compare(args[i], "--nobuild", true) == 0)
                {
                    doSetup = false;
                    doBuild = false;
                }
                else if (String.Compare(args[i], "--perf:runid", true) == 0)
                {
                    if (i + 1 < args.Length)
                    {
                        runId = args[++i] + "-";
                    }
                    else
                    {
                        Console.WriteLine("Missing runID ");
                        return UsageError();
                    }
                }
                else if (args[i][0] == '-')
                {
                    Console.WriteLine("Unknown Option {0}", args[i]);
                    return UsageError();
                }
                else
                {
                    foreach (Benchmark benchmark in Benchmarks)
                    {
                        if (String.Compare(args[i], benchmark.Name, true) == 0)
                        {
                            benchmark.SetToRun();
                            benchmarkSpecified = true;
                            break;
                        }
                    }

                    if (!benchmarkSpecified)
                    {
                        Console.WriteLine("Unknown Benchmark {0}", args[i]);
                    }
                }
            }

            // If benchmarks are not explicitly specified, run all benchmarks
            if (!benchmarkSpecified)
            {
                foreach (Benchmark benchmark in Benchmarks)
                {
                    if (String.Compare(benchmark.Name, "CoreFX", true) == 0)
                    {
                        // CoreFX is not enabled by default, because the lab cannot run it yet.
                        // Jenkins runs on an older OS with path-length limit, which causes 
                        // CoreFX build to fail.
                        continue;
                    }

                    benchmark.SetToRun();
                }
            }

            // Workspace is the ROOT of the coreclr tree.
            // If CORECLR_REPO is not set, the script assumes that the location of sandbox
            // is <path>\coreclr\sandbox.
            LinkBenchRoot = Directory.GetCurrentDirectory();
            Workspace = Environment.GetEnvironmentVariable("CORECLR_REPO");
            if (Workspace == null)
            {
                Workspace = Directory.GetParent(LinkBenchRoot).FullName;
            }
            if (Workspace == null)
            {
                Console.WriteLine("CORECLR_REPO not found");
                return -1;
            }

            string linkBenchSrcDir = Workspace + "\\tests\\src\\performance\\linkbench\\";
            ScriptDir = linkBenchSrcDir + "scripts\\";
            AssetsDir = linkBenchSrcDir + "assets\\";

            Environment.SetEnvironmentVariable("LinkBenchRoot", LinkBenchRoot);
            Environment.SetEnvironmentVariable("__dotnet1", LinkBenchRoot + "\\.Net1\\dotnet.exe");
            Environment.SetEnvironmentVariable("__dotnet2", LinkBenchRoot + "\\.Net2\\dotnet.exe");

            // Update the build files to facilitate the link step
            if (doSetup)
            {
                // Clone the benchmarks
                using (var setup = new Process())
                {
                    setup.StartInfo.FileName = ScriptDir + "clone.cmd";
                    setup.Start();
                    setup.WaitForExit();
                    if (setup.ExitCode != 0)
                    {
                        Console.WriteLine("Benchmark Setup failed");
                        return -2;
                    }
                }

                // Setup the benchmarks

                foreach (Benchmark benchmark in Benchmarks)
                {
                    if (benchmark.ShouldRun && benchmark.Setup != null)
                    {
                        benchmark.Setup();
                    }
                }
            }

            if (doBuild)
            {
                // Run the setup Script, which clones, builds and links the benchmarks.
                using (var setup = new Process())
                {
                    setup.StartInfo.FileName = ScriptDir + "build.cmd";
                    setup.StartInfo.Arguments = AssetsDir;
                    setup.Start();
                    setup.WaitForExit();
                    if (setup.ExitCode != 0)
                    {
                        Console.WriteLine("Benchmark build failed");
                        return -3;
                    }
                }
            }

            // Since this is a size measurement scenario, there are no iterations 
            // to perform. So, create a process that does nothing, to satisfy XUnit.
            // All size measurements are performed PostRun()
            var emptyCmd = new ProcessStartInfo()
            {
                FileName = ScriptDir + "empty.cmd"
            };

            for (int i = 0; i < Benchmarks.Length; i++)
            {
                CurrentBenchmark = Benchmarks[i];
                if (!CurrentBenchmark.ShouldRun)
                {
                    continue;
                }

                string[] scriptArgs = { "--perf:runid", runId + CurrentBenchmark.Name };
                using (var h = new XunitPerformanceHarness(scriptArgs))
                {
                    h.RunScenario(emptyCmd, null, null, PostRun, scenarioConfiguration);
                }
            }

            return 0;
        }

        private static ScenarioBenchmark PostRun()
        {
            // The XUnit output doesn't print the benchmark name, so print it now.
            Console.WriteLine("{0}", CurrentBenchmark.Name);

            var scenario = new ScenarioBenchmark(CurrentBenchmark.Name)
            {
                Namespace = "LinkBench"
            };

            CurrentBenchmark.Compute();

            addMeasurement(ref scenario, "MSIL Unlinked", SizeMetric, CurrentBenchmark.UnlinkedMsilSize);
            addMeasurement(ref scenario, "MSIL Linked", SizeMetric, CurrentBenchmark.LinkedMsilSize);
            addMeasurement(ref scenario, "MSIL Reduction", PercMetric, CurrentBenchmark.MsilSizeReduction);
            addMeasurement(ref scenario, "Total Uninked", SizeMetric, CurrentBenchmark.UnlinkedDirSize);
            addMeasurement(ref scenario, "Total Linked", SizeMetric, CurrentBenchmark.LinkedDirSize);
            addMeasurement(ref scenario, "Total Reduction", PercMetric, CurrentBenchmark.DirSizeReduction);

            return scenario;
        }

        private static void addMeasurement(ref ScenarioBenchmark scenario, string name, MetricModel metric, double value)
        {
            var iteration = new IterationModel
            {
                Iteration = new Dictionary<string, double>()
            };
            iteration.Iteration.Add(metric.Name, value);

            var size = new ScenarioTestModel(name);
            size.Performance.Metrics.Add(metric);
            size.Performance.IterationModels.Add(iteration);
            scenario.Tests.Add(size);
        }
    }
}
