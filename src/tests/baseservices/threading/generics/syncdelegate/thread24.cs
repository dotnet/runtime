// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;

interface IGen<T>
{
	void Target<U>();
	T Dummy(T t);
}


class GenInt : IGen<int>
{
	public int Dummy(int t) { return t; }

	public virtual void Target<U>()
	{		
		//dummy line to avoid warnings
		Test_thread24.Eval(typeof(U)!=null);
		Interlocked.Increment(ref Test_thread24.Xcounter);
	}
	
	public static void DelegateTest<U>()
	{
		IGen<int> obj = new GenInt();
		ThreadStart d = new ThreadStart(obj.Target<U>);
		
		
		d();
		Test_thread24.Eval(Test_thread24.Xcounter==1);
		Test_thread24.Xcounter = 0;
	}
}

class GenDouble : IGen<double>
{
	public double Dummy(double t) { return t; }

	public virtual void Target<U>()
	{		
		//dummy line to avoid warnings
		Test_thread24.Eval(typeof(U)!=null);
		Interlocked.Increment(ref Test_thread24.Xcounter);
	}
	
	public static void DelegateTest<U>()
	{
		IGen<double> obj = new GenDouble();
		ThreadStart d = new ThreadStart(obj.Target<U>);
		
		
		d();
		Test_thread24.Eval(Test_thread24.Xcounter==1);
		Test_thread24.Xcounter = 0;
	}
}

class GenString : IGen<string>
{
	public string Dummy(string t) { return t; }

	public virtual void Target<U>()
	{		
		//dummy line to avoid warnings
		Test_thread24.Eval(typeof(U)!=null);
		Interlocked.Increment(ref Test_thread24.Xcounter);
	}
	
	public static void DelegateTest<U>()
	{
		IGen<string> obj = new GenString();
		ThreadStart d = new ThreadStart(obj.Target<U>);
		
		
		d();
		Test_thread24.Eval(Test_thread24.Xcounter==1);
		Test_thread24.Xcounter = 0;
	}
}

class GenObject : IGen<object>
{
	public object Dummy(object t) { return t; }

	public virtual void Target<U>()
	{		
		//dummy line to avoid warnings
		Test_thread24.Eval(typeof(U)!=null);
		Interlocked.Increment(ref Test_thread24.Xcounter);
	}
	
	public static void DelegateTest<U>()
	{
		IGen<object> obj = new GenObject();
		ThreadStart d = new ThreadStart(obj.Target<U>);
		
		
		d();
		Test_thread24.Eval(Test_thread24.Xcounter==1);
		Test_thread24.Xcounter = 0;
	}
}

class GenGuid : IGen<Guid>
{
	public Guid Dummy(Guid t) { return t; }

	public virtual void Target<U>()
	{		
		//dummy line to avoid warnings
		Test_thread24.Eval(typeof(U)!=null);
		Interlocked.Increment(ref Test_thread24.Xcounter);
	}
	
	public static void DelegateTest<U>()
	{
		IGen<Guid> obj = new GenGuid();
		ThreadStart d = new ThreadStart(obj.Target<U>);
		
		
		d();
		Test_thread24.Eval(Test_thread24.Xcounter==1);
		Test_thread24.Xcounter = 0;
	}
}

public class Test_thread24
{
	public static int nThreads =50;
	public static int counter = 0;
	public static int Xcounter = 0;
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
	
	[Fact]
	public static int TestEntryPoint()
	{
	
		GenInt.DelegateTest<int>();
		GenDouble.DelegateTest<int>();
		GenString.DelegateTest<int>();
		GenObject.DelegateTest<int>(); 
		GenGuid.DelegateTest<int>(); 

		GenInt.DelegateTest<double>();
		GenDouble.DelegateTest<double>();
		GenString.DelegateTest<double>();
		GenObject.DelegateTest<double>(); 
		GenGuid.DelegateTest<double>(); 

		GenInt.DelegateTest<string>();
		GenDouble.DelegateTest<string>();
		GenString.DelegateTest<string>();
		GenObject.DelegateTest<string>(); 
		GenGuid.DelegateTest<string>(); 

		GenInt.DelegateTest<object>();
		GenDouble.DelegateTest<object>();
		GenString.DelegateTest<object>();
		GenObject.DelegateTest<object>(); 
		GenGuid.DelegateTest<object>(); 

		GenInt.DelegateTest<Guid>();
		GenDouble.DelegateTest<Guid>();
		GenString.DelegateTest<Guid>();
		GenObject.DelegateTest<Guid>(); 
		GenGuid.DelegateTest<Guid>(); 

	
		if (result)
		{
			Console.WriteLine("Test Passed");
			return 100;
		}
		else
		{
			Console.WriteLine("Test Failed");
			return 1;
		}
	}
}		


