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
using Microsoft.Xunit.Performance;
using Microsoft.Xunit.Performance.Api;
using Xunit;
using Xunit.Abstractions;

namespace LinkBench
{
    public class Benchmark
    {
        public string Name;

        public string UnlinkedDir;
        public string LinkedDir;
        public double UnlinkedMsilSize;
        public double LinkedMsilSize;
        public double UnlinkedDirSize;
        public double LinkedDirSize;
        public double MsilSizeReduction;
        public double DirSizeReduction;

        private DirectoryInfo unlinkedDirInfo;
        private DirectoryInfo linkedDirInfo;
        private double certDiff;
        const double MB = 1024 * 1024;

        public Benchmark(string _Name, string _UnlinkedDir, string _LinkedDir)
        {
            Name = _Name;
            UnlinkedDir = _UnlinkedDir;
            LinkedDir = _LinkedDir;
            unlinkedDirInfo = new DirectoryInfo(UnlinkedDir);
            linkedDirInfo = new DirectoryInfo(LinkedDir);
        }

        public void Compute()
        {
            ComputeCertDiff();
            UnlinkedMsilSize = GetMSILSize(UnlinkedDir);
            LinkedMsilSize = GetMSILSize(LinkedDir);
            UnlinkedDirSize = GetDirSize(unlinkedDirInfo);
            LinkedDirSize = GetDirSize(linkedDirInfo);

            MsilSizeReduction = (UnlinkedMsilSize - LinkedMsilSize) / UnlinkedMsilSize * 100;
            DirSizeReduction = (UnlinkedDirSize - LinkedDirSize) / UnlinkedDirSize * 100;
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
                if (file.EndsWith(".ni.dll") || file.EndsWith(".ni.exe"))
                {
                    continue;
                }
                try
                {
                    AssemblyLoadContext.GetAssemblyName(file);
                }
                catch (BadImageFormatException)
                {
                    continue;
                }

                msilSize += new FileInfo(file).Length;
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
                try
                {
                    AssemblyLoadContext.GetAssemblyName(file);
                }
                catch (BadImageFormatException)
                {
                    continue;
                }

                FileInfo fileInfo = new FileInfo(file);
                long linkedCert = GetCertSize(file);
                long unlinkedCert = GetCertSize(UnlinkedDir + "\\" + fileInfo.Name);
                totalDiff += (unlinkedCert - linkedCert);
            }

