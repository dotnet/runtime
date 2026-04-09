
using System;

public class foo {
	public static int Main() {
		Environment.ExitCode = 2;
		AppDomain domain=AppDomain.CreateDomain("Other");
		Console.WriteLine("About to execute");
		domain.ExecuteAssembly("main-exit.exe");
		Console.WriteLine("Execute returns");
		AppDomain.Unload(domain);
		Console.WriteLine("finished");
		return 1;
	}
}

