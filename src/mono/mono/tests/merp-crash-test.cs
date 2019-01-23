using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Web.Script.Serialization;
using Diag = System.Diagnostics;
using System.Runtime.InteropServices;

class C
{
	class CrasherClass
	{
		public static List<Tuple<String, Action>> Crashers;
		public static int StresserIndex;

		static CrasherClass ()
		{
			Crashers = new List<Tuple<String, Action>> ();

			// Basic functionality
			Crashers.Add(new Tuple<String, Action> ("MerpCrashManaged", MerpCrashManaged));
			//  Run this test for stress tests
			//
			//  I've ran a burn-in with all of them of
			//  1,000 - 10,000 runs already.
			//
			//  Feel free to change by moving this line.
			StresserIndex = Crashers.Count - 1;

			Crashers.Add(new Tuple<String, Action> ("MerpCrashMalloc", MerpCrashMalloc));

			Crashers.Add(new Tuple<String, Action> ("MerpCrashNullFp", MerpCrashNullFp));
			Crashers.Add(new Tuple<String, Action> ("MerpCrashExceptionHook", MerpCrashUnhandledExceptionHook));

			// Specific Edge Cases
			Crashers.Add(new Tuple<String, Action> ("MerpCrashDladdr", MerpCrashDladdr));
			Crashers.Add(new Tuple<String, Action> ("MerpCrashSnprintf", MerpCrashSnprintf));
			Crashers.Add(new Tuple<String, Action> ("MerpCrashDomainUnload", MerpCrashDomainUnload));
			Crashers.Add(new Tuple<String, Action> ("MerpCrashUnbalancedGCSafe", MerpCrashUnbalancedGCSafe));
		}

		public static void 
		MerpCrashManaged ()
		{
			unsafe { Console.WriteLine("{0}", *(int*) -1); }
		}

		[DllImport("libtest")]
		public static extern void mono_test_MerpCrashSnprintf ();

		// This test tries to test the writer's reentrancy
		public static void 
		MerpCrashSnprintf ()
		{
			mono_test_MerpCrashSnprintf ();
		}

		[DllImport("libtest")]
		public static extern void mono_test_MerpCrashDladdr ();

		public static void 
		MerpCrashDladdr ()
		{
			mono_test_MerpCrashDladdr ();
		}

		[DllImport("libtest")]
		public static extern void mono_test_MerpCrashMalloc ();

		public static void 
		MerpCrashMalloc ()
		{
			mono_test_MerpCrashMalloc ();
		}

		[DllImport("libtest")]
		public static extern void mono_test_MerpCrashLoaderLock ();

		public static void 
		MerpCrashLoaderLock ()
		{
			mono_test_MerpCrashLoaderLock ();
		}

		[DllImport("libtest")]
		public static extern void mono_test_MerpCrashDomainUnload ();

		public static void 
		MerpCrashDomainUnload ()
		{
			mono_test_MerpCrashDomainUnload ();
		}

		[DllImport("libtest")]
		public static extern void mono_test_MerpCrashUnbalancedGCSafe ();

		public static void 
		MerpCrashUnbalancedGCSafe ()
		{
			mono_test_MerpCrashUnbalancedGCSafe ();
		}

		[DllImport("libtest")]
		public static extern void mono_test_MerpCrashNullFp ();

		public static void 
		MerpCrashNullFp ()
		{
			mono_test_MerpCrashNullFp ();
		}

		[DllImport("libtest")]
		public static extern void mono_test_MerpCrashUnhandledExceptionHook ();

		public static void 
		MerpCrashUnhandledExceptionHook ()
		{
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(HandleException);
			throw new Exception ("This is Unhandled");
		}

		public static void HandleException (object sender, UnhandledExceptionEventArgs e)
		{
			Console.WriteLine ("And now to crash inside the hook");
			mono_test_MerpCrashUnhandledExceptionHook ();
		}
	}

	static string configDir = "./";

	public static void 
	CrashWithMerp (int testNum)
	{
		SetupCrash (configDir);
		CrasherClass.Crashers [Convert.ToInt32 (testNum)].Item2 ();
	}

