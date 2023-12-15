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
		Eval("Test001", UIsInstT<IPlusT<Base>, GTU<Sub,Base>>(true));
		Eval("Test002", UIsInstT<IMinusT<Sub>, GTU<Sub,Base>>(true));
		Eval("Test003", UIsInstT<IPlusTMinusU<Base,Sub>, GTU<Sub,Base>>(true));

		Eval("Test004", UIsInstT<IPlusT<Base[]>, GTU<Sub[],Base[]>>(true));
		Eval("Test005", UIsInstT<IMinusT<Sub[]>, GTU<Sub[],Base[]>>(true));
		Eval("Test006", UIsInstT<IPlusTMinusU<Base[],Sub[]>, GTU<Sub[],Base[]>>(true));

		Eval("Test007", UIsInstT<IPlusT<GBase<int>>, GTU<GSubT<int>,GBase<string>>>(true));
		Eval("Test008", UIsInstT<IMinusT<GSubT<string>>, GTU<GSubT<int>,GBase<string>>>(true));
		Eval("Test009", UIsInstT<IPlusTMinusU<GBase<int>,GSubT<string>>, GTU<GSubT<int>,GBase<string>>>(true));

		Eval("Test010", UIsInstT<IPlusT<GBase<int>[]>, GTU<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test011", UIsInstT<IMinusT<GSubT<string>[]>, GTU<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test012", UIsInstT<IPlusTMinusU<GBase<int>[],GSubT<string>[]>, GTU<GSubT<int>[],GBase<string>[]>>(true));

		Eval("Test101", UIsInstT<IPlusT<Base[]>, GTArrUArr<Sub,Base>>(true));
		Eval("Test102", UIsInstT<IMinusT<Sub[]>, GTArrUArr<Sub,Base>>(true));
		Eval("Test103", UIsInstT<IPlusTMinusU<Base[],Sub[]>, GTArrUArr<Sub,Base>>(true));
		
		Eval("Test104", UIsInstT<IPlusT<Base[][]>, GTArrUArr<Sub[],Base[]>>(true));
		Eval("Test105", UIsInstT<IMinusT<Sub[][]>, GTArrUArr<Sub[],Base[]>>(true));
		Eval("Test106", UIsInstT<IPlusTMinusU<Base[][],Sub[][]>, GTArrUArr<Sub[],Base[]>>(true));

		Eval("Test107", UIsInstT<IPlusT<GBase<int>[]>, GTArrUArr<GSubT<int>,GBase<string>>>(true));
		Eval("Test108", UIsInstT<IMinusT<GSubT<string>[]>, GTArrUArr<GSubT<int>,GBase<string>>>(true));
		Eval("Test109", UIsInstT<IPlusTMinusU<GBase<int>[],GSubT<string>[]>, GTArrUArr<GSubT<int>,GBase<string>>>(true));

		Eval("Test110", UIsInstT<IPlusT<GBase<int>[][]>, GTArrUArr<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test111", UIsInstT<IMinusT<GSubT<string>[][]>, GTArrUArr<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test112", UIsInstT<IPlusTMinusU<GBase<int>[][],GSubT<string>[][]>, GTArrUArr<GSubT<int>[],GBase<string>[]>>(true));

		Eval("Test201", UIsInstT<IPlusT<IPlusT<Base>>, GRefTRefU<Sub,Base>>(true));
		Eval("Test202", UIsInstT<IPlusT<IMinusT<Sub>>, GRefTRefU<Sub,Base>>(true));
		Eval("Test203", UIsInstT<IPlusTMinusU<IPlusT<Base>,IPlusT<Sub>>, GRefTRefU<Sub,Base>>(true));

		Eval("Test204", UIsInstT<IPlusT<IPlusT<Base[]>>, GRefTRefU<Sub[],Base[]>>(true));
		Eval("Test205", UIsInstT<IPlusT<IMinusT<Sub[]>>, GRefTRefU<Sub[],Base[]>>(true));
		Eval("Test206", UIsInstT<IPlusTMinusU<IPlusT<Base[]>,IPlusT<Sub[]>>, GRefTRefU<Sub[],Base[]>>(true));

		Eval("Test207", UIsInstT<IPlusT<IPlusT<GBase<int>>>, GRefTRefU<GSubT<int>,GBase<string>>>(true));
		Eval("Test208", UIsInstT<IPlusT<IMinusT<GSubT<string>>>, GRefTRefU<GSubT<int>,GBase<string>>>(true));
		Eval("Test209", UIsInstT<IPlusTMinusU<IPlusT<GBase<int>>,IPlusT<GSubT<string>>>, GRefTRefU<GSubT<int>,GBase<string>>>(true));

		Eval("Test210", UIsInstT<IPlusT<IPlusT<GBase<int>[]>>, GRefTRefU<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test211", UIsInstT<IPlusT<IMinusT<GSubT<string>[]>>, GRefTRefU<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test212", UIsInstT<IPlusTMinusU<IPlusT<GBase<int>[]>,IPlusT<GSubT<string>[]>>, GRefTRefU<GSubT<int>[],GBase<string>[]>>(true));

		Eval("Test301", UIsInstT<IPlusT<IPlusT<Base[]>>, GRefTArrRefUArr<Sub,Base>>(true));
		Eval("Test302", UIsInstT<IPlusT<IMinusT<Sub[]>>, GRefTArrRefUArr<Sub,Base>>(true));
		Eval("Test303", UIsInstT<IPlusTMinusU<IPlusT<Base[]>,IPlusT<Sub[]>>, GRefTArrRefUArr<Sub,Base>>(true));

		Eval("Test304", UIsInstT<IPlusT<IPlusT<Base[][]>>, GRefTArrRefUArr<Sub[],Base[]>>(true));
		Eval("Test305", UIsInstT<IPlusT<IMinusT<Sub[][]>>, GRefTArrRefUArr<Sub[],Base[]>>(true));
		Eval("Test306", UIsInstT<IPlusTMinusU<IPlusT<Base[][]>,IPlusT<Sub[][]>>, GRefTArrRefUArr<Sub[],Base[]>>(true));

		Eval("Test307", UIsInstT<IPlusT<IPlusT<GBase<int>[]>>, GRefTArrRefUArr<GSubT<int>,GBase<string>>>(true));
		Eval("Test308", UIsInstT<IPlusT<IMinusT<GSubT<string>[]>>, GRefTArrRefUArr<GSubT<int>,GBase<string>>>(true));
		Eval("Test309", UIsInstT<IPlusTMinusU<IPlusT<GBase<int>[]>,IPlusT<GSubT<string>[]>>, GRefTArrRefUArr<GSubT<int>,GBase<string>>>(true));

		Eval("Test310", UIsInstT<IPlusT<IPlusT<GBase<int>[][]>>, GRefTArrRefUArr<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test311", UIsInstT<IPlusT<IMinusT<GSubT<string>[][]>>, GRefTArrRefUArr<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test312", UIsInstT<IPlusTMinusU<IPlusT<GBase<int>[][]>,IPlusT<GSubT<string>[][]>>, GRefTArrRefUArr<GSubT<int>[],GBase<string>[]>>(true));
		
		Eval("Test401", UIsInstT<IPlusT<IPlusT<Base>[]>, GArrRefTArrRefU<Sub,Base>>(true));
		Eval("Test402", UIsInstT<IPlusT<IMinusT<Sub>[]>, GArrRefTArrRefU<Sub,Base>>(true));
		Eval("Test403", UIsInstT<IPlusTMinusU<IPlusT<Base>[],IPlusT<Sub>[]>, GArrRefTArrRefU<Sub,Base>>(true));

		Eval("Test404", UIsInstT<IPlusT<IPlusT<Base[,]>[]>, GArrRefTArrRefU<Sub[,],Base[,]>>(true));
		Eval("Test405", UIsInstT<IPlusT<IMinusT<Sub[,]>[]>, GArrRefTArrRefU<Sub[,],Base[,]>>(true));
		Eval("Test406", UIsInstT<IPlusTMinusU<IPlusT<Base[,]>[],IPlusT<Sub[,]>[]>, GArrRefTArrRefU<Sub[,],Base[,]>>(true));

		Eval("Test407", UIsInstT<IPlusT<IPlusT<GBase<int>>[]>, GArrRefTArrRefU<GSubT<int>,GBase<string>>>(true));
		Eval("Test408", UIsInstT<IPlusT<IMinusT<GSubT<string>>[]>, GArrRefTArrRefU<GSubT<int>,GBase<string>>>(true));
		Eval("Test409", UIsInstT<IPlusTMinusU<IPlusT<GBase<int>>[],IPlusT<GSubT<string>>[]>, GArrRefTArrRefU<GSubT<int>,GBase<string>>>(true));

		Eval("Test410", UIsInstT<IPlusT<IPlusT<GBase<int>[]>[]>, GArrRefTArrRefU<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test411", UIsInstT<IPlusT<IMinusT<GSubT<string>[]>[]>, GArrRefTArrRefU<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test412", UIsInstT<IPlusTMinusU<IPlusT<GBase<int>[]>[],IPlusT<GSubT<string>[]>[]>, GArrRefTArrRefU<GSubT<int>[],GBase<string>[]>>(true));
			
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
