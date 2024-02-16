// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Base {}
public class Sub : Base {}

public class GBase<T> {}
public class GSubT<T> : GBase<T> {}
	
public class GTU<T,U> : IPlusT<T>, IMinusT<U>, IMinusTPlusU<T,U> {}
public class GTArrUArr<T,U> : IMinusT<T[]>, IPlusT<U[]>, IMinusTPlusU<T[],U[]> {}
public class GRefTRefU<T,U> : IMinusT<IPlusT<T>>, IMinusT<IMinusT<U>>, IMinusTPlusU<IPlusT<T>, IPlusT<U>> {}
public class GRefTArrRefUArr<T,U> : IMinusT<IPlusT<T[]>>, IMinusT<IMinusT<U[]>>, IMinusTPlusU<IPlusT<T[]>,IPlusT<U[]>> {}
public class GArrRefTArrRefU<T,U> : IMinusT<IPlusT<T>[]>, IMinusT<IMinusT<U>[]>, IMinusTPlusU<IPlusT<T>[],IPlusT<U>[]> {}

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
		Eval("Test001", UIsInstT<IPlusT<IMinusT<Base>>, GTU<IMinusT<Sub>,Base>>(false));
		Eval("Test002", UIsInstT<IMinusT<IMinusT<Sub>>, GTU<Sub,IMinusT<Base>>>(false));
		Eval("Test003", UIsInstT<IPlusTMinusU<Base,Sub>, GTU<Sub,Base>>(false));

		Eval("Test004", UIsInstT<IPlusT<IMinusT<Base[]>>, GTU<IMinusT<Sub[]>,Base>>(false));
		Eval("Test005", UIsInstT<IMinusT<IMinusT<Sub[]>>, GTU<Sub,IMinusT<Base[]>>>(false));
		Eval("Test006", UIsInstT<IPlusTMinusU<Base[],Sub[]>, GTU<Sub[],Base[]>>(false));

		Eval("Test007", UIsInstT<IPlusT<IMinusT<GBase<int>>>, GTU<IMinusT<GSubT<int>>,GBase<int>>>(false));
		Eval("Test008", UIsInstT<IMinusT<IMinusT<GSubT<int>>>, GTU<GSubT<int>,IMinusT<GBase<int>>>>(false));
		Eval("Test009", UIsInstT<IPlusTMinusU<GBase<int>,GSubT<int>>, GTU<GSubT<int>,GBase<int>>>(false));

		Eval("Test010", UIsInstT<IPlusT<IMinusT<GBase<int>[]>>, GTU<IMinusT<GSubT<int>[]>,GBase<int>[]>>(false));
		Eval("Test011", UIsInstT<IMinusT<IMinusT<GSubT<int>[]>>, GTU<GSubT<int>[],IMinusT<GBase<int>[]>>>(false));
		Eval("Test012", UIsInstT<IPlusTMinusU<GBase<int>[],GSubT<int>[]>, GTU<GSubT<int>[],GBase<int>[]>>(false));

		Eval("Test101", UIsInstT<IPlusT<IMinusT<Base>[]>, GTArrUArr<IMinusT<Sub>,Base>>(false));
		Eval("Test102", UIsInstT<IMinusT<IMinusT<Sub>[]>, GTArrUArr<Sub,IMinusT<Base>>>(false));
		Eval("Test103", UIsInstT<IPlusTMinusU<Base[],Sub[]>, GTArrUArr<Sub,Base>>(false));

		Eval("Test104", UIsInstT<IPlusT<IMinusT<Base[]>[]>, GTArrUArr<IMinusT<Sub[]>,Base>>(false));
		Eval("Test105", UIsInstT<IMinusT<IMinusT<Sub[]>[]>, GTArrUArr<Sub,IMinusT<Base[]>>>(false));
		Eval("Test106", UIsInstT<IPlusTMinusU<Base[][],Sub[][]>, GTArrUArr<Sub[],Base[]>>(false));

		Eval("Test107", UIsInstT<IPlusT<IMinusT<GBase<int>>[]>, GTArrUArr<IMinusT<GSubT<int>>,GBase<int>>>(false));
		Eval("Test108", UIsInstT<IMinusT<IMinusT<GSubT<int>[]>>, GTArrUArr<GSubT<int>,IMinusT<GBase<int>>>>(false));
		Eval("Test109", UIsInstT<IPlusTMinusU<GBase<int>,GSubT<int>>, GTArrUArr<GSubT<int>,GBase<int>>>(false));

		Eval("Test110", UIsInstT<IPlusT<IMinusT<GBase<int>[]>>[], GTArrUArr<IMinusT<GSubT<int>[]>,GBase<int>[]>>(false));
		Eval("Test111", UIsInstT<IMinusT<IMinusT<GSubT<int>[]>[]>, GTArrUArr<GSubT<int>[],IMinusT<GBase<int>[]>>>(false));
		Eval("Test112", UIsInstT<IPlusTMinusU<GBase<int>[][],GSubT<int>[][]>, GTArrUArr<GSubT<int>[],GBase<int>[]>>(false));

		Eval("Test201", UIsInstT<IPlusT<IPlusT<Base>>, GRefTRefU<Sub,Base>>(false));
		Eval("Test202", UIsInstT<IMinusT<IMinusT<Sub>>, GRefTRefU<Sub,Base>>(false));
		Eval("Test203", UIsInstT<IPlusTMinusU<IPlusT<Base>,IPlusT<Sub>>, GRefTRefU<Sub,Base>>(false));

		Eval("Test204", UIsInstT<IPlusT<IPlusT<Base[]>>, GRefTRefU<Sub[],Base[]>>(false));
		Eval("Test205", UIsInstT<IMinusT<IMinusT<Sub[]>>, GRefTRefU<Sub[],Base[]>>(false));
		Eval("Test206", UIsInstT<IPlusTMinusU<IPlusT<Base[]>,IPlusT<Sub[]>>, GRefTRefU<Sub[],Base[]>>(false));

		Eval("Test207", UIsInstT<IPlusT<IPlusT<GBase<int>>>, GRefTRefU<GSubT<int>,GBase<int>>>(false));
		Eval("Test208", UIsInstT<IMinusT<IMinusT<GSubT<int>>>, GRefTRefU<GSubT<int>,GBase<int>>>(false));
		Eval("Test209", UIsInstT<IPlusTMinusU<IPlusT<GBase<int>>,IPlusT<GSubT<int>>>, GRefTRefU<GSubT<int>,GBase<int>>>(false));

		Eval("Test210", UIsInstT<IPlusT<IPlusT<GBase<int>[]>>, GRefTRefU<GSubT<int>[],GBase<int>[]>>(false));
		Eval("Test211", UIsInstT<IMinusT<IMinusT<GSubT<int>[]>>, GRefTRefU<GSubT<int>[],GBase<int>[]>>(false));
		Eval("Test212", UIsInstT<IPlusTMinusU<IPlusT<GBase<int>[]>,IPlusT<GSubT<int>[]>>, GRefTRefU<GSubT<int>[],GBase<int>[]>>(false));

		Eval("Test301", UIsInstT<IPlusT<IPlusT<Base[]>>, GRefTArrRefUArr<Sub,Base>>(false));
		Eval("Test302", UIsInstT<IMinusT<IMinusT<Sub[]>>, GRefTArrRefUArr<Sub,Base>>(false));
		Eval("Test303", UIsInstT<IPlusTMinusU<IPlusT<Base[]>,IPlusT<Sub[]>>, GRefTArrRefUArr<Sub,Base>>(false));

		Eval("Test304", UIsInstT<IPlusT<IPlusT<Base[][]>>, GRefTArrRefUArr<Sub[],Base[]>>(false));
		Eval("Test305", UIsInstT<IMinusT<IMinusT<Sub[][]>>, GRefTArrRefUArr<Sub[],Base[]>>(false));
		Eval("Test306", UIsInstT<IPlusTMinusU<IPlusT<Base[][]>,IPlusT<Sub[][]>>, GRefTArrRefUArr<Sub[],Base[]>>(false));

		Eval("Test307", UIsInstT<IPlusT<IPlusT<GBase<int>[]>>, GRefTArrRefUArr<GSubT<int>,GBase<int>>>(false));
		Eval("Test308", UIsInstT<IMinusT<IMinusT<GSubT<int>[]>>, GRefTArrRefUArr<GSubT<int>,GBase<int>>>(false));
		Eval("Test309", UIsInstT<IPlusTMinusU<IPlusT<GBase<int>[]>,IPlusT<GSubT<int>[]>>, GRefTArrRefUArr<GSubT<int>,GBase<int>>>(false));

		Eval("Test310", UIsInstT<IPlusT<IPlusT<GBase<int>[][]>>, GRefTArrRefUArr<GSubT<int>[],GBase<int>[]>>(false));
		Eval("Test311", UIsInstT<IMinusT<IMinusT<GSubT<int>[][]>>, GRefTArrRefUArr<GSubT<int>[],GBase<int>[]>>(false));
		Eval("Test312", UIsInstT<IPlusTMinusU<IPlusT<GBase<int>[][]>,IPlusT<GSubT<int>[][]>>, GRefTArrRefUArr<GSubT<int>[],GBase<int>[]>>(false));
		
		Eval("Test401", UIsInstT<IPlusT<IPlusT<Base>[]>, GArrRefTArrRefU<Sub,Base>>(false));
		Eval("Test402", UIsInstT<IMinusT<IMinusT<Sub>[]>, GArrRefTArrRefU<Sub,Base>>(false));
		Eval("Test403", UIsInstT<IPlusTMinusU<IPlusT<Base>[],IPlusT<Sub>[]>, GArrRefTArrRefU<Sub,Base>>(false));

		Eval("Test404", UIsInstT<IPlusT<IPlusT<Base[,]>[]>, GArrRefTArrRefU<Sub[,],Base[,]>>(false));
		Eval("Test405", UIsInstT<IMinusT<IMinusT<Sub[,]>[]>, GArrRefTArrRefU<Sub[,],Base[,]>>(false));
		Eval("Test406", UIsInstT<IPlusTMinusU<IPlusT<Base[,]>[],IPlusT<Sub[,]>[]>, GArrRefTArrRefU<Sub[,],Base[,]>>(false));

		Eval("Test407", UIsInstT<IPlusT<IPlusT<GBase<int>>[]>, GArrRefTArrRefU<GSubT<int>,GBase<int>>>(false));
		Eval("Test408", UIsInstT<IMinusT<IMinusT<GSubT<int>>[]>, GArrRefTArrRefU<GSubT<int>,GBase<int>>>(false));
		Eval("Test409", UIsInstT<IPlusTMinusU<IPlusT<GBase<int>>[],IPlusT<GSubT<int>>[]>, GArrRefTArrRefU<GSubT<int>,GBase<int>>>(false));

		Eval("Test410", UIsInstT<IPlusT<IPlusT<GBase<int>[]>[]>, GArrRefTArrRefU<GSubT<int>[],GBase<int>[]>>(false));
		Eval("Test411", UIsInstT<IMinusT<IMinusT<GSubT<int>[]>[]>, GArrRefTArrRefU<GSubT<int>[],GBase<int>[]>>(false));
		Eval("Test412", UIsInstT<IPlusTMinusU<IPlusT<GBase<int>[]>[],IPlusT<GSubT<int>[]>[]>, GArrRefTArrRefU<GSubT<int>[],GBase<int>[]>>(false));
			
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
