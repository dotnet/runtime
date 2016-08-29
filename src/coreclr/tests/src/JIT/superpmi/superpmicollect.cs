// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SuperPMICollection
{

    public class SpmiException : Exception
    {
        public SpmiException() : base()
        { }

        public SpmiException(string message)
            : base(message)
        { }

        public SpmiException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }

    internal class Global
    {
        // Arguments to the program. These should not be touched by Initialize(), as they are set earlier than that.
        internal static bool SkipCleanup = false;          // Should we skip all cleanup? That is, should we keep all temporary files? Useful for debugging.

        // Computed values based on the environment and platform.
        internal static bool IsWindows { get; private set; }
        internal static bool IsOSX { get; private set; }
        internal static bool IsLinux { get; private set; }

        internal static string CoreRoot { get; private set; }
        internal static string StandaloneJitName { get; private set; }
        internal static string CollectorShimName { get; private set; }
        internal static string SuperPmiToolName { get; private set; }
        internal static string McsToolName { get; private set; }
        internal static string JitPath { get; private set; }             // Path to the standalone JIT
        internal static string SuperPmiPath { get; private set; }        // Path to superpmi.exe
        internal static string McsPath { get; private set; }             // Path to mcs.exe

        // Initialize the global state. Don't use a class constructor, because we might throw exceptions
        // that we want to catch.
        public static void Initialize()
        {
            string core_root_raw = System.Environment.GetEnvironmentVariable("CORE_ROOT");
            if (String.IsNullOrEmpty(core_root_raw))
            {
                throw new SpmiException("Environment variable CORE_ROOT is not set");
            }

            try
            {
                CoreRoot = System.IO.Path.GetFullPath(core_root_raw);
            }
            catch (Exception ex)
            {
                throw new SpmiException("Illegal CORE_ROOT environment variable (" + core_root_raw + "), exception: " + ex.Message);
            }

            IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            IsOSX     = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            IsLinux   = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

            if (IsWindows)
            {
                StandaloneJitName = "clrjit.dll";
                CollectorShimName = "superpmi-shim-collector.dll";
                SuperPmiToolName  = "superpmi.exe";
                McsToolName       = "mcs.exe";
            }
            else if (IsLinux)
            {
                StandaloneJitName = "libclrjit.so";
                CollectorShimName = "libsuperpmi-shim-collector.so";
                SuperPmiToolName  = "superpmi";
                McsToolName       = "mcs";
            }
            else if (IsOSX)
            {
                StandaloneJitName = "libclrjit.dylib";
                CollectorShimName = "libsuperpmi-shim-collector.dylib";
                SuperPmiToolName  = "superpmi";
                McsToolName       = "mcs";
            }
            else
            {
                throw new SpmiException("Unknown platform");
            }

            JitPath      = Path.Combine(CoreRoot, StandaloneJitName);
            SuperPmiPath = Path.Combine(CoreRoot, SuperPmiToolName);
            McsPath      = Path.Combine(CoreRoot, McsToolName);
        }
    }

    internal class SuperPMICollectionClass
    {
        private static string s_tempDir = null;             // Temporary directory where we will put the MC files, MCH files, MCL files, and TOC.
        private static string s_baseFailMclFile = null;     // Pathname for a temporary .MCL file used for noticing superpmi replay failures against base MCH.
        private static string s_finalFailMclFile = null;    // Pathname for a temporary .MCL file used for noticing superpmi replay failures against final MCH.
        private static string s_baseMchFile = null;         // The base .MCH file path
        private static string s_cleanMchFile = null;        // The clean .MCH file path
        private static string s_finalMchFile = null;        // The clean thin unique .MCH file path
        private static string s_tocFile = null;             // The .TOC file path for the clean thin unique .MCH file
        private static string s_errors = "";                // Collect non-fatal file delete errors to display at the end of the collection process.

        private static bool s_saveFinalMchFile = false;     // Should we save the final MCH file, or delete it?

        private static void SafeFileDelete(string filePath)
        {
            try
            {
                File.Delete(filePath);
            }
            catch(Exception ex)
            {
                string err = string.Format("Error deleting file \"{0}\": {1}", filePath, ex.Message);
                s_errors += err + System.Environment.NewLine;
                Console.Error.WriteLine(err);
            }
        }

        private static void CreateTempDirectory(string tempPath)
        {
            if (tempPath == null)
            {
                tempPath = Path.GetTempPath();
            }
            s_tempDir = Path.Combine(tempPath, Path.GetRandomFileName() + "SPMI");
            if (Directory.Exists(s_tempDir))
            {
                throw new SpmiException("temporary directory already exists: " + s_tempDir);
            }
            DirectoryInfo di = Directory.CreateDirectory(s_tempDir);
        }

        private static void ChooseFilePaths(string outputMchPath)
        {
            s_baseFailMclFile  = Path.Combine(s_tempDir, "basefail.mcl");
            s_finalFailMclFile = Path.Combine(s_tempDir, "finalfail.mcl");
            s_baseMchFile      = Path.Combine(s_tempDir, "base.mch");
            s_cleanMchFile     = Path.Combine(s_tempDir, "clean.mch");

            if (outputMchPath == null)
            {
                s_saveFinalMchFile = false;
                s_finalMchFile = Path.Combine(s_tempDir, "final.mch");
                s_tocFile = Path.Combine(s_tempDir, "final.mch.mct");
            }
            else
            {
                s_saveFinalMchFile = true;
                s_finalMchFile = Path.GetFullPath(outputMchPath);
                s_tocFile = s_finalMchFile + ".mct";
            }
        }

        private static int RunProgram(string program, string arguments)
        {
            // If the program is a script, move the program name into the arguments, and run it
            // under the appropriate shell.
            if (Global.IsWindows)
            {
                if ((program.LastIndexOf(".bat") != -1) || (program.LastIndexOf(".cmd") != -1))
                {
                    string programArgumentSep = String.IsNullOrEmpty(arguments) ? "" : " ";
                    arguments = "/c " + program + programArgumentSep + arguments;
                    program = Environment.GetEnvironmentVariable("ComSpec"); // path to CMD.exe
                }
            }
            else
            {
                if (program.LastIndexOf(".sh") != -1)
                {
                    string programArgumentSep = String.IsNullOrEmpty(arguments) ? "" : " ";
                    arguments = "bash " + program + programArgumentSep + arguments;
                    program = "/usr/bin/env";
                }
            }

            Console.WriteLine("Running: " + program + " " + arguments);
            Process p = Process.Start(program, arguments);
            p.WaitForExit();
            return p.ExitCode;
        }

        // Run a single test from the coreclr test binary drop.
        // This works even if given a test path in Windows file system format (e.g.,
        // "c:\foo\bar\runit.cmd") when run on Unix. It converts to Unix path format and replaces
        // the ".cmd" with ".sh" before attempting to run the script.
        private static void RunTest(string testName)
        {
            string testDir;

            if (Global.IsWindows)
            {
                int lastIndex = testName.LastIndexOf("\\");
                if (lastIndex == -1)
                {
                    throw new SpmiException("test path doesn't have any directory separators? " + testName);
                }
                testDir = testName.Substring(0, lastIndex);
            }
            else
            {
                // Just in case we've been given a test name in Windows format, convert it to Unix format here.

                testName = testName.Replace("\\", "/");
                testName = testName.Replace(".cmd", ".sh");
                testName = testName.Replace(".bat", ".sh");

                // The way tests are run on Linux, we might need to do some setup. In particular,
                // if the test scripts are copied from Windows, we need to convert line endings
                // to Unix line endings, and make the script executable. We can always do this
                // more than once. This same transformation is done in runtest.sh.
                // Review: RunProgram doesn't seem to work if the program isn't a full path.

                RunProgram("/usr/bin/perl", @"-pi -e 's/\r\n|\n|\r/\n/g' " + "\"" + testName + "\"");
                RunProgram("/bin/chmod", "+x \"" + testName + "\"");

                // Now, figure out how to run the test.

                int lastIndex = testName.LastIndexOf("/");
                if (lastIndex == -1)
                {
                    throw new SpmiException("test path doesn't have any directory separators? " + testName);
                }
                testDir = testName.Substring(0, lastIndex);
            }

            // Run the script in the same directory where the test lives.
            string originalDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(testDir);

            try
            {
                RunProgram(testName, "");
            }
            finally
            {
                // Restore the original current directory from before the test run.
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        // Run all the programs from the CoreCLR test binary drop we wish to run while collecting MC files.
        private static void RunTestProgramsWhileCollecting()
        {
            // The list of all programs from the CoreCLR repo test binary drop that
            // we should run when doing the SuperPMI collection. This is currently a
            // hard-coded list of the relative paths within the test build binaries
            // directory of the Windows .cmd files used to run a test. For non-Windows
            // platforms, the .cmd is replaced by .sh, and the path separator character
            // is changed.
            //
            // TODO: this should probably be loaded dynamically from a .json/.xml file.
            //
            // Note: We previously used
            //      JIT\Performance\CodeQuality\Roslyn\CscBench\CscBench.cmd
            // but it doesn't currently run on x86 due to this issue: https://github.com/dotnet/coreclr/issues/6844.
            string[] SuperPMICollectionTestProgramsList =
            {
                @"JIT\Performance\CodeQuality\Bytemark\Bytemark\Bytemark.cmd",
                @"JIT\Methodical\fp\exgen\10w5d_cs_do\10w5d_cs_do.cmd",
                @"JIT\Generics\Coverage\chaos65204782cs\chaos65204782cs.cmd"
            };

            // Figure out the root of the test binaries directory.
            // Perhaps this (or something similar) would be a better way to figure out the binary root dir:
            // testBinaryRootDir = System.IO.Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);

            string thisTestDir = Directory.GetCurrentDirectory();
            int lastIndex = thisTestDir.LastIndexOf("JIT");
            if (lastIndex == -1)
            {
                throw new SpmiException("we expect the current directory when the test is run to be within the JIT test binaries tree, but it is not: " + thisTestDir);
            }
            string testBinaryRootDir = thisTestDir.Substring(0, lastIndex);

            // Run the tests
            foreach (string test in SuperPMICollectionTestProgramsList)
            {
                string testFullPath = Path.Combine(testBinaryRootDir, test);
                try
                {
                    RunTest(testFullPath);
                }
                catch (SpmiException ex)
                {
                    // Ignore failures running the test. We don't really care if they pass or not
                    // as long as they generate some .MC files. Plus, I'm not sure how confident
                    // we can be in getting a correct error code.

                    Console.Error.WriteLine("WARNING: test failed (ignoring): " + ex.Message);
                }
            }
        }

        // Run all the programs we wish to run while collecting MC files.
        private static void RunProgramsWhileCollecting(string runProgramPath, string runProgramArguments)
        {
            if (runProgramPath == null)
            {
                // No program was given to use for collection, so use our default set.
                RunTestProgramsWhileCollecting();
            }
            else
            {
                RunProgram(runProgramPath, runProgramArguments);
            }
        }

        // Collect MC files:
        //      a. Set environment variables
        //      b. Run tests
        //      c. Un-set environment variables
        //      d. Check that something was generated
        private static void CollectMCFiles(string runProgramPath, string runProgramArguments)
        {
            // Set environment variables.
            Console.WriteLine("Setting environment variables:");
            Console.WriteLine("    SuperPMIShimLogPath=" + s_tempDir);
            Console.WriteLine("    SuperPMIShimPath=" + Global.JitPath);
            Console.WriteLine("    COMPlus_AltJit=*");
            Console.WriteLine("    COMPlus_AltJitName=" + Global.CollectorShimName);

            Environment.SetEnvironmentVariable("SuperPMIShimLogPath", s_tempDir);
            Environment.SetEnvironmentVariable("SuperPMIShimPath", Global.JitPath);
            Environment.SetEnvironmentVariable("COMPlus_AltJit", "*");
            Environment.SetEnvironmentVariable("COMPlus_AltJitName", Global.CollectorShimName);

            RunProgramsWhileCollecting(runProgramPath, runProgramArguments);

            // Un-set environment variables
            Environment.SetEnvironmentVariable("SuperPMIShimLogPath", "");
            Environment.SetEnvironmentVariable("SuperPMIShimPath", "");
            Environment.SetEnvironmentVariable("COMPlus_AltJit", "");
            Environment.SetEnvironmentVariable("COMPlus_AltJitName", "");

            // Did any .mc files get generated?
            string[] mcFiles = Directory.GetFiles(s_tempDir, "*.mc");
            if (mcFiles.Length == 0)
            {
                throw new SpmiException("no .mc files generated");
            }
        }

        // Merge MC files:
        //      mcs -merge <s_baseMchFile> <s_tempDir>\*.mc -recursive
        private static void MergeMCFiles()
        {
            string pattern = Path.Combine(s_tempDir, "*.mc");
            RunProgram(Global.McsPath, "-merge " + s_baseMchFile + " " + pattern + " -recursive");
            if (!File.Exists(s_baseMchFile))
            {
                throw new SpmiException("file missing: " + s_baseMchFile);
            }

            if (!Global.SkipCleanup)
            {
                // All the individual MC files are no longer necessary, now that we've merged them into the base.mch. Delete them.
                string[] mcFiles = Directory.GetFiles(s_tempDir, "*.mc");
                foreach (string mcFile in mcFiles)
                {
                    SafeFileDelete(mcFile);
                }
            }
        }

        // Create clean MCH file:
        //      <superPmiPath> -p -f <s_baseFailMclFile> <s_baseMchFile> <jitPath>
        //      if <s_baseFailMclFile> is non-empty:
        //           <mcl> -strip <s_baseFailMclFile> <s_baseMchFile> <s_cleanMchFile>
        //      else:
        //           s_cleanMchFile = s_baseMchFile // no need to copy; just change string names (and null out s_baseMchFile so we don't try to delete twice)
        //      del <s_baseFailMclFile>
        private static void CreateCleanMCHFile()
        {
            RunProgram(Global.SuperPmiPath, "-p -f " + s_baseFailMclFile + " " + s_baseMchFile + " " + Global.JitPath);

            if (File.Exists(s_baseFailMclFile) && !String.IsNullOrEmpty(File.ReadAllText(s_baseFailMclFile)))
            {
                RunProgram(Global.McsPath, "-strip " + s_baseMchFile + " " + s_cleanMchFile);
            }
            else
            {
                // Instead of stripping the file, just set s_cleanMchFile = s_baseMchFile and
                // null out s_baseMchFile so we don't try to delete the same file twice.
                // Note that we never use s_baseMchFile after this function is called.

                s_cleanMchFile = s_baseMchFile;
                s_baseMchFile = null;
            }

            if (!File.Exists(s_cleanMchFile))
            {
                throw new SpmiException("file missing: " + s_cleanMchFile);
            }

            if (!Global.SkipCleanup)
            {
                if (File.Exists(s_baseFailMclFile))
                {
                    SafeFileDelete(s_baseFailMclFile);
                    s_baseFailMclFile = null;
                }

                // The base file is no longer used (unless there was no cleaning done, in which case
                // s_baseMchFile has been null-ed and s_cleanMchFile points at the base file).
                if ((s_baseMchFile != null) && File.Exists(s_baseMchFile))
                {
                    SafeFileDelete(s_baseMchFile);
                    s_baseMchFile = null;
                }
            }
        }

        // Create a thin unique MCH:
        //      <mcl> -removeDup -thin <s_cleanMchFile> <s_finalMchFile>
        private static void CreateThinUniqueMCH()
        {
            RunProgram(Global.McsPath, "-removeDup -thin " + s_cleanMchFile + " " + s_finalMchFile);

            if (!File.Exists(s_finalMchFile))
            {
                throw new SpmiException("file missing: " + s_finalMchFile);
            }

            if (!Global.SkipCleanup)
            {
                // The clean file is no longer used; delete it.
                if ((s_cleanMchFile != null) && File.Exists(s_cleanMchFile))
                {
                    SafeFileDelete(s_cleanMchFile);
                    s_cleanMchFile = null;
                }
            }
        }

        // Create a TOC file:
        //      <mcl> -toc <s_finalMchFile>
        //      // check that .mct file was created
        private static void CreateTOC()
        {
            RunProgram(Global.McsPath, "-toc " + s_finalMchFile);

            if (!File.Exists(s_tocFile))
            {
                throw new SpmiException("file missing: " + s_tocFile);
            }
        }

        // Verify the resulting MCH file is error-free when running superpmi against it with the same JIT used for collection.
        //      <superPmiPath> -p -f <s_finalFailMclFile> <s_finalMchFile> <jitPath>
        //      if <s_finalFailMclFile> is non-empty:
        //           // error!
        private static void VerifyFinalMCH()
        {
            RunProgram(Global.SuperPmiPath, "-p -f " + s_finalFailMclFile + " " + s_finalMchFile + " " + Global.JitPath);

            if (!File.Exists(s_finalFailMclFile) || !String.IsNullOrEmpty(File.ReadAllText(s_finalFailMclFile)))
            {
                throw new SpmiException("replay of final file is not error free");
            }

            if (!Global.SkipCleanup)
            {
                if (File.Exists(s_finalFailMclFile))
                {
                    SafeFileDelete(s_finalFailMclFile);
                    s_finalFailMclFile = null;
                }
            }
        }

        // Cleanup. If we get here due to a failure of some kind, we want to do full cleanup. If we get here as part
        // of normal shutdown processing, we want to keep the s_finalMchFile and s_tocFile if s_saveFinalMchFile == true.
        //      del <s_baseMchFile>
        //      del <s_cleanMchFile>
        //      del <s_finalMchFile>
        //      del <s_tocFile>
        //      rmdir <s_tempDir>
        private static void Cleanup()
        {
            if (Global.SkipCleanup)
                return;

            try
            {
                if ((s_baseFailMclFile != null) && File.Exists(s_baseFailMclFile))
                {
                    SafeFileDelete(s_baseFailMclFile);
                    s_baseFailMclFile = null;
                }

                if ((s_baseMchFile != null) && File.Exists(s_baseMchFile))
                {
                    SafeFileDelete(s_baseMchFile);
                    s_baseMchFile = null;
                }
                if ((s_cleanMchFile != null) && File.Exists(s_cleanMchFile))
                {
                    SafeFileDelete(s_cleanMchFile);
                    s_cleanMchFile = null;
                }

                if (!s_saveFinalMchFile)
                {
                    // Note that if we fail to create the TOC, but we already
                    // successfully created the MCH file, and the user wants to
                    // keep the final result, then we will still keep the final
                    // MCH file. We'll also keep it if the verify pass fails.

                    if ((s_finalMchFile != null) && File.Exists(s_finalMchFile))
                    {
                        SafeFileDelete(s_finalMchFile);
                    }
                    if ((s_tocFile != null) && File.Exists(s_tocFile))
                    {
                        SafeFileDelete(s_tocFile);
                    }
                }

                if ((s_finalFailMclFile != null) && File.Exists(s_finalFailMclFile))
                {
                    SafeFileDelete(s_finalFailMclFile);
                    s_finalFailMclFile = null;
                }

                if ((s_tempDir != null) && Directory.Exists(s_tempDir))
                {
                    Directory.Delete(s_tempDir, /* delete recursively */ true);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("ERROR during cleanup: " + ex.Message);
            }
        }

        public static int Collect(string outputMchPath, string runProgramPath, string runProgramArguments, string tempPath)
        {
            // Do a basic SuperPMI collect and validation:
            // 1. Collect MC files by running a set of sample apps.
            // 2. Merge the MC files into a single MCH using "mcs -merge *.mc -recursive".
            // 3. Create a clean MCH by running superpmi over the MCH, and using "mcs -strip" to filter
            //    out any failures (if any).
            // 4. Create a thin unique MCH by using "mcs -removeDup -thin".
            // 5. Create a TOC using "mcs -toc".
            // 6. Verify the resulting MCH file is error-free when running superpmi against it with the
            //    same JIT used for collection.
            //
            // MCH files are big. If we don't need them anymore, clean them up right away to avoid
            // running out of disk space in disk constrained situations.

            string thisTask = "SuperPMI collection and playback";
            Console.WriteLine(thisTask + " - BEGIN");

            int result = 101;           // assume error (!= 100)

            try
            {
                CreateTempDirectory(tempPath);
                ChooseFilePaths(outputMchPath);
                CollectMCFiles(runProgramPath, runProgramArguments);
                MergeMCFiles();
                CreateCleanMCHFile();
                CreateThinUniqueMCH();
                CreateTOC();
                VerifyFinalMCH();

                // Success!
                result = 100;
            }
            catch (SpmiException ex)
            {
                Console.Error.WriteLine("ERROR: " + ex.Message);
                result = 101;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("ERROR: unknown exception running collection: " + ex.Message);
                result = 101;
            }
            finally
            {
                Cleanup();
            }

            // Re-display the file delete errors, if any, in case they got lost in the output so far.
            if (!String.IsNullOrEmpty(s_errors))
            {
                Console.Error.WriteLine("Non-fatal errors occurred during processing:");
                Console.Error.Write(s_errors);
            }

            if (result == 100)
            {
                Console.WriteLine(thisTask + " - SUCCESS");
            }
            else
            {
                Console.WriteLine(thisTask + " - FAILED");
            }

            return result;
        }
    }

    internal class Program
    {
        private static void Usage()
        {
            // Unfortunately, under CoreCLR, this just gets is the path to CoreRun.exe:
            // string thisProgram = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

            string thisProgram = "superpmicollect";

            Console.WriteLine("Usage: {0} [arguments]", thisProgram);
            Console.WriteLine("  where [arguments] is zero or more of:");
            Console.WriteLine("  -? | -help                     : Display this help text.");
            Console.WriteLine("  -mch <file>                    : Specify the name of the generated clean/thin/unique MCH file.");
            Console.WriteLine("                                   The MCH file is retained (by default, the final MCH file is deleted).");
            Console.WriteLine("  -run <program> [arguments...]  : This program (or script) is invoked to run any number");
            Console.WriteLine("                                   of programs during MC collection. All arguments after");
            Console.WriteLine("                                   <program> are passed to <program> as its arguments.");
            Console.WriteLine("                                   Thus, -run must be the last argument.");
            Console.WriteLine("  -skipCleanup                   : Do not delete any intermediate files created during processing.");
            Console.WriteLine("  -temp <dir>                    : A newly created, randomly-named, subdirectory of this");
            Console.WriteLine("                                   directory will be used to store all temporary files.");
            Console.WriteLine("                                   By default, the user temporary directory is used");
            Console.WriteLine("                                   (%TEMP% on Windows, /tmp on Unix).");
            Console.WriteLine("                                   Since SuperPMI collections generate a lot of data, this option");
            Console.WriteLine("                                   is useful if the normal temporary directory doesn't have enough space.");
            Console.WriteLine("");
            Console.WriteLine("This program performs a collection of SuperPMI data. With no arguments, a hard-coded list of");
            Console.WriteLine("programs are run during collection. With the -run argument, the user species which apps are run.");
            Console.WriteLine("");
            Console.WriteLine("If -mch is not given, all generated files are deleted, and the result is simply the exit code");
            Console.WriteLine("indicating whether the collection succeeded. This is useful as a test.");
            Console.WriteLine("");
            Console.WriteLine("If the COMPlus_AltJit variable is already set, it is assumed SuperPMI collection is already happening,");
            Console.WriteLine("and the program exits with success.");
            Console.WriteLine("");
            Console.WriteLine("On success, the return code is 100.");
        }

        private static int Main(string[] args)
        {
            string outputMchPath = null;
            string runProgramPath = null;
            string runProgramArguments = null;
            string tempPath = null;

            // Parse arguments
            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        default:
                            Usage();
                            return 101;

                        case "-?":
                            Usage();
                            return 101;

                        case "-help":
                            Usage();
                            return 101;

                        case "-skipCleanup":
                            Global.SkipCleanup = true;
                            break;

                        case "-mch":
                            i++;
                            if (i >= args.Length)
                            {
                                Console.Error.WriteLine("Error: missing argument to -mch");
                                Usage();
                                return 101;
                            }
                            outputMchPath = args[i];
                            if (!outputMchPath.EndsWith(".mch"))
                            {
                                // We need the resulting file to end with ".mch". If the user didn't specify this, then simply add it.
                                // Thus, if the user specifies "-mch foo", we'll generate foo.mch (and TOC file foo.mch.mct).
                                outputMchPath += ".mch";
                            }
                            outputMchPath = Path.GetFullPath(outputMchPath);
                            break;

                        case "-run":
                            i++;
                            if (i >= args.Length)
                            {
                                Console.Error.WriteLine("Error: missing argument to -run");
                                Usage();
                                return 101;
                            }
                            runProgramPath = Path.GetFullPath(args[i]);
                            if (!File.Exists(runProgramPath))
                            {
                                Console.Error.WriteLine("Error: couldn't find program {0}", runProgramPath);
                                return 101;
                            }
                            // The rest of the arguments, if any, are passed as arguments to the run program.
                            i++;
                            if (i < args.Length)
                            {
                                string[] runArgumentsArray = new string[args.Length - i];
                                for (int j = 0; i < args.Length; i++, j++)
                                {
                                    runArgumentsArray[j] = args[i];
                                }
                                runProgramArguments = string.Join(" ", runArgumentsArray);
                            }
                            break;

                        case "-temp":
                            i++;
                            if (i >= args.Length)
                            {
                                Console.Error.WriteLine("Error: missing argument to -temp");
                                Usage();
                                return 101;
                            }
                            tempPath = args[i];
                            break;
                    }
                }
            }

            // Done with argument parsing.

            string altjitvar = System.Environment.GetEnvironmentVariable("COMPlus_AltJit");
            if (!String.IsNullOrEmpty(altjitvar))
            {
                // Someone already has the COMPlus_AltJit variable set. We don't want to override
                // that. Perhaps someone is already doing a SuperPMI collection and invokes this
                // program as part of a full test path in which this program exists.

                Console.WriteLine("COMPlus_AltJit already exists: skipping SuperPMI collection and returning success");
                return 100;
            }

            int result;

            try
            {
                Global.Initialize();
                result = SuperPMICollectionClass.Collect(outputMchPath, runProgramPath, runProgramArguments, tempPath);
            }
            catch (SpmiException ex)
            {
                Console.Error.WriteLine("ERROR: " + ex.Message);
                result = 101;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("ERROR: unknown exception running collection: " + ex.Message);
                result = 101;
            }

            return result;
        }
    }

}
