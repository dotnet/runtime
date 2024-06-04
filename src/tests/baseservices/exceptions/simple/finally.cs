// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Threading;
using Xunit;

public class Finally
{
	[Fact]
	public static int TestEntryPoint()
	{
		Finally f = new Finally();

		TestLibrary.TestFramework.BeginTestCase("Finally blocks");

		if (f.RunTests())
		{
			TestLibrary.TestFramework.EndTestCase();
			TestLibrary.TestFramework.LogInformation("PASS");
			return 100;
		}
		else
		{
			TestLibrary.TestFramework.EndTestCase();
			TestLibrary.TestFramework.LogInformation("FAIL");
			return 0;
		}
	}

	public bool RunTests()
	{
		bool retVal = true;

		TestLibrary.TestFramework.LogInformation("[Positive]");
		retVal = PosTest1() && retVal;
		retVal = PosTest2() && retVal;
		retVal = PosTest3() && retVal;

		return retVal;
	}

	public bool PosTest1()
	{
		bool retVal = true;
		int  stage  = 0;

		TestLibrary.TestFramework.LogInformation("PosTest1: Outer finally");

		try
		{
			stage++;	//1
			try
			{
				stage++;	//2
				throw new ArgumentException();
			}
			catch (ArgumentException)
			{
				if (2 != stage)
				{
					TestLibrary.TestFramework.LogError("000", "Catch block executed in wrong order: Expected(2) Actual("+stage+")");
					retVal = false;
				}
				stage++;	//3
			}
		}
		finally
		{
			if (3 != stage)
			{
				TestLibrary.TestFramework.LogError("000", "Finally block executed in wrong order: Expected(3) Actual("+stage+")");
				retVal = false;
			}
			stage++;	//4
		}

		if (4 != stage)
		{
			TestLibrary.TestFramework.LogError("000", "Finally/Catch block executed too many times: Expected(4) Actual("+stage+")");
			retVal = false;
		}

		return retVal;
	}

	public bool PosTest2()
	{
		bool retVal = true;
		int  stage  = 0;

		TestLibrary.TestFramework.LogInformation("PosTest2: Cascade finally");

		try
		{
			stage++;	//1
			throw new ArgumentException();
		}
		catch (ArgumentException)
		{
			if (1 != stage)
			{
				TestLibrary.TestFramework.LogError("000", "Catch block executed in wrong order: Expected(1) Actual("+stage+")");
				retVal = false;
			}
			stage++;	//2
		}
		finally
		{
			if (2 != stage)
			{
				TestLibrary.TestFramework.LogError("000", "Finally block executed in wrong order: Expected(2) Actual("+stage+")");
				retVal = false;
			}
			stage++;	//3
		}

		if (3 != stage)
		{
			TestLibrary.TestFramework.LogError("000", "Finally/Catch block executed too many times: Expected(3) Actual("+stage+")");
			retVal = false;
		}

		return retVal;
	}

	public bool PosTest3()
	{
		bool retVal = true;
		int  stage  = 0;

		TestLibrary.TestFramework.LogInformation("PosTest2: Nested finally");

		try
		{
			stage++;	//1
			throw new ArgumentException();
		}
		catch (ArgumentException)
		{
			if (1 != stage)
			{
				TestLibrary.TestFramework.LogError("000", "Catch block executed in wrong order: Expected(1) Actual("+stage+")");
				retVal = false;
			}
			stage++;	//2

			try
			{
				stage++;	//3
				throw new ArgumentException();
			}
			catch (ArgumentException)
			{
				if (3 != stage)
				{
					TestLibrary.TestFramework.LogError("000", "Catch block executed in wrong order: Expected(3) Actual("+stage+")");
					retVal = false;
				}
				stage++;	//4
			}
			finally
			{
				if (4 != stage)
				{
					TestLibrary.TestFramework.LogError("000", "Finally block executed in wrong order: Expected(4) Actual("+stage+")");
					retVal = false;
				}
				stage++;	//5
			}
		}
		finally
		{
			if (5 != stage)
			{
				TestLibrary.TestFramework.LogError("000", "Finally block executed in wrong order: Expected(5) Actual("+stage+")");
				retVal = false;
			}
			stage++;	//6
		}

		if (6 != stage)
		{
			TestLibrary.TestFramework.LogError("000", "Finally/Catch block executed too many times: Expected(6) Actual("+stage+")");
			retVal = false;
		}

		return retVal;
	}
}
