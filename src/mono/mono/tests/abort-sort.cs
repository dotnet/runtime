using System;
using System.Threading;
using System.Collections.Generic;

public struct J
{
	public int i;

	public J(int i_) { i = i_; }
}

struct JComp : IComparer<J>
{
	public int Compare(J x, J y)
	{
		int val = 0;
		Thread.Sleep (Timeout.Infinite);
		return val;
	}
}

public class Foo
{
	static ManualResetEventSlim mre;

	public static void Main()
	{
		mre = new ManualResetEventSlim();
		var t = new Thread(Run);
		t.Start();
		mre.Wait();
		Thread.Sleep(400);
		t.Abort();
		t.Join();
		Console.WriteLine("bye bye");

	}

	public static void Run()
	{
		int n = 10;
		var a = new J[n];
		for (int i = 0; i < n; ++i)
		{
			a[i] = new J(n - i);
		}
		mre.Set();
		Array.Sort(a, 0, n, new JComp());
	}
}
