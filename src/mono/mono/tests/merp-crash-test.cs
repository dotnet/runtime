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
		public struct Crasher {
			public string Name {get;}
			public Action Action {get; }

			public Action<object> Validator {get; }

			public Crasher (string name, Action action, Action<object> validator = null)
			{
				Name = name;
				Action = action;
				Validator = validator;
			}
		}

		public class ValidationException : Exception {
			public ValidationException () : base () {}
			public ValidationException (string msg) : base (msg) {}
			public ValidationException (string msg, Exception inner) : base (msg, inner) {}
		}

		public static List<Crasher> Crashers;
		public static int StresserIndex;

		static CrasherClass ()
		{
			Crashers = new List<Crasher> ();

			// Basic functionality
			Crashers.Add(new Crasher ("MerpCrashManaged", MerpCrashManaged));
			//  Run this test for stress tests
			//
			//  I've ran a burn-in with all of them of
			//  1,000 - 10,000 runs already.
			//
			//  Feel free to change by moving this line.
			StresserIndex = Crashers.Count - 1;

			Crashers.Add(new Crasher ("MerpCrashMalloc", MerpCrashMalloc));
			Crashers.Add(new Crasher ("MerpCrashFailFast", MerpCrashFailFast, ValidateFailFastMsg));

			Crashers.Add(new Crasher ("MerpCrashNullFp", MerpCrashNullFp));
			Crashers.Add(new Crasher ("MerpCrashExceptionHook", MerpCrashUnhandledExceptionHook));

			// Specific Edge Cases
			Crashers.Add(new Crasher ("MerpCrashDladdr", MerpCrashDladdr));
			Crashers.Add(new Crasher ("MerpCrashSnprintf", MerpCrashSnprintf));
			Crashers.Add(new Crasher ("MerpCrashDomainUnload", MerpCrashDomainUnload));
			Crashers.Add(new Crasher ("MerpCrashUnbalancedGCSafe", MerpCrashUnbalancedGCSafe));
			Crashers.Add(new Crasher ("MerpCrashSignalTerm", MerpCrashSignalTerm));
			Crashers.Add(new Crasher ("MerpCrashSignalTerm", MerpCrashSignalAbrt));
			Crashers.Add(new Crasher ("MerpCrashSignalKill", MerpCrashSignalFpe));
			Crashers.Add(new Crasher ("MerpCrashSignalKill", MerpCrashSignalBus));
			Crashers.Add(new Crasher ("MerpCrashSignalSegv", MerpCrashSignalSegv));
			Crashers.Add(new Crasher ("MerpCrashSignalIll", MerpCrashSignalIll));
			Crashers.Add(new Crasher ("MerpCrashTestBreadcrumbs", MerpCrashTestBreadcrumbs, validator: ValidateBreadcrumbs));
		}

		public static void 
		MerpCrashManaged ()
		{
			unsafe { Console.WriteLine("{0}", *(int*) -1); }
		}

		const string failfastMsg = "abcd efgh";

		public static void
		MerpCrashFailFast ()
		{
			Environment.FailFast (failfastMsg);
		}

		public static void ValidateFailFastMsg (object json)
		{
			string s = jsonGetKeys (json, "payload", "failfast_message") as string;
			if (s != failfastMsg)
				throw new ValidationException (String.Format ("incorrect fail fast message (expected: {0}, got: {1})", failfastMsg, s));
		}

		public static void ValidateBreadcrumbs (object json)
		{
			var monoType = Type.GetType ("Mono.Runtime", false);
			var m = monoType.GetMethod ("CheckCrashReportReason", BindingFlags.NonPublic | BindingFlags.Static);
			var m_params = new object [] { "./", false };
			string o = (string)m.Invoke(null, m_params);
			if (o != "segv")
				throw new Exception ("Crash report reason should be 'segv'");

			m = monoType.GetMethod ("CheckCrashReportHash", BindingFlags.NonPublic | BindingFlags.Static);
			long hash = (long)m.Invoke (null, m_params);

			if (hash == 0)
				throw new Exception ("Crash hash should not be zero");
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

		[DllImport("libtest")]
		public static extern void mono_test_MerpCrashSignalTerm ();

		public static void
		MerpCrashSignalTerm ()
		{
			mono_test_MerpCrashSignalTerm ();
		}

		[DllImport("libtest")]
		public static extern void mono_test_MerpCrashSignalAbrt ();

		public static void
		MerpCrashSignalAbrt ()
		{
			mono_test_MerpCrashSignalAbrt ();
		}

		[DllImport("libtest")]
		public static extern void mono_test_MerpCrashSignalFpe ();

		public static void
		MerpCrashSignalFpe ()
		{
			mono_test_MerpCrashSignalFpe ();
		}

		[DllImport("libtest")]
		public static extern void mono_test_MerpCrashSignalBus ();

		public static void
		MerpCrashSignalBus ()
		{
			mono_test_MerpCrashSignalBus ();
		}

		[DllImport("libtest")]
		public static extern void mono_test_MerpCrashSignalSegv ();

		public static void
		MerpCrashSignalSegv ()
		{
			mono_test_MerpCrashSignalSegv ();
		}

		[DllImport("libtest")]
		public static extern void mono_test_MerpCrashSignalIll ();

		public static void
		MerpCrashSignalIll ()
		{
			mono_test_MerpCrashSignalIll ();
		}

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

		public static void
		MerpCrashTestBreadcrumbs ()
		{
			mono_test_MerpCrashSignalSegv ();
		}


		private static object jsonGetKey (object o, string key) => (o as Dictionary<string,object>)[key];
		private static object jsonGetKeys (object o, params string[] keys) {
			try {
				foreach (var key in keys) {
					o = jsonGetKey (o, key);
				}
				return o;
			} catch (KeyNotFoundException e) {
				throw new ValidationException (String.Format ("{0}, key not found, looking for key path [{1}]", e.ToString(), String.Join (", ", keys)));
			}
		}

	}

	static string configDir = "./merp-crash-test/";

	public static void 
	CrashWithMerp (int testNum)
	{
		SetupCrash (configDir);
		CrasherClass.Crashers [Convert.ToInt32 (testNum)].Action ();
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
	TestValidate (string configDir, bool silent, Action<object> validator = null)
	{
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
		} else {
			Console.WriteLine ("Xml file {0} missing", xmlFilePath);
		}

		if (paramsFileExists) {
			var text = File.ReadAllText (paramsFilePath);
			if (!silent)
				Console.WriteLine ("Params file {0}", text);
			File.Delete (paramsFilePath);
		} else {
			Console.WriteLine ("Params file {0} missing", paramsFilePath);
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
				if (validator is object)
					validator (obj);
			} catch (CrasherClass.ValidationException e) {
				throw new Exception (String.Format ("Validation failed '{0}', json: {1}", e.Message, crashFile));
			} catch (Exception e) {
				throw new Exception (String.Format ("Invalid json  ({0}:{1}): {2}", e.GetType(), e.Message, crashFile));
			}

			File.Delete (crashFilePath);
			// Assert it has the required merp fields
		} else {
			Console.WriteLine ("Crash file {0} missing", crashFilePath);
		}

		DumpLogCheck (expected_level: "MerpInvoke"); // we are expecting merp invoke to fail

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
		Directory.Delete (configDir, true);
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

	static void DumpLogCheck (string expected_level = "Done")
	{
		var monoType = Type.GetType ("Mono.Runtime", false);
		var convert = monoType.GetMethod("CheckCrashReportLog", BindingFlags.NonPublic | BindingFlags.Static);
		var result = (int) convert.Invoke(null, new object[] { "./", true });
		// Value of enum
		string [] levels = new string [] { "None", "Setup", "SuspendHandshake", "UnmanagedStacks", "ManagedStacks", "StateWriter", "StateWriterDone", "MerpWriter", "MerpInvoke", "Cleanup", "Done", "DoubleFault" };

		if (expected_level != levels [result])
			throw new Exception (String.Format ("Crash level {0} does not match expected {1}", levels [result], expected_level));

		// also clear hash and reason breadcrumbs
		convert = monoType.GetMethod("CheckCrashReportHash", BindingFlags.NonPublic | BindingFlags.Static);
		var hash_result = (long) convert.Invoke(null, new object[] { "./", true });
		convert = monoType.GetMethod("CheckCrashReportReason", BindingFlags.NonPublic | BindingFlags.Static);
		var reason_result = (string) convert.Invoke(null, new object[] { "./", true });

		if (reason_result == string.Empty)
			throw new Exception("Crash reason should not be an empty string");
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

		if (!silent) {
			Console.WriteLine ("Running {0}", CrasherClass.Crashers [testNum].Name);
			Console.WriteLine ("MONO_PATH={0} {1} {2} {3}", env, runtime, asm, testNum);
		}

		if (Directory.Exists (configDir)) {
			Console.WriteLine ("Cleaning up left over configDir {0}", configDir);
			Cleanup (configDir);
		}

		Directory.CreateDirectory (configDir);

		try {
			var process = Diag.Process.Start (pi);
			process.WaitForExit ();

			TestValidate (configDir, silent, CrasherClass.Crashers [testNum].Validator);
		} finally {
			Cleanup (configDir);
		}
	}

	public static void TestManagedException ()
	{
		if (Directory.Exists (configDir)) {
			Console.WriteLine ("Cleaning up left over configDir {0}", configDir);
			Cleanup (configDir);
		}
		Directory.CreateDirectory (configDir);

		SetupCrash (configDir);
		var monoType = Type.GetType ("Mono.Runtime", false);
		var m = monoType.GetMethod ("ExceptionToState", BindingFlags.NonPublic | BindingFlags.Static);
		var exception = new Exception ("test managed exception");
		var m_params = new object[] { exception };

		var result = m.Invoke (null, m_params) as Tuple<String, ulong, ulong>;
		DumpLogCheck (expected_level: "StateWriterDone");
		Cleanup (configDir);
	}

	public static Exception RunManagedExceptionTest ()
	{
			Console.WriteLine ("Testing ExceptionToState()...");
			Exception exception_test_failure = null;

			try {
				TestManagedException();
			}
			catch (Exception e)
			{
				return e;
			}
			return null;
	}

	public static int Main (string [] args)
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

			// Also test sending a managed exception
			Exception exception_test_failure = RunManagedExceptionTest ();

			Console.WriteLine ("\n\n##################");
			Console.WriteLine ("Merp Test Results:");
			Console.WriteLine ("##################\n\n");

			if (exception_test_failure != null)
			{
				Console.WriteLine ("Sending managed exception to MERP failed: {0}\n{1}\n", exception_test_failure.Message, exception_test_failure.StackTrace);
			}

			if (failure_count > 0) {
				for (int i=0; i < CrasherClass.Crashers.Count; i++) {
					if (failures [i] != null) {
						Console.WriteLine ("Crash reporter failed test {0}", CrasherClass.Crashers [i].Name);
						Console.WriteLine ("Cause: {0}\n{1}\n", failures [i].Message, failures [i].StackTrace);
					}
				}
			}

			if (failure_count > 0 || exception_test_failure != null)
				return 1;

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
					throw;
				}
			}
			Console.WriteLine ("Ending crash stress test. No failures caught.\n");

			return 0;
		} else {
			CrashWithMerp (Convert.ToInt32 (args [0]));
			return 0;
		}
	}
}
