using System;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

[assembly:TypeForwardedTo(typeof(AssemblyBuilder))]

class Driver {
	public static void Main (){
		Console.WriteLine (typeof (Module.Exported));
		Console.WriteLine (new Module.Exported.NestedClass ());
	}
}
