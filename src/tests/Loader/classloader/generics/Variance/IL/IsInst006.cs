// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Base {}
public class Sub : Base {}

public class GBase<T> {}
public class GSubT<T> : GBase<T> {}
	
public struct GTU<T,U> : IPlusT<T>, IMinusT<U>, IPlusTMinusU<T,U> {}
public struct GTArrUArr<T,U> : IPlusT<T[]>, IMinusT<U[]>, IPlusTMinusU<T[],U[]> {}
public struct GRefTRefU<T,U> : IPlusT<IPlusT<T>>, IPlusT<IMinusT<U>>, IPlusTMinusU<IPlusT<T>, IPlusT<U>> {}
public struct GRefTArrRefUArr<T,U> : IPlusT<IPlusT<T[]>>, IPlusT<IMinusT<U[]>>, IPlusTMinusU<IPlusT<T[]>,IPlusT<U[]>> {}
public struct GArrRefTArrRefU<T,U> : IPlusT<IPlusT<T>[]>, IPlusT<IMinusT<U>[]>, IPlusTMinusU<IPlusT<T>[],IPlusT<U>[]> {}

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

	public static bool UIsInstT<T,U>(bool expected)
	{
		try
		{
			return (expected == (Activator.CreateInstance(typeof(U)) is T));
		}
		catch(Exception E)
		{
			Console.WriteLine("Unexpected Exception {0}, with T = {1} and U = {2}", E, typeof(T).Name, typeof(U).Name);
			return false;
		}
	}
	
	private static bool RunTests()
	{
		Eval("Test001", UIsInstT<IPlusT<Sub>, GTU<Base,Sub>>(false));
		Eval("Test002", UIsInstT<IMinusT<Base>, GTU<Base,Sub>>(false));
		Eval("Test003", UIsInstT<IPlusTMinusU<Sub,Base>, GTU<Base,Sub>>(false));

		Eval("Test004", UIsInstT<IPlusT<Sub[]>, GTU<Base[],Sub[]>>(false));
		Eval("Test005", UIsInstT<IMinusT<Base[]>, GTU<Base[],Sub[]>>(false));
		Eval("Test006", UIsInstT<IPlusTMinusU<Sub[],Base[]>, GTU<Base[],Sub[]>>(false));

		Eval("Test007", UIsInstT<IPlusT<GSubT<int>>, GTU<GBase<int>,GSubT<string>>>(false));
		Eval("Test008", UIsInstT<IMinusT<GBase<string>>, GTU<GBase<int>,GSubT<string>>>(false));
		Eval("Test009", UIsInstT<IPlusTMinusU<GSubT<int>,GBase<string>>, GTU<GBase<int>,GSubT<string>>>(false));

		Eval("Test010", UIsInstT<IPlusT<GSubT<int>[]>, GTU<GBase<int>[],GSubT<string>[]>>(false));
		Eval("Test011", UIsInstT<IMinusT<GBase<string>[]>, GTU<GBase<int>[],GSubT<string>[]>>(false));
		Eval("Test012", UIsInstT<IPlusTMinusU<GSubT<int>[],GBase<string>[]>, GTU<GBase<int>[],GSubT<string>[]>>(false));

		Eval("Test101", UIsInstT<IPlusT<Sub[]>, GTArrUArr<Base,Sub>>(false));
		Eval("Test102", UIsInstT<IMinusT<Base[]>, GTArrUArr<Base,Sub>>(false));
		Eval("Test103", UIsInstT<IPlusTMinusU<Sub[],Base[]>, GTArrUArr<Base,Sub>>(false));
		
		Eval("Test104", UIsInstT<IPlusT<Sub[][]>, GTArrUArr<Base[],Sub[]>>(false));
		Eval("Test105", UIsInstT<IMinusT<Base[][]>, GTArrUArr<Base[],Sub[]>>(false));
		Eval("Test106", UIsInstT<IPlusTMinusU<Sub[][],Base[][]>, GTArrUArr<Base[],Sub[]>>(false));

		Eval("Test107", UIsInstT<IPlusT<GSubT<int>[]>, GTArrUArr<GBase<int>,GSubT<string>>>(false));
		Eval("Test108", UIsInstT<IMinusT<GBase<string>[]>, GTArrUArr<GBase<int>,GSubT<string>>>(false));
		Eval("Test109", UIsInstT<IPlusTMinusU<GSubT<int>[],GBase<string>[]>, GTArrUArr<GBase<int>,GSubT<string>>>(false));

		Eval("Test110", UIsInstT<IPlusT<GSubT<int>[][]>, GTArrUArr<GBase<int>[],GSubT<string>[]>>(false));
		Eval("Test111", UIsInstT<IMinusT<GBase<string>[][]>, GTArrUArr<GBase<int>[],GSubT<string>[]>>(false));
		Eval("Test112", UIsInstT<IPlusTMinusU<GSubT<int>[][],GBase<string>[][]>, GTArrUArr<GBase<int>[],GSubT<string>[]>>(false));

		Eval("Test201", UIsInstT<IPlusT<IPlusT<Sub>>, GRefTRefU<Base,Sub>>(false));
		Eval("Test202", UIsInstT<IPlusT<IMinusT<Base>>, GRefTRefU<Base,Sub>>(false));
		Eval("Test203", UIsInstT<IPlusTMinusU<IPlusT<Sub>,IPlusT<Base>>, GRefTRefU<Base,Sub>>(false));

		Eval("Test204", UIsInstT<IPlusT<IPlusT<Sub[]>>, GRefTRefU<Base[],Sub[]>>(false));
		Eval("Test205", UIsInstT<IPlusT<IMinusT<Base[]>>, GRefTRefU<Base[],Sub[]>>(false));
		Eval("Test206", UIsInstT<IPlusTMinusU<IPlusT<Sub[]>,IPlusT<Base[]>>, GRefTRefU<Base[],Sub[]>>(false));

		Eval("Test207", UIsInstT<IPlusT<IPlusT<GSubT<int>>>, GRefTRefU<GBase<int>,GSubT<string>>>(false));
		Eval("Test208", UIsInstT<IPlusT<IMinusT<GBase<string>>>, GRefTRefU<GBase<int>,GSubT<string>>>(false));
		Eval("Test209", UIsInstT<IPlusTMinusU<IPlusT<GSubT<int>>,IPlusT<GBase<string>>>, GRefTRefU<GBase<int>,GSubT<string>>>(false));

		Eval("Test210", UIsInstT<IPlusT<IPlusT<GSubT<int>[]>>, GRefTRefU<GBase<int>[],GSubT<string>[]>>(false));
		Eval("Test211", UIsInstT<IPlusT<IMinusT<GBase<string>[]>>, GRefTRefU<GBase<int>[],GSubT<string>[]>>(false));
		Eval("Test212", UIsInstT<IPlusTMinusU<IPlusT<GSubT<int>[]>,IPlusT<GBase<string>[]>>, GRefTRefU<GBase<int>[],GSubT<string>[]>>(false));

		Eval("Test301", UIsInstT<IPlusT<IPlusT<Sub[]>>, GRefTArrRefUArr<Base,Sub>>(false));
		Eval("Test302", UIsInstT<IPlusT<IMinusT<Base[]>>, GRefTArrRefUArr<Base,Sub>>(false));
		Eval("Test303", UIsInstT<IPlusTMinusU<IPlusT<Sub[]>,IPlusT<Base[]>>, GRefTArrRefUArr<Base,Sub>>(false));

		Eval("Test304", UIsInstT<IPlusT<IPlusT<Sub[][]>>, GRefTArrRefUArr<Base[],Sub[]>>(false));
		Eval("Test305", UIsInstT<IPlusT<IMinusT<Base[][]>>, GRefTArrRefUArr<Base[],Sub[]>>(false));
		Eval("Test306", UIsInstT<IPlusTMinusU<IPlusT<Sub[][]>,IPlusT<Base[][]>>, GRefTArrRefUArr<Base[],Sub[]>>(false));

		Eval("Test307", UIsInstT<IPlusT<IPlusT<GSubT<int>[]>>, GRefTArrRefUArr<GBase<int>,GSubT<string>>>(false));
		Eval("Test308", UIsInstT<IPlusT<IMinusT<GBase<string>[]>>, GRefTArrRefUArr<GBase<int>,GSubT<string>>>(false));
		Eval("Test309", UIsInstT<IPlusTMinusU<IPlusT<GSubT<int>[]>,IPlusT<GBase<string>[]>>, GRefTArrRefUArr<GBase<int>,GSubT<string>>>(false));

		Eval("Test310", UIsInstT<IPlusT<IPlusT<GSubT<int>[][]>>, GRefTArrRefUArr<GBase<int>[],GSubT<string>[]>>(false));
		Eval("Test311", UIsInstT<IPlusT<IMinusT<GBase<string>[][]>>, GRefTArrRefUArr<GBase<int>[],GSubT<string>[]>>(false));
		Eval("Test312", UIsInstT<IPlusTMinusU<IPlusT<GSubT<int>[][]>,IPlusT<GBase<string>[][]>>, GRefTArrRefUArr<GBase<int>[],GSubT<string>[]>>(false));
		
		Eval("Test401", UIsInstT<IPlusT<IPlusT<Sub>[]>, GArrRefTArrRefU<Base,Sub>>(false));
		Eval("Test402", UIsInstT<IPlusT<IMinusT<Base>[]>, GArrRefTArrRefU<Base,Sub>>(false));
		Eval("Test403", UIsInstT<IPlusTMinusU<IPlusT<Sub>[],IPlusT<Base>[]>, GArrRefTArrRefU<Base,Sub>>(false));

		Eval("Test404", UIsInstT<IPlusT<IPlusT<Sub[,]>[]>, GArrRefTArrRefU<Base[,],Sub[,]>>(false));
		Eval("Test405", UIsInstT<IPlusT<IMinusT<Base[,]>[]>, GArrRefTArrRefU<Base[,],Sub[,]>>(false));
		Eval("Test406", UIsInstT<IPlusTMinusU<IPlusT<Sub[,]>[],IPlusT<Base[,]>[]>, GArrRefTArrRefU<Base[,],Sub[,]>>(false));

		Eval("Test407", UIsInstT<IPlusT<IPlusT<GSubT<int>>[]>, GArrRefTArrRefU<GBase<int>,GSubT<string>>>(false));
		Eval("Test408", UIsInstT<IPlusT<IMinusT<GBase<string>>[]>, GArrRefTArrRefU<GBase<int>,GSubT<string>>>(false));
		Eval("Test409", UIsInstT<IPlusTMinusU<IPlusT<GSubT<int>>[],IPlusT<GBase<string>>[]>, GArrRefTArrRefU<GBase<int>,GSubT<string>>>(false));

		Eval("Test410", UIsInstT<IPlusT<IPlusT<GSubT<int>[]>[]>, GArrRefTArrRefU<GBase<int>[],GSubT<string>[]>>(false));
		Eval("Test411", UIsInstT<IPlusT<IMinusT<GBase<string>[]>[]>, GArrRefTArrRefU<GBase<int>[],GSubT<string>[]>>(false));
		Eval("Test412", UIsInstT<IPlusTMinusU<IPlusT<GSubT<int>[]>[],IPlusT<GBase<string>[]>[]>, GArrRefTArrRefU<GBase<int>[],GSubT<string>[]>>(false));
			
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
