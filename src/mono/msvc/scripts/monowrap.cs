using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace csc
{
    class Program
    {
        //
        // Executes the Mono command with MONO_PATH set to the
        // class/lib/PROFILE where PROFILE is the directory where
        // this executable is hosted, and the tool is the name
        // of this executable
        //
        // This allows us to use MSBuild CscToolPath property to
        // point to this directory to use our compiler to drive
        // the build and set the MONO_PATH as it is expected to
        // be for bootstrap
        //
        static int Main(string[] args)
        {
            string cmd = Environment.GetCommandLineArgs () [0];
            string tool = Path.GetFileName(cmd);
            string profile = Path.GetDirectoryName(cmd);
            int p = profile.LastIndexOf('\\');
            if (p == -1) {
                Console.Error.WriteLine("Could not find the profile name from this {0}", profile);
                return 1;
            }
            profile = profile.Substring(p+1);

	    if (tool.ToLower () == "csc.exe"){
		if (profile.IndexOf ("net_2_0") != -1)
		   tool = "gmcs.exe";
                else if (profile.IndexOf ("net_2_1") != -1)
		   tool = "smcs.exe";
 	 	else
		   tool = "mcs.exe";
	    }

            var root_mono = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(cmd), "..\\..\\.."));
            if (!File.Exists(Path.Combine(root_mono, "mono\\mini\\mini.c"))) {
                Console.WriteLine("My root is incorrect {0} based on {1}", root_mono, cmd);
                Console.WriteLine("Must be in mono/msvc/scripts/PROFILE/COMMAND.exe");
                return 1;
            }
            var root_mcs = Path.GetFullPath (Path.Combine (root_mono, "..\\mcs"));
            var mono_cmd = root_mono + "\\msvc\\Win32_Debug_eglib\\bin\\mono.exe";
            var tool_cmd = root_mcs + "\\class\\lib\\" + profile + "\\" + tool;

            Console.WriteLine("root_mcs={0}\nmono_cmd={1}\ntool_cmd={2}", root_mcs, mono_cmd, tool_cmd);
            Environment.SetEnvironmentVariable ("MONO_PATH", root_mcs + "\\class\\lib\\net_1_1_bootstrap");

            var pi = new ProcessStartInfo() {
                FileName = mono_cmd,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = tool_cmd + " " + String.Join (" ", args),
                UseShellExecute = false
            };

            var proc = Process.Start (pi);

            proc.WaitForExit();
            return proc.ExitCode;
        }
    }
}
