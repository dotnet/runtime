// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// test constrainsts with inheritance

using System;
using Xunit;

public interface I1{}


//===================================================
// Test #1 
//base type (A<T>) has constraint and child (B<T>) inherits an instantiated base type (A<int>)
public class A<T> where T :  I1
	{}
public class B<T> : A<I1>
	{}


//===================================================
// Test #2
// base type (A<T>) has constraint and child (C<T>) inherits an uninstantiated base type (A<T>)
public class C<T> : A<T> where T : I1
	{}


//===================================================
// Test #3
// child class (E<T>) inherits from base type and child type adds a constraint
public class D<T,U> {}

public class E<T,U> : D<T,U> where T: B<U>
	{}



//===================================================
// Test #4
// child inherits from uninstantiated generic interface and an instantiated generic interface

public interface I2<T>{}

public interface I3<U>{}

public class F<T> : I2<T>, I3<int>{}


//===================================================
// Test #5
// child inherits from instantiated generic interface and an instantiated generic interface
// child class has 2 parameters

public class G<T, U> : I2<C<I1>>, I3<D<int,double>>{}

//===================================================

public class GenTypes
{
	public A<I1> a_I1;
	public B<I1> b_I1;

	public C<I1> c_I1;

	public D<B<I1>,I1> d_BofI1_I1;
	
	public E<B<I1>,I1> e_BofI1_I1;

	public I2<int> i2_int;

	public F<int> f_int;

	public G<int,I1> g_int_I1;
	 
	public I2<C<I1>> i2_CofI1;
	
}

public class Test_ConstraintsAndInheritance
{
	[Fact]
	public static int TestEntryPoint()
	{
		try
		{
			Console.Write("Test1: ");
	 		A<I1> i = new B<I1>();

			Console.WriteLine("PASS");
			Console.Write("Test2: ");
			A<I1> i2 = new C<I1>();

			Console.WriteLine("PASS");
			Console.Write("Test3: ");
			D<B<I1>,I1> e = new E<B<I1>,I1>();

			Console.WriteLine("PASS");
			Console.Write("Test4: ");
			I2<int> ii2 = new F<int>();

			Console.WriteLine("PASS");
			Console.Write("Test5: ");
			I2<C<I1>> ii3 = new G<int,I1>();
			Console.WriteLine("PASS");

			return 100;
		}
		catch (Exception e)
		{
			Console.WriteLine("FAIL: Caught unexpected exception - " + e);
			return 101;		
		}

	}
}
