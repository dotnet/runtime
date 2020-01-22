//
// This is a wrapper used to invoke one of the Mono C#
// compilers with the correct MONO_PATH depending on
// where this command is located.   
//
// This allows us to use MSBuild CscToolPath property to
// point to this directory to use our compiler to drive
// the build and set the MONO_PATH as it is expected to
// be for bootstrap
//
// The MONO_PATH and the compiler are chosen based on the
// directory that hosts the command.
//
// The directory specifies a profile, and the format is:
//     PROFILES-COMPILER
//
// Will request that the COMPILER compiler is used setting
// MONO_PATH to PROFILES.   The PROFILES string can contain
// multiple directories, separated by dashes.
//
// COMPILER is one of:
//    basic             -> class/lib/basic/mcs.exe
//    net_1_1_bootstrap -> class/lib/net_1_1_bootstrap/mcs.exe
//    net_1_1           -> class/lib/net_1_1/mcs.exe
//    net_2_0_bootstrap -> class/lib/net_2_0_bootstrap/gmcs.exe
//    gmcs              -> mcs/gmcs.exe
//    moonlight_bootstrap -> class/lib/moonlight_bootstrap/smcs.exe
//    moonlight_raw       -> class/lib/moonlight_raw/smcs.exe
//
// So for example:
// moonlight_bootstrap-net_2_0-moonlight_bootstrap
//
// Will set MONO_PATH to "%MCS_ROOT%\class\lib\moonlight_bootstrap;%MCS_ROOT\class\lib\net_2_0"
// and run the compiler in %MCS_ROOT%\class\lib\moonlight_bootstrap
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace csc
{
	class Program
	{
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

			var root_mono = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(cmd), "..\\..\\.."));
			if (!File.Exists(Path.Combine(root_mono, "mono\\mini\\mini.c"))) {
				Console.WriteLine("My root is incorrect {0} based on {1}", root_mono, cmd);
				Console.WriteLine("Must be in mono/msvc/scripts/PROFILE/COMMAND.exe");
				return 1;
			}

			p = profile.LastIndexOf ('-');
			if (p == -1){
				Console.Error.WriteLine("The directory holding this executable should be MPATHS-COMPILER, instead it is: {0}", profile);
				return 1;
			}

			var root_mcs = Path.GetFullPath (Path.Combine (root_mono, "..\\mcs"));
			var mono_cmd = root_mono + "\\msvc\\Win32_Debug_eglib\\bin\\mono.exe";

			string compiler = null;
			switch (profile.Substring (p+1)){
			case "basic":
				compiler = root_mcs + "\\class\\lib\\basic\\mcs.exe";
				break;
			case "net_1_1_bootstrap":
				compiler = root_mcs + "\\class\\lib\\net_1_1_bootstrap\\mcs.exe";
				break;
				
			case "net_1_1":
				compiler = root_mcs + "\\class\\lib\\net_1_1\\mcs.exe";
				break;
				
			case "net_2_0_bootstrap":
				compiler = root_mcs + "\\class\\lib\\net_2_0_bootstrap\\gmcs.exe";
				break;
				
			case "gmcs":
			case "mcs":
				compiler = root_mcs + "\\mcs\\gmcs.exe";
				break;
				
			case "moonlight_bootstrap":
				compiler = root_mcs + "\\class\\lib\\moonlight_bootstrap\\smcs.exe";
				break;
				
			case "moonlight_raw":
				compiler = root_mcs + "\\class\\lib\\moonlight_raw\\smcs.exe";
				break;

			default:
				Console.WriteLine ("Unknown compiler configuration: {0}", profile.Substring (p+1));
				return 1;

			}
			var paths = profile.Substring (0, p).Split (new char [] { '-' });
			StringBuilder sb = new StringBuilder ();
			foreach (string dir in paths){
				if (sb.Length != 0)
					sb.Append (";");
				sb.Append (root_mcs + "\\class\\lib\\" + dir);
			}
			Environment.SetEnvironmentVariable ("MONO_PATH", sb.ToString ());

			Console.WriteLine ("Compiler: {0}", compiler);
			Console.WriteLine ("MONO_PATH: {0}", sb.ToString ());
			var pi = new ProcessStartInfo() {
				FileName = mono_cmd,
				WindowStyle = ProcessWindowStyle.Hidden,
				Arguments = compiler + " " + String.Join (" ", args),
				UseShellExecute = false
			};

            try {
                var proc = Process.Start (pi);

                proc.WaitForExit ();
                return proc.ExitCode;
            } catch (System.ComponentModel.Win32Exception){
                Console.Error.WriteLine ("Chances are, it did not find {0}", mono_cmd);
                throw;

            }
		}
	}
}
