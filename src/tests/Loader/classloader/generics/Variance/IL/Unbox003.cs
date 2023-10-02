// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Base {}
public class Sub : Base {}

public class GBase<T> {}
public class GSubT<T> : GBase<T> {}

public class GTU<T,U> : IPlusT<T>, IMinusT<U>, IPlusTMinusU<T,U> {}
public class GTArrUArr<T,U> : IPlusT<T[]>, IMinusT<U[]>, IPlusTMinusU<T[],U[]> {}
public class GRefTRefU<T,U> : IPlusT<IPlusT<T>>, IPlusT<IMinusT<U>>, IPlusTMinusU<IPlusT<T>, IPlusT<U>> {}
public class GRefTArrRefUArr<T,U> : IPlusT<IPlusT<T[]>>, IPlusT<IMinusT<U[]>>, IPlusTMinusU<IPlusT<T[]>,IPlusT<U[]>> {}
public class GArrRefTArrRefU<T,U> : IPlusT<IPlusT<T>[]>, IPlusT<IMinusT<U>[]>, IPlusTMinusU<IPlusT<T>[],IPlusT<U>[]> {}

public class TestClass
{
	static int iTestCount= 0;	
	static int iErrorCount= 0;	
	static int iExitCode = 101;

	public static void Eval(string location, bool exp)
	{
		++iTestCount;

		if ( !(exp))
		{
			iErrorCount++;
			Console.WriteLine("Test Failed at location: {0} @ count {1} ", location, iTestCount);
		}
	}
	private static void UnboxUToTInternal<T,U>()
	{
		T t = (T) Activator.CreateInstance(typeof(U));
	}

	private static void CaseClassUToTWrapper<T,U>()
	{
		UnboxUToTInternal<T,U>();
	}

	public static bool UnboxUToT<T,U>(bool expected)
	{
		try
		{
			CaseClassUToTWrapper<T,U>();
			if (expected)
			{
				return true;
			}
			else
			{
				return false;
			}
		}
		catch (InvalidCastException)
		{
			if (expected)
			{
				Console.WriteLine("Unexpected Exception InvalidCastException");
				return false;
			}
			else
			{
				return true;
			}	
		}
		catch(Exception E)
		{
			Console.WriteLine("Unexpected Exception {0}, with T = {1} and U = {2}", E, typeof(T), typeof(U));
			return false;
		}
	}
	
