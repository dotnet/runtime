// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// This is a regression test for VSWhidbey 139763
// Static Constructor introduces bad side effects for generic type instantiated 
// to reference type in async static event case.

using System;
using System.Threading;

public delegate T D<T>(T t);
	
public class Gen<T>
{	
	public static void Init()
	{
		Ev+=new D<T>(Gen<T>.OnEv);	
	}

	static Gen()
	{
		Console.WriteLine("Call to CCTor has bad side effects");
	}

	private static D<T> FldEv;
	
	public static event D<T> Ev
	{
		add
		{
			FldEv+=value;
		}
		remove
		{
			FldEv-=value;
		}
	}

	public static T OnEv(T t) { return t; }

	public static void AsyncFireEv(T t)
	{
		Console.WriteLine("AsyncFireEv");
		try
		{
			IAsyncResult ar = FldEv.BeginInvoke(t,null,null);
			ar.AsyncWaitHandle.WaitOne();
			Test.Eval(t.Equals(FldEv.EndInvoke(ar)));
		}
		catch (NotSupportedException)
		{
			// expected
			Test.Eval(true);
		}
	}

}
	
public class Test
{
	public static int counter = 0;
	public static bool result = true;
	public static void Eval(bool exp)
	{
		counter++;
		if (!exp)
		{
			result = exp;
			Console.WriteLine("Test Failed at location: " + counter);
		}
	}
	
	public static int Main()
	{
		
		Gen<object>.Init();
		Gen<object>.AsyncFireEv("X");

		Console.WriteLine("PASS");

		return 100;
	}
}
