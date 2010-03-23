//
// test-runner.cs
//
// Author:
//   Zoltan Varga (vargaz@gmail.com)
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;

//
// This is a simple test runner with support for parallel execution
//

public class TestRunner
{
	class ProcessData {
		public string test;
		public StreamWriter stdout, stderr;
	}

	class TestInfo {
		public string test, opt_set;
	}

	public static int Main (String[] args) {
		// Defaults
		int concurrency = 1;
		int timeout = 2 * 60; // in seconds

		// FIXME: Add support for runtime arguments + env variables

		string disabled_tests = null;
		string runtime = "mono";
		var opt_sets = new List<string> ();

		// Process options
		int i = 0;
		while (i < args.Length) {
			if (args [i].StartsWith ("-")) {
				if (args [i] == "-j") {
					if (i + i >= args.Length) {
						Console.WriteLine ("Missing argument to -j command line option.");
						return 1;
					}
					if (args [i + 1] == "a")
						concurrency = Environment.ProcessorCount;
					else
						concurrency = Int32.Parse (args [i + 1]);
					i += 2;
				} else if (args [i] == "--timeout") {
					if (i + i >= args.Length) {
						Console.WriteLine ("Missing argument to --timeout command line option.");
						return 1;
					}
					timeout = Int32.Parse (args [i + 1]);
					i += 2;
				} else if (args [i] == "--disabled") {
					if (i + i >= args.Length) {
						Console.WriteLine ("Missing argument to --disabled command line option.");
						return 1;
					}
					disabled_tests = args [i + 1];
					i += 2;
				} else if (args [i] == "--runtime") {
					if (i + i >= args.Length) {
						Console.WriteLine ("Missing argument to --runtime command line option.");
						return 1;
					}
					runtime = args [i + 1];
					i += 2;
				} else if (args [i] == "--opt-sets") {
					if (i + i >= args.Length) {
						Console.WriteLine ("Missing argument to --opt-sets command line option.");
						return 1;
					}
					foreach (var s in args [i + 1].Split ())
						opt_sets.Add (s);
					i += 2;
				} else {
					Console.WriteLine ("Unknown command line option: '" + args [i] + "'.");
					return 1;
				}
			} else {
				break;
			}
		}

		var disabled = new Dictionary <string, string> ();

		if (disabled_tests != null) {
			foreach (string test in disabled_tests.Split ())
				disabled [test] = test;
		}

		// The remaining arguments are the tests
		var tests = new List<string> ();
		for (int j = i; j < args.Length; ++j)
			if (!disabled.ContainsKey (args [j]))
				tests.Add (args [j]);

		int npassed = 0;
		int nfailed = 0;

		var processes = new List<Process> ();
		var failed = new List<string> ();
		var process_data = new Dictionary<Process, ProcessData> ();

		object monitor = new object ();

		var terminated = new List<Process> ();

		if (concurrency != 1)
			Console.WriteLine ("Running tests: ");

		var test_info = new List<TestInfo> ();
		if (opt_sets.Count == 0) {
			foreach (string s in tests)
				test_info.Add (new TestInfo { test = s });
		} else {
			foreach (string opt in opt_sets) {
				foreach (string s in tests)
					test_info.Add (new TestInfo { test = s, opt_set = opt });
			}
		}		

		foreach (TestInfo ti in test_info) {
			lock (monitor) {
				while (processes.Count == concurrency) {
					/* Wait for one process to terminate */
					Monitor.Wait (monitor);
				}

				/* Cleaup terminated processes */
				foreach (Process dead in terminated) {
					if (process_data [dead].stdout != null)
						process_data [dead].stdout.Close ();
					if (process_data [dead].stderr != null)
						process_data [dead].stderr.Close ();
					// This is needed to avoid CreateProcess failed errors :(
					dead.Close ();
				}
				terminated.Clear ();
			}

			string test = ti.test;
			string opt_set = ti.opt_set;

			if (concurrency == 1)
				Console.Write ("Testing " + test + "... ");

			/* Spawn a new process */
			string process_args;
			if (opt_set == null)
				process_args = test;
			else
				process_args = "-O=" + opt_set + " " + test;
			ProcessStartInfo info = new ProcessStartInfo (runtime, process_args);
			info.UseShellExecute = false;
			info.RedirectStandardOutput = true;
			info.RedirectStandardError = true;
			Process p = new Process ();
			p.StartInfo = info;
			p.EnableRaisingEvents = true;

			ProcessData data = new ProcessData ();
			data.test = test;

			p.Exited += delegate (object sender, EventArgs e) {
				// Anon methods share some of their state, so we can't use
				// variables which change during the loop (test, p)
				Process dead = (Process)sender;

				lock (monitor) {
					if (dead.ExitCode == 0) {
						if (concurrency == 1)
							Console.WriteLine ("passed.");
						else
							Console.Write (".");
						npassed ++;
					} else {
						if (concurrency == 1)
							Console.WriteLine ("failed.");
						else
							Console.Write ("F");
						failed.Add (process_data [dead].test);
						nfailed ++;
					}
					processes.Remove (dead);
					terminated.Add (dead);
					Monitor.Pulse (monitor);
				}
			};

			string log_prefix = "";
			if (opt_set != null)
				log_prefix = "." + opt_set.Replace ("-", "no").Replace (",", "_");

			data.stdout = new StreamWriter (new FileStream (test + log_prefix + ".stdout", FileMode.Create));

			data.stderr = new StreamWriter (new FileStream (test + log_prefix + ".stderr", FileMode.Create));

			p.OutputDataReceived += delegate (object sender, DataReceivedEventArgs e) {
				Process p2 = (Process)sender;

				StreamWriter fs;

				lock (monitor) {
					fs = process_data [p2].stdout;

					if (String.IsNullOrEmpty (e.Data))
						process_data [p2].stdout = null;
				}

				if (String.IsNullOrEmpty (e.Data))
					fs.Close ();
				else
					fs.WriteLine (e.Data);
			};

			p.ErrorDataReceived += delegate (object sender, DataReceivedEventArgs e) {
				Process p2 = (Process)sender;

				StreamWriter fs;

				lock (monitor) {
					fs = process_data [p2].stderr;

					if (String.IsNullOrEmpty (e.Data))
						process_data [p2].stderr = null;

				}

				if (String.IsNullOrEmpty (e.Data)) {
					fs.Close ();

					lock (monitor) {
						process_data [p2].stderr = null;
					}
				}
				else
					fs.WriteLine (e.Data);
			};

			lock (monitor) {
				processes.Add (p);
				process_data [p] = data;
			}
			p.Start ();

			p.BeginOutputReadLine ();
			p.BeginErrorReadLine ();
		}

		bool timed_out = false;

		/* Wait for all processes to terminate */
		while (true) {
			lock (monitor) {
				int nprocesses = processes.Count;

				if (nprocesses == 0)
					break;

				bool res = Monitor.Wait (monitor, 1000 * timeout);
				if (!res) {
					timed_out = true;
					break;
				}
			}
		}

		Console.WriteLine ();

		if (timed_out) {
			Console.WriteLine ("\nrunning tests timed out:\n");
			Console.WriteLine (npassed + nfailed);
			lock (monitor) {
				foreach (Process p in processes) {
					Console.WriteLine (process_data [p].test);
				}
			}
			return 1;
		}

		Console.WriteLine ("" + npassed + " test(s) passed. " + nfailed + " test(s) did not pass.");
		if (nfailed > 0) {
			Console.WriteLine ("\nFailed tests:\n");
			foreach (string s in failed)
				Console.WriteLine (s);
			return 1;
		} else {
			return 0;
		}
	}
}
