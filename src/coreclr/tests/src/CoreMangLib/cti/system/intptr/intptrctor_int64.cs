// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class IntPtrCtor_Int64
{

    public static int Main()
    {
        IntPtrCtor_Int64 testCase = new IntPtrCtor_Int64();

        TestLibrary.TestFramework.BeginTestCase("IntPtr.ctor(Int64)");
        if (testCase.RunTests())
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
        retVal = PosTest1("001", 0) && retVal;
        retVal = PosTest1("002", Int32.MinValue) && retVal;
        retVal = PosTest1("003", Int32.MaxValue) && retVal;
        retVal = PosTest1("004", Int32.MinValue+1) && retVal;
        retVal = PosTest1("005", Int32.MaxValue-1) && retVal;
        retVal = PosTest1("006", TestLibrary.Generator.GetInt32(-55)) && retVal;
       
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1("001", Int64.MaxValue) && retVal;
        retVal = NegTest1("002", Int64.MinValue) && retVal;
        retVal = NegTest1("003", (long)Int32.MinValue - 1) && retVal;        
        retVal = NegTest1("004", (long)Int32.MaxValue + 1) && retVal;

        return retVal;
    }

    /// <summary>
    /// for positive testing 
    /// </summary>
    /// <returns></returns>
    public bool PosTest1(string id, long i)
    {
        bool retVal = true;
        try
        {
            System.IntPtr ip = new IntPtr(i);
            if (ip.ToInt64() != i)
            {
                TestLibrary.TestFramework.LogError(id,
                    String.Format("IntPtr value expect {0}", i));
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError(id, "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    /// <summary>
    /// for negative testing 
    /// </summary>
    /// <returns></returns>
    public bool NegTest1(string id, long i)
    {
        bool retVal = true;
        try
        {
            System.IntPtr ip = new IntPtr(i);
        }
        catch (System.OverflowException ex)
        {
        	if (System.IntPtr.Size == 4)
           {
            // ok, that's what it should be
            return retVal;
           }
		   else
		   	{
		   	  TestLibrary.TestFramework.LogError(id, String.Format("IntPtr should not have thrown an OverflowException for value {0}: ", i) + ex.ToString());
	          return false;
		   	}
        }
        catch (Exception e)
        {
           if (System.IntPtr.Size == 4)
           {
            TestLibrary.TestFramework.LogError(id, "Unexpected exception: " + e);
            retVal = false;
           	}
        }

        // no exception, ERROR!
        if (System.IntPtr.Size == 4)
       	{
	        TestLibrary.TestFramework.LogError(id, String.Format("IntPtr should throw an OverflowException for value {0}", i));
    	    return false;
        }
		else
		{
				return true;
		}
    }

}
