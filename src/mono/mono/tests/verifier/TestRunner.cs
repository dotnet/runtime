using System;
using System.IO;
using System.Reflection;

public class TestRunner : MarshalByRefObject
{
	public void RunTest (String test) {
		Console.Write (test);

		Assembly a = Assembly.LoadFrom (test);

		MethodInfo mi = a.EntryPoint;

		if (mi == null) {
			Console.WriteLine (" FAILED (no entry point found)");
			return;
		}

		try {
			mi.Invoke (null, null);
			Console.WriteLine (" FAILED (silent success)");
		}
		catch (TargetInvocationException ex) {
			if (ex.InnerException is InvalidProgramException)
				Console.WriteLine (" OK");
			else
				Console.WriteLine (" FAILED -> " + ex.InnerException);
		}
		catch (Exception ex) {
			Console.WriteLine (" FAILED -> " + ex);
		}
	}

	public static void Main (String[] args) {
		if (args.Length < 1) {
			Console.WriteLine ("Usage: TestRunner <file pattern>");
			Environment.Exit (1);
			return;
		}

		String[] tests = Directory.GetFiles (".", args [0]);

		AppDomain domain = null;
		TestRunner runner = null;

		int count = 0;
		foreach (String test in tests) {
			/* 
			 * Run each bunch of tests in a new appdomain, then unload it to 
			 * avoid too many open files exceptions.
			 */
			if ((count % 500) == 0) {
				if (domain != null)
					AppDomain.Unload (domain);
				domain = AppDomain.CreateDomain ("domain-" + count);

				runner = (TestRunner)domain.CreateInstanceAndUnwrap (typeof (TestRunner).Assembly.FullName, typeof (TestRunner).FullName);
			}

			runner.RunTest (test);

			count ++;
		}
	}
}
