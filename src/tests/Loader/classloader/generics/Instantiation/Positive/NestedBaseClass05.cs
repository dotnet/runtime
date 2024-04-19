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

public class Outer
{
	public class GenBase<T>
	{
		public T Fld1;
	
		public GenBase(T fld1)
		{
			Fld1 =  fld1;
		}

		public bool InstVerify(System.Type t1)
		{
			bool result = true;

			if (!(Fld1.GetType().Equals(t1)))
			{	
				result = false;
				Console.WriteLine("Failed to verify type of Fld1 in: " + typeof(GenBase<T>) );
			}
		
			return result;
		}
	}
}


public class Gen<T> : Outer.GenBase<T>
{	
	public Gen(T fld1) : base(fld1) {}

	new public bool InstVerify(System.Type t1)
	{
		return base.InstVerify(t1);	
	}
}

public class Test_NestedBaseClass05
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
		Eval((new Gen<int>(new int())).InstVerify(typeof(int))); 	
		Eval((new Gen<double>(new double())).InstVerify(typeof(double))); 
		Eval((new Gen<string>("string")).InstVerify(typeof(string)));
		Eval((new Gen<object>(new object())).InstVerify(typeof(object))); 
		Eval((new Gen<Guid>(new Guid())).InstVerify(typeof(Guid))); 

		Eval((new Gen<int[]>(new int[1])).InstVerify(typeof(int[]))); 
		Eval((new Gen<double[,]>(new double[1,1])).InstVerify(typeof(double[,])));
		Eval((new Gen<string[][][]>(new string[1][][])).InstVerify(typeof(string[][][]))); 
		Eval((new Gen<object[,,,]>(new object[1,1,1,1])).InstVerify(typeof(object[,,,])));
		Eval((new Gen<Guid[][,,,][]>(new Guid[1][,,,][])).InstVerify(typeof(Guid[][,,,][])));

		Eval((new Gen<RefX1<int>[]>(new RefX1<int>[]{})).InstVerify(typeof(RefX1<int>[]))); 
		Eval((new Gen<RefX1<double>[,]>(new RefX1<double>[1,1])).InstVerify(typeof(RefX1<double>[,])));
		Eval((new Gen<RefX1<string>[][][]>(new RefX1<string>[1][][])).InstVerify(typeof(RefX1<string>[][][]))); 
		Eval((new Gen<RefX1<object>[,,,]>(new RefX1<object>[1,1,1,1])).InstVerify(typeof(RefX1<object>[,,,])));
		Eval((new Gen<RefX1<Guid>[][,,,][]>(new RefX1<Guid>[1][,,,][])).InstVerify(typeof(RefX1<Guid>[][,,,][])));

		Eval((new Gen<RefX2<int,int>[]>(new RefX2<int,int>[]{})).InstVerify(typeof(RefX2<int,int>[]))); 
		Eval((new Gen<RefX2<double,double>[,]>(new RefX2<double,double>[1,1])).InstVerify(typeof(RefX2<double,double>[,])));
		Eval((new Gen<RefX2<string,string>[][][]>(new RefX2<string,string>[1][][])).InstVerify(typeof(RefX2<string,string>[][][]))); 
		Eval((new Gen<RefX2<object,object>[,,,]>(new RefX2<object,object>[1,1,1,1])).InstVerify(typeof(RefX2<object,object>[,,,])));
		Eval((new Gen<RefX2<Guid,Guid>[][,,,][]>(new RefX2<Guid,Guid>[1][,,,][])).InstVerify(typeof(RefX2<Guid,Guid>[][,,,][])));

		Eval((new Gen<ValX1<int>[]>(new ValX1<int>[]{})).InstVerify(typeof(ValX1<int>[]))); 
		Eval((new Gen<ValX1<double>[,]>(new ValX1<double>[1,1])).InstVerify(typeof(ValX1<double>[,])));
		Eval((new Gen<ValX1<string>[][][]>(new ValX1<string>[1][][])).InstVerify(typeof(ValX1<string>[][][]))); 
		Eval((new Gen<ValX1<object>[,,,]>(new ValX1<object>[1,1,1,1])).InstVerify(typeof(ValX1<object>[,,,])));
		Eval((new Gen<ValX1<Guid>[][,,,][]>(new ValX1<Guid>[1][,,,][])).InstVerify(typeof(ValX1<Guid>[][,,,][])));

		Eval((new Gen<ValX2<int,int>[]>(new ValX2<int,int>[]{})).InstVerify(typeof(ValX2<int,int>[]))); 
		Eval((new Gen<ValX2<double,double>[,]>(new ValX2<double,double>[1,1])).InstVerify(typeof(ValX2<double,double>[,])));
		Eval((new Gen<ValX2<string,string>[][][]>(new ValX2<string,string>[1][][])).InstVerify(typeof(ValX2<string,string>[][][]))); 
		Eval((new Gen<ValX2<object,object>[,,,]>(new ValX2<object,object>[1,1,1,1])).InstVerify(typeof(ValX2<object,object>[,,,])));

		Eval((new Gen<ValX2<Guid,Guid>[][,,,][]>(new ValX2<Guid,Guid>[1][,,,][])).InstVerify(typeof(ValX2<Guid,Guid>[][,,,][])));
		
		Eval((new Gen<RefX1<int>>(new RefX1<int>())).InstVerify(typeof(RefX1<int>)));
		Eval((new Gen<RefX1<ValX1<int>>>(new RefX1<ValX1<int>>())).InstVerify(typeof(RefX1<ValX1<int>>)));
		Eval((new Gen<RefX2<int,string>>(new RefX2<int,string>())).InstVerify(typeof(RefX2<int,string>)));
		Eval((new Gen<RefX3<int,string,Guid>>(new RefX3<int,string,Guid>())).InstVerify(typeof(RefX3<int,string,Guid>)));

		Eval((new Gen<RefX1<RefX1<int>>>(new RefX1<RefX1<int>>())).InstVerify(typeof(RefX1<RefX1<int>>)));
		Eval((new Gen<RefX1<RefX1<RefX1<string>>>>(new RefX1<RefX1<RefX1<string>>>())).InstVerify(typeof(RefX1<RefX1<RefX1<string>>>)));
		Eval((new Gen<RefX1<RefX1<RefX1<RefX1<Guid>>>>>(new RefX1<RefX1<RefX1<RefX1<Guid>>>>())).InstVerify(typeof(RefX1<RefX1<RefX1<RefX1<Guid>>>>)));

		Eval((new Gen<RefX1<RefX2<int,string>>>(new RefX1<RefX2<int,string>>())).InstVerify(typeof(RefX1<RefX2<int,string>>)));
		Eval((new Gen<RefX2<RefX2<RefX1<int>,RefX3<int,string, RefX1<RefX2<int,string>>>>,RefX2<RefX1<int>,RefX3<int,string, RefX1<RefX2<int,string>>>>>>(new RefX2<RefX2<RefX1<int>,RefX3<int,string, RefX1<RefX2<int,string>>>>,RefX2<RefX1<int>,RefX3<int,string, RefX1<RefX2<int,string>>>>>())).InstVerify(typeof(RefX2<RefX2<RefX1<int>,RefX3<int,string, RefX1<RefX2<int,string>>>>,RefX2<RefX1<int>,RefX3<int,string, RefX1<RefX2<int,string>>>>>)));
		Eval((new Gen<RefX3<RefX1<int[][,,,]>,RefX2<object[,,,][][],Guid[][][]>,RefX3<double[,,,,,,,,,,],Guid[][][][,,,,][,,,,][][][],string[][][][][][][][][][][]>>>(new RefX3<RefX1<int[][,,,]>,RefX2<object[,,,][][],Guid[][][]>,RefX3<double[,,,,,,,,,,],Guid[][][][,,,,][,,,,][][][],string[][][][][][][][][][][]>>())).InstVerify(typeof(RefX3<RefX1<int[][,,,]>,RefX2<object[,,,][][],Guid[][][]>,RefX3<double[,,,,,,,,,,],Guid[][][][,,,,][,,,,][][][],string[][][][][][][][][][][]>>)));

		Eval((new Gen<ValX1<int>>(new ValX1<int>())).InstVerify(typeof(ValX1<int>)));
		Eval((new Gen<ValX1<RefX1<int>>>(new ValX1<RefX1<int>>())).InstVerify(typeof(ValX1<RefX1<int>>)));
		Eval((new Gen<ValX2<int,string>>(new ValX2<int,string>())).InstVerify(typeof(ValX2<int,string>)));
		Eval((new Gen<ValX3<int,string,Guid>>(new ValX3<int,string,Guid>())).InstVerify(typeof(ValX3<int,string,Guid>)));

		Eval((new Gen<ValX1<ValX1<int>>>(new ValX1<ValX1<int>>())).InstVerify(typeof(ValX1<ValX1<int>>)));
		Eval((new Gen<ValX1<ValX1<ValX1<string>>>>(new ValX1<ValX1<ValX1<string>>>())).InstVerify(typeof(ValX1<ValX1<ValX1<string>>>)));
		Eval((new Gen<ValX1<ValX1<ValX1<ValX1<Guid>>>>>(new ValX1<ValX1<ValX1<ValX1<Guid>>>>())).InstVerify(typeof(ValX1<ValX1<ValX1<ValX1<Guid>>>>)));

		Eval((new Gen<ValX1<ValX2<int,string>>>(new ValX1<ValX2<int,string>>())).InstVerify(typeof(ValX1<ValX2<int,string>>)));
		Eval((new Gen<ValX2<ValX2<ValX1<int>,ValX3<int,string, ValX1<ValX2<int,string>>>>,ValX2<ValX1<int>,ValX3<int,string, ValX1<ValX2<int,string>>>>>>(new ValX2<ValX2<ValX1<int>,ValX3<int,string, ValX1<ValX2<int,string>>>>,ValX2<ValX1<int>,ValX3<int,string, ValX1<ValX2<int,string>>>>>())).InstVerify(typeof(ValX2<ValX2<ValX1<int>,ValX3<int,string, ValX1<ValX2<int,string>>>>,ValX2<ValX1<int>,ValX3<int,string, ValX1<ValX2<int,string>>>>>)));
		Eval((new Gen<ValX3<ValX1<int[][,,,]>,ValX2<object[,,,][][],Guid[][][]>,ValX3<double[,,,,,,,,,,],Guid[][][][,,,,][,,,,][][][],string[][][][][][][][][][][]>>>(new ValX3<ValX1<int[][,,,]>,ValX2<object[,,,][][],Guid[][][]>,ValX3<double[,,,,,,,,,,],Guid[][][][,,,,][,,,,][][][],string[][][][][][][][][][][]>>())).InstVerify(typeof(ValX3<ValX1<int[][,,,]>,ValX2<object[,,,][][],Guid[][][]>,ValX3<double[,,,,,,,,,,],Guid[][][][,,,,][,,,,][][][],string[][][][][][][][][][][]>>)));
		


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
