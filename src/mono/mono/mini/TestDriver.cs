using System;
using System.Reflection;
using System.Collections.Generic;

public class CategoryAttribute : Attribute
{
	public CategoryAttribute (string category) {
		Category = category;
	}

	public string Category {
		get; set;
	}
}

public class TestDriver {

	static public int RunTests (Type type, string[] args) {
		int failed = 0, ran = 0;
		int result, expected, elen;
		int i, j, iterations;
		string name;
		MethodInfo[] methods;
		bool do_timings = false;
		bool verbose = false;
		int tms = 0;
		DateTime start, end = DateTime.Now;

		iterations = 1;

		var exclude = new Dictionary<string, string> ();
		List<string> run_only = new List<string> ();
		if (args != null && args.Length > 0) {
			for (j = 0; j < args.Length;) {
				if (args [j] == "--time") {
					do_timings = true;
					j ++;
				} else if (args [j] == "--iter") {
					iterations = Int32.Parse (args [j + 1]);
					j += 2;
				} else if ((args [j] == "-v") || (args [j] == "--verbose")) {
					verbose = true;
				} else if (args [j] == "--exclude") {
					exclude [args [j + 1]] = args [j + 1];
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
				if (exclude.Count > 0) {
					var attrs = methods [i].GetCustomAttributes (typeof (CategoryAttribute), false);
					bool skip = false;
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
		
			if (do_timings) {
				Console.WriteLine ("Total ms: {0}", tms);
			}
			if (nskipped > 0)
				Console.WriteLine ("Regression tests: {0} ran, {1} skipped, {2} failed in {3}", ran, nskipped, failed, type);
			else
				Console.WriteLine ("Regression tests: {0} ran, {1} failed in {2}", ran, failed, type);
		}

		//Console.WriteLine ("Regression tests: {0} ran, {1} failed in [{2}]{3}", ran, failed, type.Assembly.GetName().Name, type);
		return failed;
	}
	static public int RunTests (Type type) {
		return RunTests (type, null);
	}
}

