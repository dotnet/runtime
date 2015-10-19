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
public struct Gen
{
	public void ExceptionTest<U>(bool throwException)
	{
		try
		{
			if (throwException)
			{
				throw new GenException<U>();
			}
			Test.Eval(!throwException);
		}
		catch(GenException<U>)
		{
			Test.Eval(throwException);
		}
		catch
		{
			Console.WriteLine("Caught Wrong Exception");
			Test.Eval(false);
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
		new Gen().ExceptionTest<int>(true);
		new Gen().ExceptionTest<double>(true); 
		new Gen().ExceptionTest<string>(true);
		new Gen().ExceptionTest<object>(true); 
		new Gen().ExceptionTest<Guid>(true); 

		new Gen().ExceptionTest<int[]>(true); 
		new Gen().ExceptionTest<double[,]>(true); 
		new Gen().ExceptionTest<string[][][]>(true); 
		new Gen().ExceptionTest<object[,,,]>(true); 
		new Gen().ExceptionTest<Guid[][,,,][]>(true); 

		new Gen().ExceptionTest<RefX1<int>[]>(true); 
		new Gen().ExceptionTest<RefX1<double>[,]>(true); 
		new Gen().ExceptionTest<RefX1<string>[][][]>(true); 
		new Gen().ExceptionTest<RefX1<object>[,,,]>(true); 
		new Gen().ExceptionTest<RefX1<Guid>[][,,,][]>(true); 
		new Gen().ExceptionTest<RefX2<int,int>[]>(true); 
		new Gen().ExceptionTest<RefX2<double,double>[,]>(true); 
		new Gen().ExceptionTest<RefX2<string,string>[][][]>(true); 
		new Gen().ExceptionTest<RefX2<object,object>[,,,]>(true); 
		new Gen().ExceptionTest<RefX2<Guid,Guid>[][,,,][]>(true); 
		new Gen().ExceptionTest<ValX1<int>[]>(true); 
		new Gen().ExceptionTest<ValX1<double>[,]>(true); 
		new Gen().ExceptionTest<ValX1<string>[][][]>(true); 
		new Gen().ExceptionTest<ValX1<object>[,,,]>(true); 
		new Gen().ExceptionTest<ValX1<Guid>[][,,,][]>(true); 

		new Gen().ExceptionTest<ValX2<int,int>[]>(true); 
		new Gen().ExceptionTest<ValX2<double,double>[,]>(true); 
		new Gen().ExceptionTest<ValX2<string,string>[][][]>(true); 
		new Gen().ExceptionTest<ValX2<object,object>[,,,]>(true); 
		new Gen().ExceptionTest<ValX2<Guid,Guid>[][,,,][]>(true); 
		
		new Gen().ExceptionTest<RefX1<int>>(true); 
		new Gen().ExceptionTest<RefX1<ValX1<int>>>(true); 
		new Gen().ExceptionTest<RefX2<int,string>>(true); 
		new Gen().ExceptionTest<RefX3<int,string,Guid>>(true); 

		new Gen().ExceptionTest<RefX1<RefX1<int>>>(true); 
		new Gen().ExceptionTest<RefX1<RefX1<RefX1<string>>>>(true); 
		new Gen().ExceptionTest<RefX1<RefX1<RefX1<RefX1<Guid>>>>>(true); 

		new Gen().ExceptionTest<RefX1<RefX2<int,string>>>(true); 
		new Gen().ExceptionTest<RefX2<RefX2<RefX1<int>,RefX3<int,string, RefX1<RefX2<int,string>>>>,RefX2<RefX1<int>,RefX3<int,string, RefX1<RefX2<int,string>>>>>>(true); 
		new Gen().ExceptionTest<RefX3<RefX1<int[][,,,]>,RefX2<object[,,,][][],Guid[][][]>,RefX3<double[,,,,,,,,,,],Guid[][][][,,,,][,,,,][][][],string[][][][][][][][][][][]>>>(true); 

		new Gen().ExceptionTest<ValX1<int>>(true); 
		new Gen().ExceptionTest<ValX1<RefX1<int>>>(true); 
		new Gen().ExceptionTest<ValX2<int,string>>(true); 
		new Gen().ExceptionTest<ValX3<int,string,Guid>>(true); 

		new Gen().ExceptionTest<ValX1<ValX1<int>>>(true); 
		new Gen().ExceptionTest<ValX1<ValX1<ValX1<string>>>>(true); 
		new Gen().ExceptionTest<ValX1<ValX1<ValX1<ValX1<Guid>>>>>(true); 

		new Gen().ExceptionTest<ValX1<ValX2<int,string>>>(true); 
		new Gen().ExceptionTest<ValX2<ValX2<ValX1<int>,ValX3<int,string, ValX1<ValX2<int,string>>>>,ValX2<ValX1<int>,ValX3<int,string, ValX1<ValX2<int,string>>>>>>(true); 
		new Gen().ExceptionTest<ValX3<ValX1<int[][,,,]>,ValX2<object[,,,][][],Guid[][][]>,ValX3<double[,,,,,,,,,,],Guid[][][][,,,,][,,,,][][][],string[][][][][][][][][][][]>>>(true); 
		



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
