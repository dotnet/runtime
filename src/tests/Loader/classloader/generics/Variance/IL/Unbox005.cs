// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Base {}
public class Sub : Base {}

public class GBase<T> {}
public class GSubT<T> : GBase<T> {}
	
public struct GTU<T,U> : IPlusT<T>, IMinusT<U>, IMinusTPlusU<T,U> {}
public struct GTArrUArr<T,U> : IMinusT<T[]>, IPlusT<U[]>, IMinusTPlusU<T[],U[]> {}
public struct GRefTRefU<T,U> : IMinusT<IPlusT<T>>, IMinusT<IMinusT<U>>, IMinusTPlusU<IPlusT<T>, IPlusT<U>> {}
public struct GRefTArrRefUArr<T,U> : IMinusT<IPlusT<T[]>>, IMinusT<IMinusT<U[]>>, IMinusTPlusU<IPlusT<T[]>,IPlusT<U[]>> {}
public struct GArrRefTArrRefU<T,U> : IMinusT<IPlusT<T>[]>, IMinusT<IMinusT<U>[]>, IMinusTPlusU<IPlusT<T>[],IPlusT<U>[]> {}

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
		Eval("Test001", UnboxUToT<IPlusT<IMinusT<Base>>, GTU<IMinusT<Sub>,Base>>(false));
		Eval("Test002", UnboxUToT<IMinusT<IMinusT<Sub>>, GTU<Sub,IMinusT<Base>>>(false));
		Eval("Test003", UnboxUToT<IPlusTMinusU<Base,Sub>, GTU<Sub,Base>>(false));

		Eval("Test004", UnboxUToT<IPlusT<IMinusT<Base[]>>, GTU<IMinusT<Sub[]>,Base>>(false));
		Eval("Test005", UnboxUToT<IMinusT<IMinusT<Sub[]>>, GTU<Sub,IMinusT<Base[]>>>(false));
		Eval("Test006", UnboxUToT<IPlusTMinusU<Base[],Sub[]>, GTU<Sub[],Base[]>>(false));

		Eval("Test007", UnboxUToT<IPlusT<IMinusT<GBase<int>>>, GTU<IMinusT<GSubT<int>>,GBase<int>>>(false));
		Eval("Test008", UnboxUToT<IMinusT<IMinusT<GSubT<int>>>, GTU<GSubT<int>,IMinusT<GBase<int>>>>(false));
		Eval("Test009", UnboxUToT<IPlusTMinusU<GBase<int>,GSubT<int>>, GTU<GSubT<int>,GBase<int>>>(false));

		Eval("Test010", UnboxUToT<IPlusT<IMinusT<GBase<int>[]>>, GTU<IMinusT<GSubT<int>[]>,GBase<int>[]>>(false));
		Eval("Test011", UnboxUToT<IMinusT<IMinusT<GSubT<int>[]>>, GTU<GSubT<int>[],IMinusT<GBase<int>[]>>>(false));
		Eval("Test012", UnboxUToT<IPlusTMinusU<GBase<int>[],GSubT<int>[]>, GTU<GSubT<int>[],GBase<int>[]>>(false));

		Eval("Test101", UnboxUToT<IPlusT<IMinusT<Base>[]>, GTArrUArr<IMinusT<Sub>,Base>>(false));
		Eval("Test102", UnboxUToT<IMinusT<IMinusT<Sub>[]>, GTArrUArr<Sub,IMinusT<Base>>>(false));
		Eval("Test103", UnboxUToT<IPlusTMinusU<Base[],Sub[]>, GTArrUArr<Sub,Base>>(false));

		Eval("Test104", UnboxUToT<IPlusT<IMinusT<Base[]>[]>, GTArrUArr<IMinusT<Sub[]>,Base>>(false));
		Eval("Test105", UnboxUToT<IMinusT<IMinusT<Sub[]>[]>, GTArrUArr<Sub,IMinusT<Base[]>>>(false));
		Eval("Test106", UnboxUToT<IPlusTMinusU<Base[][],Sub[][]>, GTArrUArr<Sub[],Base[]>>(false));

		Eval("Test107", UnboxUToT<IPlusT<IMinusT<GBase<int>>[]>, GTArrUArr<IMinusT<GSubT<int>>,GBase<int>>>(false));
		Eval("Test108", UnboxUToT<IMinusT<IMinusT<GSubT<int>[]>>, GTArrUArr<GSubT<int>,IMinusT<GBase<int>>>>(false));
		Eval("Test109", UnboxUToT<IPlusTMinusU<GBase<int>,GSubT<int>>, GTArrUArr<GSubT<int>,GBase<int>>>(false));

		Eval("Test110", UnboxUToT<IPlusT<IMinusT<GBase<int>[]>>[], GTArrUArr<IMinusT<GSubT<int>[]>,GBase<int>[]>>(false));
		Eval("Test111", UnboxUToT<IMinusT<IMinusT<GSubT<int>[]>[]>, GTArrUArr<GSubT<int>[],IMinusT<GBase<int>[]>>>(false));
		Eval("Test112", UnboxUToT<IPlusTMinusU<GBase<int>[][],GSubT<int>[][]>, GTArrUArr<GSubT<int>[],GBase<int>[]>>(false));

		Eval("Test201", UnboxUToT<IPlusT<IPlusT<Base>>, GRefTRefU<Sub,Base>>(false));
		Eval("Test202", UnboxUToT<IMinusT<IMinusT<Sub>>, GRefTRefU<Sub,Base>>(false));
		Eval("Test203", UnboxUToT<IPlusTMinusU<IPlusT<Base>,IPlusT<Sub>>, GRefTRefU<Sub,Base>>(false));

		Eval("Test204", UnboxUToT<IPlusT<IPlusT<Base[]>>, GRefTRefU<Sub[],Base[]>>(false));
		Eval("Test205", UnboxUToT<IMinusT<IMinusT<Sub[]>>, GRefTRefU<Sub[],Base[]>>(false));
		Eval("Test206", UnboxUToT<IPlusTMinusU<IPlusT<Base[]>,IPlusT<Sub[]>>, GRefTRefU<Sub[],Base[]>>(false));

		Eval("Test207", UnboxUToT<IPlusT<IPlusT<GBase<int>>>, GRefTRefU<GSubT<int>,GBase<int>>>(false));
		Eval("Test208", UnboxUToT<IMinusT<IMinusT<GSubT<int>>>, GRefTRefU<GSubT<int>,GBase<int>>>(false));
		Eval("Test209", UnboxUToT<IPlusTMinusU<IPlusT<GBase<int>>,IPlusT<GSubT<int>>>, GRefTRefU<GSubT<int>,GBase<int>>>(false));

		Eval("Test210", UnboxUToT<IPlusT<IPlusT<GBase<int>[]>>, GRefTRefU<GSubT<int>[],GBase<int>[]>>(false));
		Eval("Test211", UnboxUToT<IMinusT<IMinusT<GSubT<int>[]>>, GRefTRefU<GSubT<int>[],GBase<int>[]>>(false));
		Eval("Test212", UnboxUToT<IPlusTMinusU<IPlusT<GBase<int>[]>,IPlusT<GSubT<int>[]>>, GRefTRefU<GSubT<int>[],GBase<int>[]>>(false));

		Eval("Test301", UnboxUToT<IPlusT<IPlusT<Base[]>>, GRefTArrRefUArr<Sub,Base>>(false));
		Eval("Test302", UnboxUToT<IMinusT<IMinusT<Sub[]>>, GRefTArrRefUArr<Sub,Base>>(false));
		Eval("Test303", UnboxUToT<IPlusTMinusU<IPlusT<Base[]>,IPlusT<Sub[]>>, GRefTArrRefUArr<Sub,Base>>(false));

		Eval("Test304", UnboxUToT<IPlusT<IPlusT<Base[][]>>, GRefTArrRefUArr<Sub[],Base[]>>(false));
		Eval("Test305", UnboxUToT<IMinusT<IMinusT<Sub[][]>>, GRefTArrRefUArr<Sub[],Base[]>>(false));
		Eval("Test306", UnboxUToT<IPlusTMinusU<IPlusT<Base[][]>,IPlusT<Sub[][]>>, GRefTArrRefUArr<Sub[],Base[]>>(false));

		Eval("Test307", UnboxUToT<IPlusT<IPlusT<GBase<int>[]>>, GRefTArrRefUArr<GSubT<int>,GBase<int>>>(false));
		Eval("Test308", UnboxUToT<IMinusT<IMinusT<GSubT<int>[]>>, GRefTArrRefUArr<GSubT<int>,GBase<int>>>(false));
		Eval("Test309", UnboxUToT<IPlusTMinusU<IPlusT<GBase<int>[]>,IPlusT<GSubT<int>[]>>, GRefTArrRefUArr<GSubT<int>,GBase<int>>>(false));

		Eval("Test310", UnboxUToT<IPlusT<IPlusT<GBase<int>[][]>>, GRefTArrRefUArr<GSubT<int>[],GBase<int>[]>>(false));
		Eval("Test311", UnboxUToT<IMinusT<IMinusT<GSubT<int>[][]>>, GRefTArrRefUArr<GSubT<int>[],GBase<int>[]>>(false));
		Eval("Test312", UnboxUToT<IPlusTMinusU<IPlusT<GBase<int>[][]>,IPlusT<GSubT<int>[][]>>, GRefTArrRefUArr<GSubT<int>[],GBase<int>[]>>(false));
		
		Eval("Test401", UnboxUToT<IPlusT<IPlusT<Base>[]>, GArrRefTArrRefU<Sub,Base>>(false));
		Eval("Test402", UnboxUToT<IMinusT<IMinusT<Sub>[]>, GArrRefTArrRefU<Sub,Base>>(false));
		Eval("Test403", UnboxUToT<IPlusTMinusU<IPlusT<Base>[],IPlusT<Sub>[]>, GArrRefTArrRefU<Sub,Base>>(false));

		Eval("Test404", UnboxUToT<IPlusT<IPlusT<Base[,]>[]>, GArrRefTArrRefU<Sub[,],Base[,]>>(false));
		Eval("Test405", UnboxUToT<IMinusT<IMinusT<Sub[,]>[]>, GArrRefTArrRefU<Sub[,],Base[,]>>(false));
		Eval("Test406", UnboxUToT<IPlusTMinusU<IPlusT<Base[,]>[],IPlusT<Sub[,]>[]>, GArrRefTArrRefU<Sub[,],Base[,]>>(false));

		Eval("Test407", UnboxUToT<IPlusT<IPlusT<GBase<int>>[]>, GArrRefTArrRefU<GSubT<int>,GBase<int>>>(false));
		Eval("Test408", UnboxUToT<IMinusT<IMinusT<GSubT<int>>[]>, GArrRefTArrRefU<GSubT<int>,GBase<int>>>(false));
		Eval("Test409", UnboxUToT<IPlusTMinusU<IPlusT<GBase<int>>[],IPlusT<GSubT<int>>[]>, GArrRefTArrRefU<GSubT<int>,GBase<int>>>(false));

		Eval("Test410", UnboxUToT<IPlusT<IPlusT<GBase<int>[]>[]>, GArrRefTArrRefU<GSubT<int>[],GBase<int>[]>>(false));
		Eval("Test411", UnboxUToT<IMinusT<IMinusT<GSubT<int>[]>[]>, GArrRefTArrRefU<GSubT<int>[],GBase<int>[]>>(false));
		Eval("Test412", UnboxUToT<IPlusTMinusU<IPlusT<GBase<int>[]>[],IPlusT<GSubT<int>[]>[]>, GArrRefTArrRefU<GSubT<int>[],GBase<int>[]>>(false));
		
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
