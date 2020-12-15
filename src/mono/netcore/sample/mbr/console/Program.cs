// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using MonoDelta;

namespace HelloWorld
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            bool isMono = typeof(object).Assembly.GetType("Mono.RuntimeStructs") != null;
            Console.WriteLine($"Hello World {(isMono ? "from Mono!" : "from CoreCLR!")}");
            Console.WriteLine(typeof(object).Assembly.FullName);
            Console.WriteLine(System.Reflection.Assembly.GetEntryAssembly ());
            Console.WriteLine(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);

	    Assembly assm = typeof (TestClass).Assembly;
	    var replacer = DeltaHelper.Make ();

	    string res = TestClass.TargetMethod ();
	    if (res != "OLD STRING")
		return 1;

	    replacer.Update (assm);

	    res = TestClass.TargetMethod ();
	    if (res != "NEW STRING")
		return 2;

	    return 0;
	}

    }
}

