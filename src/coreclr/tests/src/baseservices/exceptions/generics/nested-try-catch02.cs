// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

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



public class GenException<T> : Exception {}
public class Gen<T>
{
	public void ExceptionTest(bool throwException)
	{
		try
		{
			try
			{
				if (throwException)
				{
					throw new GenException<T>();
				}
				Test.Eval(!throwException);
			}
			catch(GenException<T>)
			{
				Test.Eval(throwException);
				throw;
			}
		}
		catch(GenException<T>)
		{
			Test.Eval(throwException);
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
		new Gen<int>().ExceptionTest(true); 	
		new Gen<double>().ExceptionTest(true); 
		new Gen<string>().ExceptionTest(true);
		new Gen<object>().ExceptionTest(true); 
		new Gen<Guid>().ExceptionTest(true); 

		new Gen<int[]>().ExceptionTest(true); 
		new Gen<double[,]>().ExceptionTest(true); 
		new Gen<string[][][]>().ExceptionTest(true); 
		new Gen<object[,,,]>().ExceptionTest(true); 
		new Gen<Guid[][,,,][]>().ExceptionTest(true); 

		new Gen<RefX1<int>[]>().ExceptionTest(true); 
		new Gen<RefX1<double>[,]>().ExceptionTest(true); 
		new Gen<RefX1<string>[][][]>().ExceptionTest(true); 
		new Gen<RefX1<object>[,,,]>().ExceptionTest(true); 
		new Gen<RefX1<Guid>[][,,,][]>().ExceptionTest(true); 
		new Gen<RefX2<int,int>[]>().ExceptionTest(true); 
		new Gen<RefX2<double,double>[,]>().ExceptionTest(true); 
		new Gen<RefX2<string,string>[][][]>().ExceptionTest(true); 
		new Gen<RefX2<object,object>[,,,]>().ExceptionTest(true); 
		new Gen<RefX2<Guid,Guid>[][,,,][]>().ExceptionTest(true); 
		new Gen<ValX1<int>[]>().ExceptionTest(true); 
		new Gen<ValX1<double>[,]>().ExceptionTest(true); 
		new Gen<ValX1<string>[][][]>().ExceptionTest(true); 
		new Gen<ValX1<object>[,,,]>().ExceptionTest(true); 
		new Gen<ValX1<Guid>[][,,,][]>().ExceptionTest(true); 

		new Gen<ValX2<int,int>[]>().ExceptionTest(true); 
		new Gen<ValX2<double,double>[,]>().ExceptionTest(true); 
		new Gen<ValX2<string,string>[][][]>().ExceptionTest(true); 
		new Gen<ValX2<object,object>[,,,]>().ExceptionTest(true); 
		new Gen<ValX2<Guid,Guid>[][,,,][]>().ExceptionTest(true); 
		
		new Gen<RefX1<int>>().ExceptionTest(true); 
		new Gen<RefX1<ValX1<int>>>().ExceptionTest(true); 
		new Gen<RefX2<int,string>>().ExceptionTest(true); 
		new Gen<RefX3<int,string,Guid>>().ExceptionTest(true); 

		new Gen<RefX1<RefX1<int>>>().ExceptionTest(true); 
		new Gen<RefX1<RefX1<RefX1<string>>>>().ExceptionTest(true); 
		new Gen<RefX1<RefX1<RefX1<RefX1<Guid>>>>>().ExceptionTest(true); 

		new Gen<RefX1<RefX2<int,string>>>().ExceptionTest(true); 
		new Gen<RefX2<RefX2<RefX1<int>,RefX3<int,string, RefX1<RefX2<int,string>>>>,RefX2<RefX1<int>,RefX3<int,string, RefX1<RefX2<int,string>>>>>>().ExceptionTest(true); 
		new Gen<RefX3<RefX1<int[][,,,]>,RefX2<object[,,,][][],Guid[][][]>,RefX3<double[,,,,,,,,,,],Guid[][][][,,,,][,,,,][][][],string[][][][][][][][][][][]>>>().ExceptionTest(true); 

		new Gen<ValX1<int>>().ExceptionTest(true); 
		new Gen<ValX1<RefX1<int>>>().ExceptionTest(true); 
		new Gen<ValX2<int,string>>().ExceptionTest(true); 
		new Gen<ValX3<int,string,Guid>>().ExceptionTest(true); 

		new Gen<ValX1<ValX1<int>>>().ExceptionTest(true); 
		new Gen<ValX1<ValX1<ValX1<string>>>>().ExceptionTest(true); 
		new Gen<ValX1<ValX1<ValX1<ValX1<Guid>>>>>().ExceptionTest(true); 

		new Gen<ValX1<ValX2<int,string>>>().ExceptionTest(true); 
		new Gen<ValX2<ValX2<ValX1<int>,ValX3<int,string, ValX1<ValX2<int,string>>>>,ValX2<ValX1<int>,ValX3<int,string, ValX1<ValX2<int,string>>>>>>().ExceptionTest(true); 
		new Gen<ValX3<ValX1<int[][,,,]>,ValX2<object[,,,][][],Guid[][][]>,ValX3<double[,,,,,,,,,,],Guid[][][][,,,,][,,,,][][][],string[][][][][][][][][][][]>>>().ExceptionTest(true); 
		


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
