using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

public class MainClass
{
	class TestCase
	{
		public string MethodSpec;
		public string[] ExpectedMethods;
	}

	public static int Main (string[] args)
	{
		var testCase = new TestCase {
			MethodSpec = "*:Method",
			ExpectedMethods = new[] {
				"N1.Test:Method ()",
				"N2.Test:Method ()",
				"N2.Test:Method (int)",
				"N2.Test:Method (int,string[])",
			},
		};
		if (!RunTest (testCase))
			return 1;

		testCase = new TestCase {
			MethodSpec = "*:Method (int)",
			ExpectedMethods = new[] {
				"N2.Test:Method (int)",
			},
		};
		if (!RunTest (testCase))
			return 2;

		testCase = new TestCase
		{
			MethodSpec = "N1.Test:Method",
			ExpectedMethods = new[] {
				"N1.Test:Method ()",
			},
		};
		if (!RunTest (testCase))
			return 3;

		testCase = new TestCase {
			MethodSpec = "Test:Method",
			ExpectedMethods = new[] {
				"N1.Test:Method ()",
				"N2.Test:Method ()",
				"N2.Test:Method (int)",
				"N2.Test:Method (int,string[])",
			},
		};
		if (!RunTest (testCase))
			return 4;

		testCase = new TestCase {
			MethodSpec = "*:Method(int,string[])",
			ExpectedMethods = new[] {
				"N2.Test:Method (int,string[])",
			},
		};
		if (!RunTest (testCase))
			return 5;

		testCase = new TestCase {
			MethodSpec = "*:Method();N2.*:Method(int,string[])",
			ExpectedMethods = new[] {
				"N1.Test:Method ()",
				"N2.Test:Method ()",
				"N2.Test:Method (int,string[])",
			},
		};
		if (!RunTest (testCase))
			return 6;

		return 0;
	}

	static bool RunTest (TestCase testCase)
	{
		var thisProcess = typeof (MainClass).Assembly.Location;

		var process = StartCompileAssemblyProcess (thisProcess, testCase.MethodSpec);
		var output = process.StandardOutput.ReadToEnd ();
		process.WaitForExit ();

		var lines = output.Split (new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
		foreach (var expectedMethod in testCase.ExpectedMethods) {
			var sortedExpectedMethods = testCase.ExpectedMethods.OrderBy (x => x).ToArray ();

			var regex = new Regex ("converting method void (?<methodName>.*)");
			var matches = regex.Matches (output);
			var sortedJittedMethods = matches.Cast<Match> ().Select (x => x.Groups["methodName"])
			                           .SelectMany (x => x.Captures.Cast<Capture> ())
			                           .Select (x => x.Value)
			                           .OrderBy (x => x)
			                           .ToArray ();

			if (sortedJittedMethods.Length != sortedExpectedMethods.Length)
				return false;

			for (int i = 0; i < sortedJittedMethods.Length; ++i) {
				if (sortedJittedMethods[i] != sortedExpectedMethods[i])
					return false;
			}

		}

		return true;
	}

	static Process StartCompileAssemblyProcess (string process, string methodSpec)
	{
		var psi = new ProcessStartInfo (process) {
			UseShellExecute = false,
			RedirectStandardOutput = true,
		};
		psi.EnvironmentVariables["MONO_ENV_OPTIONS"] = "--compile-all";
		psi.EnvironmentVariables["MONO_VERBOSE_METHOD"] = methodSpec;

		return Process.Start (psi);
	}
}

namespace N1
{
	public class Test
	{
		public static void Method ()
		{
		}
	}
}

namespace N2
{
	public class Test
	{
		public static void Method (int n)
		{
		}

		public static void Method ()
		{
		}

		public static void Method (int n, string[] args)
		{
		}
	}
}


