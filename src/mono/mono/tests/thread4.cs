
using System;
using System.Threading;

public class Test {
	
	public static int Main () {
		Console.WriteLine ("Starting test\n");

		Console.WriteLine("Domain name: {0}\n", Thread.GetDomain().FriendlyName);
		Console.WriteLine("Domain id: {0}\n", Thread.GetDomainID().ToString());
		
		return 0;
	}
}