	public static string env = Environment.GetEnvironmentVariable ("MONO_PATH");
	public static string this_assembly_path = Path.GetDirectoryName (Assembly.GetExecutingAssembly ().Location);

	public static void 
	SetupCrash (string configDir)
	{
		var monoType = Type.GetType ("Mono.Runtime", false);
		var m = monoType.GetMethod("EnableMicrosoftTelemetry", BindingFlags.NonPublic | BindingFlags.Static);

		// This leads to open -a /bin/cat, which errors out, but errors
		// in invoking merp are only logged errors, not fatal assertions.
		var merpGUIPath = "/bin/cat";
		var appBundleId = "com.xam.Minimal";
		var appSignature = "Test.Xam.Minimal";
		var appVersion = "123456";
		var eventType = "AppleAppCrash";
		var appPath = "/where/mono/lives";
		var m_params = new object[] { appBundleId, appSignature, appVersion, merpGUIPath, eventType, appPath, configDir };

		m.Invoke(null, m_params);	

		DumpLogSet ();
	}

	public static void 
	TestValidateAndCleanup (string configDir, bool silent)
	{
		DumpLogCheck ();

		var xmlFilePath = String.Format("{0}CustomLogsMetadata.xml", configDir);
		var paramsFilePath = String.Format("{0}MERP.uploadparams.txt", configDir);
		var crashFilePath = String.Format("{0}lastcrashlog.txt", configDir);

		// Fixme: Maybe parse these json files rather than
		// just checking they exist
		var xmlFileExists = File.Exists (xmlFilePath);
		var paramsFileExists = File.Exists (paramsFilePath);
		var crashFileExists = File.Exists (crashFilePath);

		if (xmlFileExists) {
			var text = File.ReadAllText (xmlFilePath);
			if (!silent)
				Console.WriteLine ("Xml file {0}", text);
			File.Delete (xmlFilePath);
		}

		if (paramsFileExists) {
			var text = File.ReadAllText (paramsFilePath);
			if (!silent)
				Console.WriteLine ("Params file {0}", text);
			File.Delete (paramsFilePath);
		}

		if (crashFileExists) {
			var crashFile = File.ReadAllText (crashFilePath);
			File.Delete (crashFilePath);

			var checker = new JavaScriptSerializer ();

			// Throws if invalid json
			if (!silent)
				Console.WriteLine("Validating: {0}",  crashFile);
			try {
				var obj = checker.DeserializeObject (crashFile);
			} catch (Exception e) {
				throw new Exception (String.Format ("Invalid json: {0}", crashFile));
			}

			File.Delete (crashFilePath);
			// Assert it has the required merp fields
		}

		if (!xmlFileExists)
			throw new Exception (String.Format ("Did not produce {0}", xmlFilePath));

		if (!paramsFileExists)
			throw new Exception (String.Format ("Did not produce {0}", paramsFilePath));

		if (!crashFileExists)
			throw new Exception (String.Format ("Did not produce {0}", crashFilePath));
	}

	public static void
	Cleanup (string configDir)
	{
		var xmlFilePath = String.Format("{0}CustomLogsMetadata.xml", configDir);
		var paramsFilePath = String.Format("{0}MERP.uploadparams.txt", configDir);
		var crashFilePath = String.Format("{0}lastcrashlog.txt", configDir);

		// Fixme: Maybe parse these json files rather than
		// just checking they exist
		var xmlFileExists = File.Exists (xmlFilePath);
		var paramsFileExists = File.Exists (paramsFilePath);
		var crashFileExists = File.Exists (crashFilePath);

		if (xmlFileExists)
			File.Delete (xmlFilePath);

		if (paramsFileExists)
			File.Delete (paramsFilePath);

		if (crashFileExists)
			File.Delete (crashFilePath);
	}

	static void DumpLogSet ()
	{
		var monoType = Type.GetType ("Mono.Runtime", false);
		var convert = monoType.GetMethod("EnableCrashReportLog", BindingFlags.NonPublic | BindingFlags.Static);
		convert.Invoke(null, new object[] { "./" });
	}