	private static bool RunTests()
	{	
		Eval("Test001", UnboxUToT<IPlusT<Sub>, GTU<Base,Sub>>(false));
		Eval("Test002", UnboxUToT<IMinusT<Base>, GTU<Base,Sub>>(false));
		Eval("Test003", UnboxUToT<IPlusTMinusU<Sub,Base>, GTU<Base,Sub>>(false));

		Eval("Test004", UnboxUToT<IPlusT<Sub[]>, GTU<Base[],Sub[]>>(false));
		Eval("Test005", UnboxUToT<IMinusT<Base[]>, GTU<Base[],Sub[]>>(false));
		Eval("Test006", UnboxUToT<IPlusTMinusU<Sub[],Base[]>, GTU<Base[],Sub[]>>(false));

		Eval("Test007", UnboxUToT<IPlusT<GSubT<int>>, GTU<GBase<int>,GSubT<string>>>(false));
		Eval("Test008", UnboxUToT<IMinusT<GBase<string>>, GTU<GBase<int>,GSubT<string>>>(false));
		Eval("Test009", UnboxUToT<IPlusTMinusU<GSubT<int>,GBase<string>>, GTU<GBase<int>,GSubT<string>>>(false));

		Eval("Test010", UnboxUToT<IPlusT<GSubT<int>[]>, GTU<GBase<int>[],GSubT<string>[]>>(false));
		Eval("Test011", UnboxUToT<IMinusT<GBase<string>[]>, GTU<GBase<int>[],GSubT<string>[]>>(false));
		Eval("Test012", UnboxUToT<IPlusTMinusU<GSubT<int>[],GBase<string>[]>, GTU<GBase<int>[],GSubT<string>[]>>(false));

		Eval("Test101", UnboxUToT<IPlusT<Sub[]>, GTArrUArr<Base,Sub>>(false));
		Eval("Test102", UnboxUToT<IMinusT<Base[]>, GTArrUArr<Base,Sub>>(false));
		Eval("Test103", UnboxUToT<IPlusTMinusU<Sub[],Base[]>, GTArrUArr<Base,Sub>>(false));
		
		Eval("Test104", UnboxUToT<IPlusT<Sub[][]>, GTArrUArr<Base[],Sub[]>>(false));
		Eval("Test105", UnboxUToT<IMinusT<Base[][]>, GTArrUArr<Base[],Sub[]>>(false));
		Eval("Test106", UnboxUToT<IPlusTMinusU<Sub[][],Base[][]>, GTArrUArr<Base[],Sub[]>>(false));

		Eval("Test107", UnboxUToT<IPlusT<GSubT<int>[]>, GTArrUArr<GBase<int>,GSubT<string>>>(false));
		Eval("Test108", UnboxUToT<IMinusT<GBase<string>[]>, GTArrUArr<GBase<int>,GSubT<string>>>(false));
		Eval("Test109", UnboxUToT<IPlusTMinusU<GSubT<int>[],GBase<string>[]>, GTArrUArr<GBase<int>,GSubT<string>>>(false));

		Eval("Test110", UnboxUToT<IPlusT<GSubT<int>[][]>, GTArrUArr<GBase<int>[],GSubT<string>[]>>(false));
		Eval("Test111", UnboxUToT<IMinusT<GBase<string>[][]>, GTArrUArr<GBase<int>[],GSubT<string>[]>>(false));
		Eval("Test112", UnboxUToT<IPlusTMinusU<GSubT<int>[][],GBase<string>[][]>, GTArrUArr<GBase<int>[],GSubT<string>[]>>(false));

		Eval("Test201", UnboxUToT<IPlusT<IPlusT<Sub>>, GRefTRefU<Base,Sub>>(false));
		Eval("Test202", UnboxUToT<IPlusT<IMinusT<Base>>, GRefTRefU<Base,Sub>>(false));
		Eval("Test203", UnboxUToT<IPlusTMinusU<IPlusT<Sub>,IPlusT<Base>>, GRefTRefU<Base,Sub>>(false));

		Eval("Test204", UnboxUToT<IPlusT<IPlusT<Sub[]>>, GRefTRefU<Base[],Sub[]>>(false));
		Eval("Test205", UnboxUToT<IPlusT<IMinusT<Base[]>>, GRefTRefU<Base[],Sub[]>>(false));
		Eval("Test206", UnboxUToT<IPlusTMinusU<IPlusT<Sub[]>,IPlusT<Base[]>>, GRefTRefU<Base[],Sub[]>>(false));

		Eval("Test207", UnboxUToT<IPlusT<IPlusT<GSubT<int>>>, GRefTRefU<GBase<int>,GSubT<string>>>(false));
		Eval("Test208", UnboxUToT<IPlusT<IMinusT<GBase<string>>>, GRefTRefU<GBase<int>,GSubT<string>>>(false));
		Eval("Test209", UnboxUToT<IPlusTMinusU<IPlusT<GSubT<int>>,IPlusT<GBase<string>>>, GRefTRefU<GBase<int>,GSubT<string>>>(false));

		Eval("Test210", UnboxUToT<IPlusT<IPlusT<GSubT<int>[]>>, GRefTRefU<GBase<int>[],GSubT<string>[]>>(false));
		Eval("Test211", UnboxUToT<IPlusT<IMinusT<GBase<string>[]>>, GRefTRefU<GBase<int>[],GSubT<string>[]>>(false));
		Eval("Test212", UnboxUToT<IPlusTMinusU<IPlusT<GSubT<int>[]>,IPlusT<GBase<string>[]>>, GRefTRefU<GBase<int>[],GSubT<string>[]>>(false));

		Eval("Test301", UnboxUToT<IPlusT<IPlusT<Sub[]>>, GRefTArrRefUArr<Base,Sub>>(false));
		Eval("Test302", UnboxUToT<IPlusT<IMinusT<Base[]>>, GRefTArrRefUArr<Base,Sub>>(false));
		Eval("Test303", UnboxUToT<IPlusTMinusU<IPlusT<Sub[]>,IPlusT<Base[]>>, GRefTArrRefUArr<Base,Sub>>(false));

		Eval("Test304", UnboxUToT<IPlusT<IPlusT<Sub[][]>>, GRefTArrRefUArr<Base[],Sub[]>>(false));
		Eval("Test305", UnboxUToT<IPlusT<IMinusT<Base[][]>>, GRefTArrRefUArr<Base[],Sub[]>>(false));
		Eval("Test306", UnboxUToT<IPlusTMinusU<IPlusT<Sub[][]>,IPlusT<Base[][]>>, GRefTArrRefUArr<Base[],Sub[]>>(false));

		Eval("Test307", UnboxUToT<IPlusT<IPlusT<GSubT<int>[]>>, GRefTArrRefUArr<GBase<int>,GSubT<string>>>(false));
		Eval("Test308", UnboxUToT<IPlusT<IMinusT<GBase<string>[]>>, GRefTArrRefUArr<GBase<int>,GSubT<string>>>(false));
		Eval("Test309", UnboxUToT<IPlusTMinusU<IPlusT<GSubT<int>[]>,IPlusT<GBase<string>[]>>, GRefTArrRefUArr<GBase<int>,GSubT<string>>>(false));

		Eval("Test310", UnboxUToT<IPlusT<IPlusT<GSubT<int>[][]>>, GRefTArrRefUArr<GBase<int>[],GSubT<string>[]>>(false));
		Eval("Test311", UnboxUToT<IPlusT<IMinusT<GBase<string>[][]>>, GRefTArrRefUArr<GBase<int>[],GSubT<string>[]>>(false));
		Eval("Test312", UnboxUToT<IPlusTMinusU<IPlusT<GSubT<int>[][]>,IPlusT<GBase<string>[][]>>, GRefTArrRefUArr<GBase<int>[],GSubT<string>[]>>(false));
		
		Eval("Test401", UnboxUToT<IPlusT<IPlusT<Sub>[]>, GArrRefTArrRefU<Base,Sub>>(false));
		Eval("Test402", UnboxUToT<IPlusT<IMinusT<Base>[]>, GArrRefTArrRefU<Base,Sub>>(false));
		Eval("Test403", UnboxUToT<IPlusTMinusU<IPlusT<Sub>[],IPlusT<Base>[]>, GArrRefTArrRefU<Base,Sub>>(false));

		Eval("Test404", UnboxUToT<IPlusT<IPlusT<Sub[,]>[]>, GArrRefTArrRefU<Base[,],Sub[,]>>(false));
		Eval("Test405", UnboxUToT<IPlusT<IMinusT<Base[,]>[]>, GArrRefTArrRefU<Base[,],Sub[,]>>(false));
		Eval("Test406", UnboxUToT<IPlusTMinusU<IPlusT<Sub[,]>[],IPlusT<Base[,]>[]>, GArrRefTArrRefU<Base[,],Sub[,]>>(false));

		Eval("Test407", UnboxUToT<IPlusT<IPlusT<GSubT<int>>[]>, GArrRefTArrRefU<GBase<int>,GSubT<string>>>(false));
		Eval("Test408", UnboxUToT<IPlusT<IMinusT<GBase<string>>[]>, GArrRefTArrRefU<GBase<int>,GSubT<string>>>(false));
		Eval("Test409", UnboxUToT<IPlusTMinusU<IPlusT<GSubT<int>>[],IPlusT<GBase<string>>[]>, GArrRefTArrRefU<GBase<int>,GSubT<string>>>(false));

		Eval("Test410", UnboxUToT<IPlusT<IPlusT<GSubT<int>[]>[]>, GArrRefTArrRefU<GBase<int>[],GSubT<string>[]>>(false));
		Eval("Test411", UnboxUToT<IPlusT<IMinusT<GBase<string>[]>[]>, GArrRefTArrRefU<GBase<int>[],GSubT<string>[]>>(false));
		Eval("Test412", UnboxUToT<IPlusTMinusU<IPlusT<GSubT<int>[]>[],IPlusT<GBase<string>[]>[]>, GArrRefTArrRefU<GBase<int>[],GSubT<string>[]>>(false));
		
		if( iErrorCount > 0 )
		{
			Console.WriteLine( "Total test cases: " + iTestCount + "  Failed test cases: " + iErrorCount );
			return false;
		}
		else
		{
			Console.WriteLine( "Total test cases: " + iTestCount );
			return true;
		}	
	}
	
	[Fact]
	public static int TestEntryPoint()
	{			
	
		if( RunTests() )
		{
			iExitCode = 100;
			Console.WriteLine( "All test cases passed" );
		}
		else
		{
			iExitCode = 101;
			Console.WriteLine( "Test failed" );
		}
		return iExitCode;
	}
	
}
