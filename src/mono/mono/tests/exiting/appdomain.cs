
using System;

public class foo {
	public static void Main() {
		AppDomain domain=AppDomain.CreateDomain("Other");
		Console.WriteLine("About to execute");
		domain.ExecuteAssembly("main-exit.exe");
		Console.WriteLine("Execute returns");
		AppDomain.Unload(domain);
		Console.WriteLine("finished");
	}
}

