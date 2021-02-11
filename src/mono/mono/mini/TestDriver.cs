using System;
using System.Reflection;
using System.Collections.Generic;

[AttributeUsageAttribute(AttributeTargets.All, Inherited = true, AllowMultiple = true)]
public class CategoryAttribute : Attribute
{
	public CategoryAttribute (string category) {
		Category = category;
	}

	public string Category {
		get; set;
	}
}
public class TestDriverReporter
{
	public int FailedTests { get; private set; }
	public int SkippedTests { get; private set; }
	public int ExecutedTests { get; private set; }

	public void ReportResults (int executed, int skipped, int failed) {
		ExecutedTests = executed;
		SkippedTests = skipped;
		FailedTests = failed;
	}
};

public class TestDriver {

	static public int RunTests(Type type, string[] args, TestDriverReporter reporter) {
		int failed = 0, ran = 0;
		int result, expected;
		int i, j, iterations;
		string name;
		MethodInfo[] methods;
		bool do_timings = false;
		bool verbose = false;
		bool quiet = false;
		int tms = 0;
		DateTime start, end = DateTime.Now;

		iterations = 1;

		var exclude = new Dictionary<string, string> ();
		List<string> run_only = new List<string> ();
		List<string> exclude_test = new List<string> ();
		if (args != null && args.Length > 0) {
			for (j = 0; j < args.Length;) {
				if (args [j] == "--time") {
					do_timings = !quiet;
					j ++;
				} else if (args [j] == "--iter") {
					iterations = Int32.Parse (args [j + 1]);
					j += 2;
				} else if ((args [j] == "-v") || (args [j] == "--verbose")) {
					verbose = !quiet;
					j += 1;
				} else if ((args [j] == "-q") || (args [j] == "--quiet")) {
					quiet = true;
					verbose = false;
					do_timings = false;
					j += 1;
				} else if (args [j] == "--exclude") {
					exclude [args [j + 1]] = args [j + 1];
					j += 2;
				} else if (args [j] == "--exclude-test") {
					exclude_test.Add (args [j + 1]);
					j += 2;
				} else if (args [j] == "--run-only") {
					run_only.Add (args [j + 1]);
					j += 2;
				} else {
					Console.WriteLine ("Unknown argument: " + args [j]);
					return 1;
				}
			}
		}
		int nskipped = 0;
		methods = type.GetMethods (BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);
		for (int iter = 0; iter < iterations; ++iter) {
			for (i = 0; i < methods.Length; ++i) {
				name = methods [i].Name;
				if (!name.StartsWith ("test_", StringComparison.Ordinal))
					continue;
				if (run_only.Count > 0) {
					bool found = false;
					for (j = 0; j < run_only.Count; j++) {
						if (name.EndsWith (run_only [j])) {
							found = true;
							break;
						}
					}
					if (!found)
						continue;
				}
				if (exclude.Count > 0 || exclude_test.Count > 0) {
					var attrs = methods [i].GetCustomAttributes (typeof (CategoryAttribute), false);
					bool skip = false;
					for (j = 0; j < exclude_test.Count; j++) {
						if (name.EndsWith (exclude_test [j])) {
							skip = true;
							break;
						}
					}
					foreach (CategoryAttribute attr in attrs) {
						if (exclude.ContainsKey (attr.Category))
							skip = true;
					}
					if (skip) {
						if (verbose)
							Console.WriteLine ("Skipping '{0}'.", name);
						nskipped ++;
						continue;
					}
				}
				for (j = 5; j < name.Length; ++j)
					if (!Char.IsDigit (name [j]))
						break;
				if (verbose)
					Console.WriteLine ("Running '{0}' ...", name);
				expected = Int32.Parse (name.Substring (5, j - 5));
				start = DateTime.Now;
				result = (int)methods [i].Invoke (null, null);
				if (do_timings) {
					end = DateTime.Now;
					long tdiff = end.Ticks - start.Ticks;
					int mdiff = (int)tdiff/10000;
					tms += mdiff;
					Console.WriteLine ("{0} took {1} ms", name, mdiff);
				}
				ran++;
				if (result != expected) {
					failed++;
					Console.WriteLine ("{0} failed: got {1}, expected {2}", name, result, expected);
				}
			}
		
			if (!quiet) {
				if (do_timings) {
					Console.WriteLine ("Total ms: {0}", tms);
				}
				if (nskipped > 0)
					Console.WriteLine ("Regression tests: {0} ran, {1} skipped, {2} failed in {3}", ran, nskipped, failed, type);
				else
					Console.WriteLine ("Regression tests: {0} ran, {1} failed in {2}", ran, failed, type);
			}
		}

		if (reporter != null) {
			reporter.ReportResults (ran, nskipped, failed);
		}

		//Console.WriteLine ("Regression tests: {0} ran, {1} failed in [{2}]{3}", ran, failed, type.Assembly.GetName().Name, type);
		return failed;
	}

	static public int RunTests (Type type, string[] args) {
		return RunTests (type, args, null);
	}

	static public int RunTests (Type type) {
		return RunTests (type, null, null);
	}
}

/// Provide tests with the ability to find out how much time they have to run before being timed out.
public class TestTimeout
{
	private TimeSpan Timeout { get; }

	private DateTime StartTime { get; }

	public bool HaveTimeLeft { get { return DateTime.UtcNow - StartTime < Timeout; } }

	public static bool IsStressTest { get { return Environment.GetEnvironmentVariable("MONO_TESTS_STRESS") == "1"; } }

	private TestTimeout (TimeSpan timeout)
	{
		Timeout = timeout;
		StartTime = DateTime.UtcNow;
	}

	public static TestTimeout Start(TimeSpan timeout)
	{
		if (timeout.Ticks < 0)
		{
			throw new ArgumentException("timeout");
		}

		return new TestTimeout(timeout);
	}
}
