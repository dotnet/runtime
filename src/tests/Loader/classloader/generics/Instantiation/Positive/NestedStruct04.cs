// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public struct ValX0 {}
public struct ValY0 {}
public struct ValX1<T> {}
public struct ValY1<T> {}
public struct ValX2<T,U> {}
public struct ValY2<T,U>{}
public struct ValX3<T,U,V>{}
public struct ValY3<T,U,V>{}
public class RefX0 {}
public class RefY0 {}
public class RefX1<T> {}
public class RefY1<T> {}
public class RefX2<T,U> {}
public class RefY2<T,U>{}
public class RefX3<T,U,V>{}
public class RefY3<T,U,V>{}


public struct GenOuter<U>
{
	public struct GenInner<T>
	{
		public T Fld1;
	
		public GenInner(T fld1)
		{
			Fld1 =  fld1;
		}

		public bool InstVerify(System.Type t1)
		{
			bool result = true;

			if (!(Fld1.GetType().Equals(t1)))
			{	
				result = false;
				Console.WriteLine("Failed to verify type of Fld1 in: " + typeof(GenOuter<U>.GenInner<T>) );
			}
		
			return result;
		}
	}
}

public class Test_NestedStruct04
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
	
	[Fact]
	public static int TestEntryPoint()
	{
		Eval((new GenOuter<int>.GenInner<int>(new int())).InstVerify(typeof(int))); 	
		Eval((new GenOuter<int>.GenInner<double>(new double())).InstVerify(typeof(double))); 
		Eval((new GenOuter<int>.GenInner<string>("string")).InstVerify(typeof(string)));
		Eval((new GenOuter<int>.GenInner<object>(new object())).InstVerify(typeof(object))); 
		Eval((new GenOuter<int>.GenInner<Guid>(new Guid())).InstVerify(typeof(Guid))); 

		Eval((new GenOuter<string>.GenInner<int>(new int())).InstVerify(typeof(int))); 	
		Eval((new GenOuter<string>.GenInner<double>(new double())).InstVerify(typeof(double))); 
		Eval((new GenOuter<string>.GenInner<string>("string")).InstVerify(typeof(string)));
		Eval((new GenOuter<string>.GenInner<object>(new object())).InstVerify(typeof(object))); 
		Eval((new GenOuter<string>.GenInner<Guid>(new Guid())).InstVerify(typeof(Guid))); 

		
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