            return totalDiff / MB;
        }
    }

    public class LinkBench
    {
        private static ScenarioConfiguration scenarioConfiguration = new ScenarioConfiguration(new TimeSpan(2000000));
        private static MetricModel SizeMetric = new MetricModel { Name = "Size", DisplayName = "File Size", Unit = "MB" };
        private static MetricModel PercMetric = new MetricModel { Name = "Perc", DisplayName = "% Reduction", Unit = "%" };
        public static string Workspace;
        public static string ScriptDir;
        public static string AssetsDir;
        private static Benchmark CurrentBenchmark;

        public static int Main(String [] args)
        {
            // Workspace is the ROOT of the coreclr tree.
            // If CORECLR_REPO is not set, the script assumes that the location of sandbox
            // is <path>\coreclr\sandbox.
            bool doClone = true;
            bool doBuild = true;

            for(int i=0; i < args.Length; i++)
            {
                if (String.Compare(args[i], "noclone", true) == 0)
                {
                    doClone = false;
                }
                else if (String.Compare(args[i], "nobuild", true) == 0)
                {
                    doClone = false;
                    doBuild = false;
                }
                else
                {
                    Console.WriteLine("Unknown argument");
                    return -4;
                }
            }

            Workspace = Environment.GetEnvironmentVariable("CORECLR_REPO");
            if (Workspace == null)
            {
                Workspace = Directory.GetParent(Directory.GetCurrentDirectory()).FullName;
            }
            if (Workspace == null)
            {
                Console.WriteLine("CORECLR_REPO not found");
                return -1;
            }

            string LinkBenchDir = Workspace + "\\tests\\src\\performance\\linkbench\\";
            ScriptDir = LinkBenchDir + "scripts\\";
            AssetsDir = LinkBenchDir + "assets\\";

            Benchmark[] Benchmarks =
            {
                new Benchmark("HelloWorld", 
                              "LinkBench\\HelloWorld\\bin\\release\\netcoreapp2.0\\win10-x64\\publish", 
                              "LinkBench\\HelloWorld\\bin\\release\\netcoreapp2.0\\win10-x64\\linked"),
                new Benchmark("WebAPI",
                              "LinkBench\\WebAPI\\bin\\release\\netcoreapp2.0\\win10-x64\\publish",
                              "LinkBench\\WebAPI\\bin\\release\\netcoreapp2.0\\win10-x64\\linked"),
                new Benchmark("MusicStore", 
                              "LinkBench\\JitBench\\src\\MusicStore\\bin\\release\\netcoreapp2.0\\win10-x64\\publish",
                              "LinkBench\\JitBench\\src\\MusicStore\\bin\\release\\netcoreapp2.0\\win10-x64\\linked"),
                new Benchmark("MusicStore_R2R", 
                              "LinkBench\\JitBench\\src\\MusicStore\\bin\\release\\netcoreapp2.0\\win10-x64\\publish_r2r",
                              "LinkBench\\JitBench\\src\\MusicStore\\bin\\release\\netcoreapp2.0\\win10-x64\\linked_r2r"),
                new Benchmark("Corefx", 
                              "LinkBench\\corefx\\bin\\ILLinkTrimAssembly\\netcoreapp-Windows_NT-Release-x64\\pretrimmed",
                              "LinkBench\\corefx\\bin\\ILLinkTrimAssembly\\netcoreapp-Windows_NT-Release-x64\\trimmed"),
                new Benchmark("Roslyn", 
                              "LinkBench\\roslyn\\Binaries\\Release\\Exes\\CscCore",
                              "LinkBench\\roslyn\\Binaries\\Release\\Exes\\Linked"),
            };

            // Update the build files to facilitate the link step
            if(doClone)
            {
                if(!Setup())
                {
                    return -2;
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
                        Console.WriteLine("Setup failed");
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
                string[] scriptArgs = { "--perf:runid", CurrentBenchmark.Name };

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
            addMeasurement(ref scenario, "MSIL %Reduction", PercMetric, CurrentBenchmark.MsilSizeReduction);
            addMeasurement(ref scenario, "Total Uninked", SizeMetric, CurrentBenchmark.UnlinkedDirSize);
            addMeasurement(ref scenario, "Total Linked", SizeMetric, CurrentBenchmark.LinkedDirSize);
            addMeasurement(ref scenario, "Total %Reduction", PercMetric, CurrentBenchmark.DirSizeReduction);

            return scenario;
        }

        private static bool Setup()
        {
            // Clone the benchmarks
            using (var setup = new Process())
            {
                setup.StartInfo.FileName = ScriptDir + "clone.cmd";
                Console.WriteLine("Run {0}", setup.StartInfo.FileName);
                setup.Start();
                setup.WaitForExit();
                if (setup.ExitCode != 0)
                {
                    Console.WriteLine("clone failed");
                    return false;
                }
            }

            //Update the project files
            AddLinkerReference("LinkBench\\HelloWorld\\HelloWorld.csproj");
            AddLinkerReference("LinkBench\\WebAPI\\WebAPI.csproj");
            AddLinkerReference("LinkBench\\JitBench\\src\\MusicStore\\MusicStore.csproj");
            RemoveCrossgenTarget("LinkBench\\JitBench\\src\\MusicStore\\MusicStore.csproj");

            return true;
        }

        private static void AddLinkerReference(string csproj)
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
                        new XAttribute("Version", "0.1.0-preview")));
                    added = true;
                    break;
                }
            }
            if (!added)
            {
                xdoc.Root.Add(new XElement(ns + "ItemGroup",
                    new XElement(ns + "PackageReference",
                        new XAttribute("Include", "ILLink.Tasks"),
                        new XAttribute("Version", "0.1.0-preview"))));
                added = true;
            }
            using (var fs = new FileStream(csproj, FileMode.Create))
            {
                xdoc.Save(fs);
            }
        }

        private static void RemoveCrossgenTarget(string csproj)
        {
            var xdoc = XDocument.Load(csproj);
            var ns = xdoc.Root.GetDefaultNamespace();
            var target = xdoc.Root.Element(ns + "Target");
            target.Remove();
            using (var fs = new FileStream(csproj, FileMode.Create))
            {
                xdoc.Save(fs);
            }
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
