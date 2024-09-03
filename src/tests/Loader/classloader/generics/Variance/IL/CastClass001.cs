// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Base {}
public class Sub : Base {}

public class GBase<T> { }
public class GSubT<T> : GBase<T> { }

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
	private static void CastClassUToTInternalIPlusT<T,U>(U u)
	{
		IPlusT<T> t = (IPlusT<T>) u;
	}

	private static void CastClassUToTInternalIMinusT<T,U>(U u)
	{
		IMinusT<T> t = (IMinusT<T>) u;
	}

	private static void CastClassVToITUInternalIPlusTMinusU<T,U,V>(V v)
	{
		IPlusTMinusU<T,U> t = (IPlusTMinusU<T,U>) v;
	}
		
	private static void CastClassUToTWrapperIPlusT<T,U>()
	{
		CastClassUToTInternalIPlusT<T,U>((U)Activator.CreateInstance(typeof(U)));
	}

	private static void CastClassUToTWrapperIMinusT<T,U>()
	{
		CastClassUToTInternalIMinusT<T,U>((U)Activator.CreateInstance(typeof(U)));
	}

	private static void CastClassUToTWrapper<T,U,V>()
	{
		CastClassVToITUInternalIPlusTMinusU<T,U,V>((V)Activator.CreateInstance(typeof(V)));
	}

	
	public static bool CastClassUToTIPlusT<T,U>(bool expected)
	{
		try
		{
			CastClassUToTWrapperIPlusT<T,U>();
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

	public static bool CastClassUToTIMinusT<T,U>(bool expected)
	{
		try
		{
			CastClassUToTWrapperIMinusT<T,U>();
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

	public static bool CastClassUToT<T,U,V>(bool expected)
	{
		try
		{
			CastClassUToTWrapper<T,U,V>();
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
		Eval("Test001", CastClassUToTIPlusT<Base, GTU<Sub,Base>>(true));
		Eval("Test002", CastClassUToTIMinusT<Sub, GTU<Sub,Base>>(true));
		Eval("Test003", CastClassUToT<Base,Sub, GTU<Sub,Base>>(true));

		Eval("Test004", CastClassUToTIPlusT<Base[], GTU<Sub[],Base[]>>(true));
		Eval("Test005", CastClassUToTIMinusT<Sub[], GTU<Sub[],Base[]>>(true));
		Eval("Test006", CastClassUToT<Base[],Sub[], GTU<Sub[],Base[]>>(true));

		Eval("Test007", CastClassUToTIPlusT<GBase<int>, GTU<GSubT<int>,GBase<string>>>(true));
		Eval("Test008", CastClassUToTIMinusT<GSubT<string>, GTU<GSubT<int>,GBase<string>>>(true));
		Eval("Test009", CastClassUToT<GBase<int>,GSubT<string>, GTU<GSubT<int>,GBase<string>>>(true));

		Eval("Test010", CastClassUToTIPlusT<GBase<int>[], GTU<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test011", CastClassUToTIMinusT<GSubT<string>[], GTU<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test012", CastClassUToT<GBase<int>[],GSubT<string>[], GTU<GSubT<int>[],GBase<string>[]>>(true));

		Eval("Test101", CastClassUToTIPlusT<Base[], GTArrUArr<Sub,Base>>(true));
		Eval("Test102", CastClassUToTIMinusT<Sub[], GTArrUArr<Sub,Base>>(true));
		Eval("Test103", CastClassUToT<Base[],Sub[], GTArrUArr<Sub,Base>>(true));
		
		Eval("Test104", CastClassUToTIPlusT<Base[][], GTArrUArr<Sub[],Base[]>>(true));
		Eval("Test105", CastClassUToTIMinusT<Sub[][], GTArrUArr<Sub[],Base[]>>(true));
		Eval("Test106", CastClassUToT<Base[][],Sub[][], GTArrUArr<Sub[],Base[]>>(true));

		Eval("Test107", CastClassUToTIPlusT<GBase<int>[], GTArrUArr<GSubT<int>,GBase<string>>>(true));
		Eval("Test108", CastClassUToTIMinusT<GSubT<string>[], GTArrUArr<GSubT<int>,GBase<string>>>(true));
		Eval("Test109", CastClassUToT<GBase<int>[],GSubT<string>[], GTArrUArr<GSubT<int>,GBase<string>>>(true));

		Eval("Test110", CastClassUToTIPlusT<GBase<int>[][], GTArrUArr<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test111", CastClassUToTIMinusT<GSubT<string>[][], GTArrUArr<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test112", CastClassUToT<GBase<int>[][],GSubT<string>[][], GTArrUArr<GSubT<int>[],GBase<string>[]>>(true));

		Eval("Test201", CastClassUToTIPlusT<IPlusT<Base>, GRefTRefU<Sub,Base>>(true));
		Eval("Test202", CastClassUToTIPlusT<IMinusT<Sub>, GRefTRefU<Sub,Base>>(true));
		Eval("Test203", CastClassUToT<IPlusT<Base>,IPlusT<Sub>, GRefTRefU<Sub,Base>>(true));

		Eval("Test204", CastClassUToTIPlusT<IPlusT<Base[]>, GRefTRefU<Sub[],Base[]>>(true));
		Eval("Test205", CastClassUToTIPlusT<IMinusT<Sub[]>, GRefTRefU<Sub[],Base[]>>(true));
		Eval("Test206", CastClassUToT<IPlusT<Base[]>,IPlusT<Sub[]>, GRefTRefU<Sub[],Base[]>>(true));

		Eval("Test207", CastClassUToTIPlusT<IPlusT<GBase<int>>, GRefTRefU<GSubT<int>,GBase<string>>>(true));
		Eval("Test208", CastClassUToTIPlusT<IMinusT<GSubT<string>>, GRefTRefU<GSubT<int>,GBase<string>>>(true));
		Eval("Test209", CastClassUToT<IPlusT<GBase<int>>,IPlusT<GSubT<string>>, GRefTRefU<GSubT<int>,GBase<string>>>(true));

		Eval("Test210", CastClassUToTIPlusT<IPlusT<GBase<int>[]>, GRefTRefU<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test211", CastClassUToTIPlusT<IMinusT<GSubT<string>[]>, GRefTRefU<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test212", CastClassUToT<IPlusT<GBase<int>[]>,IPlusT<GSubT<string>[]>, GRefTRefU<GSubT<int>[],GBase<string>[]>>(true));

		Eval("Test301", CastClassUToTIPlusT<IPlusT<Base[]>, GRefTArrRefUArr<Sub,Base>>(true));
		Eval("Test302", CastClassUToTIPlusT<IMinusT<Sub[]>, GRefTArrRefUArr<Sub,Base>>(true));
		Eval("Test303", CastClassUToT<IPlusT<Base[]>,IPlusT<Sub[]>, GRefTArrRefUArr<Sub,Base>>(true));

		Eval("Test304", CastClassUToTIPlusT<IPlusT<Base[][]>, GRefTArrRefUArr<Sub[],Base[]>>(true));
		Eval("Test305", CastClassUToTIPlusT<IMinusT<Sub[][]>, GRefTArrRefUArr<Sub[],Base[]>>(true));
		Eval("Test306", CastClassUToT<IPlusT<Base[][]>,IPlusT<Sub[][]>, GRefTArrRefUArr<Sub[],Base[]>>(true));

		Eval("Test307", CastClassUToTIPlusT<IPlusT<GBase<int>[]>, GRefTArrRefUArr<GSubT<int>,GBase<string>>>(true));
		Eval("Test308", CastClassUToTIPlusT<IMinusT<GSubT<string>[]>, GRefTArrRefUArr<GSubT<int>,GBase<string>>>(true));
		Eval("Test309", CastClassUToT<IPlusT<GBase<int>[]>,IPlusT<GSubT<string>[]>, GRefTArrRefUArr<GSubT<int>,GBase<string>>>(true));

		Eval("Test310", CastClassUToTIPlusT<IPlusT<GBase<int>[][]>, GRefTArrRefUArr<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test311", CastClassUToTIPlusT<IMinusT<GSubT<string>[][]>, GRefTArrRefUArr<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test312", CastClassUToT<IPlusT<GBase<int>[][]>,IPlusT<GSubT<string>[][]>, GRefTArrRefUArr<GSubT<int>[],GBase<string>[]>>(true));
		
		Eval("Test401", CastClassUToTIPlusT<IPlusT<Base>[], GArrRefTArrRefU<Sub,Base>>(true));
		Eval("Test402", CastClassUToTIPlusT<IMinusT<Sub>[], GArrRefTArrRefU<Sub,Base>>(true));
		Eval("Test403", CastClassUToT<IPlusT<Base>[],IPlusT<Sub>[], GArrRefTArrRefU<Sub,Base>>(true));

		Eval("Test404", CastClassUToTIPlusT<IPlusT<Base[,]>[], GArrRefTArrRefU<Sub[,],Base[,]>>(true));
		Eval("Test405", CastClassUToTIPlusT<IMinusT<Sub[,]>[], GArrRefTArrRefU<Sub[,],Base[,]>>(true));
		Eval("Test406", CastClassUToT<IPlusT<Base[,]>[],IPlusT<Sub[,]>[], GArrRefTArrRefU<Sub[,],Base[,]>>(true));

		Eval("Test407", CastClassUToTIPlusT<IPlusT<GBase<int>>[], GArrRefTArrRefU<GSubT<int>,GBase<string>>>(true));
		Eval("Test408", CastClassUToTIPlusT<IMinusT<GSubT<string>>[], GArrRefTArrRefU<GSubT<int>,GBase<string>>>(true));
		Eval("Test409", CastClassUToT<IPlusT<GBase<int>>[],IPlusT<GSubT<string>>[], GArrRefTArrRefU<GSubT<int>,GBase<string>>>(true));

		Eval("Test410", CastClassUToTIPlusT<IPlusT<GBase<int>[]>[], GArrRefTArrRefU<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test411", CastClassUToTIPlusT<IMinusT<GSubT<string>[]>[], GArrRefTArrRefU<GSubT<int>[],GBase<string>[]>>(true));
		Eval("Test412", CastClassUToT<IPlusT<GBase<int>[]>[],IPlusT<GSubT<string>[]>[], GArrRefTArrRefU<GSubT<int>[],GBase<string>[]>>(true));

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
