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
		Eval("Test001", UnboxUToT<IPlusT<Base>, GTU<Sub,Base>>(true));
		Eval("Test002", UnboxUToT<IMinusT<Sub>, GTU<Sub,Base>>(true));
		Eval("Test003", UnboxUToT<IPlusTMinusU<Base,Sub>, GTU<Sub,Base>>(true));

		Eval("Test004", UnboxUToT<IPlusT<Base[]>, GTU<Sub[],Base[]>>(true));
		Eval("Test005", UnboxUToT<IMinusT<Sub[]>, GTU<Sub[],Base[]>>(true));
		Eval("Test006", UnboxUToT<IPlusTMinusU<Base[],Sub[]>, GTU<Sub[],Base[]>>(true));

		Eval("Test007", UnboxUToT<IPlusT<GBase<int>>, GTU<GSubT<int>,GBase<string>>>(true));
		Eval("Test008", UnboxUToT<IMinusT<GSubT<string>>, GTU<GSubT<int>,GBase<string>>>(true));
		Eval("Test009", UnboxUToT<IPlusTMinusU<GBase<int>,GSubT<string>>, GTU<GSubT<int>,GBase<string>>>(true));

		Eval("Test010", UnboxUToT<IPlusT<GBase<int>[]>, GTU<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test011", UnboxUToT<IMinusT<GSubT<string>[]>, GTU<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test012", UnboxUToT<IPlusTMinusU<GBase<int>[],GSubT<string>[]>, GTU<GSubT<int>[],GBase<string>[]>>(true));

		Eval("Test101", UnboxUToT<IPlusT<Base[]>, GTArrUArr<Sub,Base>>(true));
		Eval("Test102", UnboxUToT<IMinusT<Sub[]>, GTArrUArr<Sub,Base>>(true));
		Eval("Test103", UnboxUToT<IPlusTMinusU<Base[],Sub[]>, GTArrUArr<Sub,Base>>(true));
		
		Eval("Test104", UnboxUToT<IPlusT<Base[][]>, GTArrUArr<Sub[],Base[]>>(true));
		Eval("Test105", UnboxUToT<IMinusT<Sub[][]>, GTArrUArr<Sub[],Base[]>>(true));
		Eval("Test106", UnboxUToT<IPlusTMinusU<Base[][],Sub[][]>, GTArrUArr<Sub[],Base[]>>(true));

		Eval("Test107", UnboxUToT<IPlusT<GBase<int>[]>, GTArrUArr<GSubT<int>,GBase<string>>>(true));
		Eval("Test108", UnboxUToT<IMinusT<GSubT<string>[]>, GTArrUArr<GSubT<int>,GBase<string>>>(true));
		Eval("Test109", UnboxUToT<IPlusTMinusU<GBase<int>[],GSubT<string>[]>, GTArrUArr<GSubT<int>,GBase<string>>>(true));

		Eval("Test110", UnboxUToT<IPlusT<GBase<int>[][]>, GTArrUArr<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test111", UnboxUToT<IMinusT<GSubT<string>[][]>, GTArrUArr<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test112", UnboxUToT<IPlusTMinusU<GBase<int>[][],GSubT<string>[][]>, GTArrUArr<GSubT<int>[],GBase<string>[]>>(true));

		Eval("Test201", UnboxUToT<IPlusT<IPlusT<Base>>, GRefTRefU<Sub,Base>>(true));
		Eval("Test202", UnboxUToT<IPlusT<IMinusT<Sub>>, GRefTRefU<Sub,Base>>(true));
		Eval("Test203", UnboxUToT<IPlusTMinusU<IPlusT<Base>,IPlusT<Sub>>, GRefTRefU<Sub,Base>>(true));

		Eval("Test204", UnboxUToT<IPlusT<IPlusT<Base[]>>, GRefTRefU<Sub[],Base[]>>(true));
		Eval("Test205", UnboxUToT<IPlusT<IMinusT<Sub[]>>, GRefTRefU<Sub[],Base[]>>(true));
		Eval("Test206", UnboxUToT<IPlusTMinusU<IPlusT<Base[]>,IPlusT<Sub[]>>, GRefTRefU<Sub[],Base[]>>(true));

		Eval("Test207", UnboxUToT<IPlusT<IPlusT<GBase<int>>>, GRefTRefU<GSubT<int>,GBase<string>>>(true));
		Eval("Test208", UnboxUToT<IPlusT<IMinusT<GSubT<string>>>, GRefTRefU<GSubT<int>,GBase<string>>>(true));
		Eval("Test209", UnboxUToT<IPlusTMinusU<IPlusT<GBase<int>>,IPlusT<GSubT<string>>>, GRefTRefU<GSubT<int>,GBase<string>>>(true));

		Eval("Test210", UnboxUToT<IPlusT<IPlusT<GBase<int>[]>>, GRefTRefU<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test211", UnboxUToT<IPlusT<IMinusT<GSubT<string>[]>>, GRefTRefU<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test212", UnboxUToT<IPlusTMinusU<IPlusT<GBase<int>[]>,IPlusT<GSubT<string>[]>>, GRefTRefU<GSubT<int>[],GBase<string>[]>>(true));

		Eval("Test301", UnboxUToT<IPlusT<IPlusT<Base[]>>, GRefTArrRefUArr<Sub,Base>>(true));
		Eval("Test302", UnboxUToT<IPlusT<IMinusT<Sub[]>>, GRefTArrRefUArr<Sub,Base>>(true));
		Eval("Test303", UnboxUToT<IPlusTMinusU<IPlusT<Base[]>,IPlusT<Sub[]>>, GRefTArrRefUArr<Sub,Base>>(true));

		Eval("Test304", UnboxUToT<IPlusT<IPlusT<Base[][]>>, GRefTArrRefUArr<Sub[],Base[]>>(true));
		Eval("Test305", UnboxUToT<IPlusT<IMinusT<Sub[][]>>, GRefTArrRefUArr<Sub[],Base[]>>(true));
		Eval("Test306", UnboxUToT<IPlusTMinusU<IPlusT<Base[][]>,IPlusT<Sub[][]>>, GRefTArrRefUArr<Sub[],Base[]>>(true));

		Eval("Test307", UnboxUToT<IPlusT<IPlusT<GBase<int>[]>>, GRefTArrRefUArr<GSubT<int>,GBase<string>>>(true));
		Eval("Test308", UnboxUToT<IPlusT<IMinusT<GSubT<string>[]>>, GRefTArrRefUArr<GSubT<int>,GBase<string>>>(true));
		Eval("Test309", UnboxUToT<IPlusTMinusU<IPlusT<GBase<int>[]>,IPlusT<GSubT<string>[]>>, GRefTArrRefUArr<GSubT<int>,GBase<string>>>(true));

		Eval("Test310", UnboxUToT<IPlusT<IPlusT<GBase<int>[][]>>, GRefTArrRefUArr<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test311", UnboxUToT<IPlusT<IMinusT<GSubT<string>[][]>>, GRefTArrRefUArr<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test312", UnboxUToT<IPlusTMinusU<IPlusT<GBase<int>[][]>,IPlusT<GSubT<string>[][]>>, GRefTArrRefUArr<GSubT<int>[],GBase<string>[]>>(true));
		
		Eval("Test401", UnboxUToT<IPlusT<IPlusT<Base>[]>, GArrRefTArrRefU<Sub,Base>>(true));
		Eval("Test402", UnboxUToT<IPlusT<IMinusT<Sub>[]>, GArrRefTArrRefU<Sub,Base>>(true));
		Eval("Test403", UnboxUToT<IPlusTMinusU<IPlusT<Base>[],IPlusT<Sub>[]>, GArrRefTArrRefU<Sub,Base>>(true));

		Eval("Test404", UnboxUToT<IPlusT<IPlusT<Base[,]>[]>, GArrRefTArrRefU<Sub[,],Base[,]>>(true));
		Eval("Test405", UnboxUToT<IPlusT<IMinusT<Sub[,]>[]>, GArrRefTArrRefU<Sub[,],Base[,]>>(true));
		Eval("Test406", UnboxUToT<IPlusTMinusU<IPlusT<Base[,]>[],IPlusT<Sub[,]>[]>, GArrRefTArrRefU<Sub[,],Base[,]>>(true));

		Eval("Test407", UnboxUToT<IPlusT<IPlusT<GBase<int>>[]>, GArrRefTArrRefU<GSubT<int>,GBase<string>>>(true));
		Eval("Test408", UnboxUToT<IPlusT<IMinusT<GSubT<string>>[]>, GArrRefTArrRefU<GSubT<int>,GBase<string>>>(true));
		Eval("Test409", UnboxUToT<IPlusTMinusU<IPlusT<GBase<int>>[],IPlusT<GSubT<string>>[]>, GArrRefTArrRefU<GSubT<int>,GBase<string>>>(true));

		Eval("Test410", UnboxUToT<IPlusT<IPlusT<GBase<int>[]>[]>, GArrRefTArrRefU<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test411", UnboxUToT<IPlusT<IMinusT<GSubT<string>[]>[]>, GArrRefTArrRefU<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test412", UnboxUToT<IPlusTMinusU<IPlusT<GBase<int>[]>[],IPlusT<GSubT<string>[]>[]>, GArrRefTArrRefU<GSubT<int>[],GBase<string>[]>>(true));
		
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
