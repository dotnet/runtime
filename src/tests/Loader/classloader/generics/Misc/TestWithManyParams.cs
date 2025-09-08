// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// test large number of generic parameters
//test large number of nested generic type isntantiations

using System;
using Xunit;


public class Test_TestWithManyParams
{
	public static int i = 0;


	[Fact]
	public static int TestEntryPoint()
	{
		int ret1, ret2;
	 	try
	 	{

			IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<int>>>>>>>>>>>>>>>>>>>>>>>>>>>>> t = new Child<int>();

			Console.WriteLine("Test1: PASS");
			ret1 = 100;
	 	}
		catch (Exception e)
		{
			Console.WriteLine("Test1 FAIL: Caught unexpected exception - " + e);
			ret1 = 101;
		}


 		try
	 	{

			IParent2<int,double,String, Object, char, uint, Guid, bool, IParent<int>, IParent<double>, IParent<String>, IParent<Object>, IParent<char>,
						IParent<uint>, IParent<Guid>,IParent<bool>,IParent<IParent<int>>,IParent<IParent<double>>,IParent<IParent<String>>,IParent<IParent<Object>>,
						IParent<IParent<char>>,IParent<IParent<uint>>,IParent<IParent<Guid>>,IParent<IParent<bool>>,IParent<char>,IParent<int>> t2 = new Child2<int>();

			ret2 = 100;
			Console.WriteLine("Test2: PASS");

	 	}
		catch (Exception e)
		{
			Console.WriteLine("Test2 FAIL: Caught unexpected exception - " + e);
			ret2 = 101;
		}


		if (ret1 == 100 && ret2 == 100)
		{
			return 100;
		}
		else
		{
			Console.WriteLine("FAIL");
			return 101;
		}


	}
}

public class GenTypes
{

	public IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<int>>>>>>>>>>>>>>>>>>>>>>>>>>>>> iParent_Nested;
	public Child<int> child_int;


	public IParent2<int,double,String, Object, char, uint, Guid, bool, IParent<int>, IParent<double>, IParent<String>, IParent<Object>, IParent<char>,
						IParent<uint>, IParent<Guid>,IParent<bool>,IParent<IParent<int>>,IParent<IParent<double>>,IParent<IParent<String>>,IParent<IParent<Object>>,
						IParent<IParent<char>>,IParent<IParent<uint>>,IParent<IParent<Guid>>,IParent<IParent<bool>>,IParent<char>,IParent<int>> iParent2ManyParams;
	public Child2<int> child2_int;
}


public interface IParent<T>
{
	void Method1<Sa>()  ;
}


public interface IParent2<A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,R,S,T,U,V,W,X,Y,Z>
{
	void Method1<Sa>()  ;
}

public class Child<T> : IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<int>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
{

	void IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<IParent<int>>>>>>>>>>>>>>>>>>>>>>>>>>>>>.Method1<Sa>()  {}
}


public class Child2<T> : IParent2<int,double,String, Object, char, uint, Guid, bool, IParent<int>, IParent<double>, IParent<String>, IParent<Object>, IParent<char>,
						IParent<uint>, IParent<Guid>,IParent<bool>,IParent<IParent<int>>,IParent<IParent<double>>,IParent<IParent<String>>,IParent<IParent<Object>>,
						IParent<IParent<char>>,IParent<IParent<uint>>,IParent<IParent<Guid>>,IParent<IParent<bool>>,IParent<char>,IParent<int>>
{

	void  IParent2<int,double,String, Object, char, uint, Guid, bool, IParent<int>, IParent<double>, IParent<String>, IParent<Object>, IParent<char>,
						IParent<uint>, IParent<Guid>,IParent<bool>,IParent<IParent<int>>,IParent<IParent<double>>,IParent<IParent<String>>,IParent<IParent<Object>>,
						IParent<IParent<char>>,IParent<IParent<uint>>,IParent<IParent<Guid>>,IParent<IParent<bool>>,IParent<char>,IParent<int>>.Method1<Sa>()  {}
}



