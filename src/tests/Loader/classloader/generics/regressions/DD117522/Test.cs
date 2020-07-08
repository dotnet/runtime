// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

public class A<T>
{
    public class B : A<B.C[]>
    {
        public class C : B
        { }
    }
}

public class Program
{
    public static int Main()
    {
	try
	{
		M();
	}
	catch(TypeLoadException ex)
	{
		Console.WriteLine("Caught expected TypeLoadException. \nThe exception message is : {0}", ex.Message);
		Console.WriteLine("PASS");
		return 100;
	}
	catch(Exception ex)
	{
		Console.WriteLine("Caught unexpected exception: {0}", ex.Message);
		Console.WriteLine("FAIL");
		return 99;
	}

	Console.WriteLine("Did not catch TypeLoadException, FAIL");
	return 99;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void M()
    {
		new A<int>.B();
    }
}

