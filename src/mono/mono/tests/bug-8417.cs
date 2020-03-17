#define USE_REDIRECT
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;

namespace Example
{
	public static class EntryPoint 
	{
		public static bool RunProcess (string filename, string arguments, out int exitCode, out string stdout, bool capture_stderr = false)
		{
			var sb = new StringBuilder ();
			var stdout_done = new System.Threading.ManualResetEvent (false);
			var stderr_done = new System.Threading.ManualResetEvent (false);
			using (var p = new Process ()) {
				p.StartInfo.FileName = filename;
				p.StartInfo.Arguments = arguments;
				p.StartInfo.UseShellExecute = false;
				p.StartInfo.RedirectStandardOutput = true;
				p.StartInfo.RedirectStandardError = capture_stderr;

				p.OutputDataReceived += (sender, e) => {
					if (e.Data == null) {
						stdout_done.Set ();
					}
					else {
						lock (sb)
							sb.AppendLine (e.Data);
					}
				};
				if (capture_stderr) {
					p.ErrorDataReceived += (sender, e) => {
						if (e.Data == null) {
							stderr_done.Set ();
						}
						else {
							lock (sb)
								sb.AppendLine (e.Data);
						}
					};
				}
				p.Start ();
				p.BeginOutputReadLine ();
				if (capture_stderr)
					p.BeginErrorReadLine ();
				p.WaitForExit ();
				stdout_done.WaitOne (TimeSpan.FromSeconds (1));
				if (capture_stderr)
					stderr_done.WaitOne (TimeSpan.FromSeconds (1));
				stdout = sb.ToString ();
				exitCode = p.ExitCode;
				return exitCode == 0;
			}
		}

		static string RunRedirectOutput (Action action)
		{
			var existingOut = Console.Out;
			var existingErr = Console.Error;

			try {
				using (StringWriter writer = new StringWriter ()) {

#if USE_REDIRECT
					Console.SetOut (writer);
					Console.SetError (writer);
#endif
					action ();
					return writer.ToString ();
				}
			}
			finally {
				Console.SetOut (existingOut);
				Console.SetError (existingErr);
			}
		}

		static void RunProcess ()
		{
			RunProcess ("/bin/echo", "who am i", out int exitCode, out string stdout);
			Console.Write (stdout);
		}

		public static int Main ()
		{
			var str = RunRedirectOutput (RunProcess);
			Console.WriteLine ("'{0}'", str);
			return str == "who am i\n" ? 0 : 1;
		}
	}
}