	static void DumpLogUnset ()
	{
		var monoType = Type.GetType ("Mono.Runtime", false);
		var convert = monoType.GetMethod("EnableCrashReportLog", BindingFlags.NonPublic | BindingFlags.Static);
		convert.Invoke(null, new object[] { null });
	}

	static void DumpLogCheck ()
	{
		var monoType = Type.GetType ("Mono.Runtime", false);
		var convert = monoType.GetMethod("CheckCrashReportLog", BindingFlags.NonPublic | BindingFlags.Static);
		var result = (int) convert.Invoke(null, new object[] { "./", true });
		// Value of enum
		string [] levels = new string [] { "None", "Setup", "SuspendHandshake", "UnmanagedStacks", "ManagedStacks", "StateWriter", "StateWriterDone", "MerpWriter", "MerpInvoke", "Cleanup", "Done", "DoubleFault" };

		if ("MerpInvoke" == levels [result]) {
			Console.WriteLine ("Merp invoke command failed, expected failure?");
		} else if ("Done" != levels [result]) {
			throw new Exception (String.Format ("Crash level not done, failed in stage: {0}", levels [result]));
		}
	}


	public static void 
	SpawnCrashingRuntime (string runtime, int testNum, bool silent)
	{
		var asm = "merp-crash-test.exe";
		var pi = new Diag.ProcessStartInfo ();
		pi.UseShellExecute = false;
		pi.FileName = runtime;
		pi.Arguments = String.Format ("{0} {1}", asm, testNum);;
		pi.Environment ["MONO_PATH"] = env;

		if (!silent)
			Console.WriteLine ("MONO_PATH={0} {1} {2} {3}", env, runtime, asm, testNum);

		var process = Diag.Process.Start (pi);
		process.WaitForExit ();

		TestValidateAndCleanup (configDir, silent);
	}

	public static void Main (string [] args)
	{
		if (args.Length == 0) {
			string processExe = Diag.Process.GetCurrentProcess ().MainModule.FileName;
			if (processExe == null)
				throw new ArgumentException ("Couldn't get name of running file");
			else if (string.IsNullOrEmpty (processExe))
				throw new ArgumentException ("Couldn't find mono runtime.");
			else if (!Path.GetFileName (processExe).StartsWith ("mono"))
				throw new ArgumentException (String.Format("Running native app {0}  isn't 'mono'"));

			var failures = new Exception [CrasherClass.Crashers.Count];
			int failure_count = 0;
			for (int i=0; i < CrasherClass.Crashers.Count; i++) {
				try {
					SpawnCrashingRuntime (processExe, i, false);
				} catch (Exception e) {
					failures [i] = e;
					if (e.InnerException != null)
						failures [i] = e.InnerException;
					failure_count++;
				}
			}

			Console.WriteLine ("\n\n##################");
			Console.WriteLine ("Merp Test Results:");
			Console.WriteLine ("##################\n\n");

			if (failure_count > 0) {
				for (int i=0; i < CrasherClass.Crashers.Count; i++) {
					if (failures [i] != null) {
						Console.WriteLine ("Crash reporter failed test {0}", CrasherClass.Crashers [i].Item1);
						Console.WriteLine ("Cause: {0}\n{1}\n", failures [i].Message, failures [i].StackTrace);
					}
				}
			}

			if (failure_count > 0)
				return;

			Console.WriteLine ("\n\n##################");
			Console.WriteLine ("Merp Stress Test:");
			Console.WriteLine ("##################\n\n");

			Console.WriteLine ("Starting crash stress test\n");
			int iter = 0;
			for (iter=0; iter < 20; iter++) {
				Console.WriteLine ("\n#############################################");
				Console.WriteLine ("\tMerp Stress Test Iteration {0}", iter);
				Console.WriteLine ("#############################################\n");
				try {
					SpawnCrashingRuntime (processExe, CrasherClass.StresserIndex, true);
				} catch (Exception e) {
					Console.WriteLine ("Stress test caught failure. Shutting down after {1} iterations.\n {0} \n\n", e.InnerException, iter);
					Cleanup (configDir);
					throw;
				}
			}
			Console.WriteLine ("Ending crash stress test. No failures caught.\n");

			return;
		} else {
			CrashWithMerp (Convert.ToInt32 (args [0]));
		}
	}
}
