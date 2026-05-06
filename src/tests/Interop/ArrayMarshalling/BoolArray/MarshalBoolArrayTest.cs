// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//

//
//  Adding tests for BoolArrayMarshaler code coverage
//
//Rule for Passing Value
//        Reverse Pinvoke
//M--->N  true,true,true,true,true
//N----M  true,false,true,false,true
using System;
using System.Text;
using System.Security;
using System.Runtime.InteropServices;
using TestLibrary;
using Xunit;

[ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
public class MarshalBoolArray
{
    #region"variable"
    const int SIZE = 5;
    #endregion

    #region "Reverse PInvoke"

    #region "Bool Array"
    [DllImport("MarshalBoolArrayNative")]
    private static extern bool DoCallBackIn(CallBackIn callback);
    private delegate bool CallBackIn([In]int size, [In, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I1, SizeConst = SIZE)] bool[] array);
    private static bool TestMethod_CallBackIn(int size, bool[] array)
    {
        bool retVal = true;

        //Check the Input
        if (SIZE != size)
        {
            retVal = false;
            //TestFramework.LogError("001","Failed on the Managed Side:TestMethod_CallBackIn:Parameter Size is wrong");
        }
        for (int i = 0; i < SIZE; ++i) //Reverse PInvoke, true,false,true false,true
        {
            if ((0 == i % 2) && !array[i])
            {
                retVal = false;
                //TestFramework.LogError("002","Failed on the Managed Side:TestMethod_CallBackIn. The " + (i + 1) + "st Item failed");
            }
            else if ((1 == i % 2) && array[i])
            {
                retVal = false;
                //TestFramework.LogError("003","Failed on the Managed Side:TestMethod_CallBackIn. The " + (i + 1) + "st Item failed");
            }
        }
        return retVal;
    }

    [DllImport("MarshalBoolArrayNative")]
    private static extern bool DoCallBackOut(CallBackOut callback);
    private delegate bool CallBackOut([In]int size, [Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1, SizeConst = SIZE)] bool[] array);
    private static bool TestMethod_CallBackOut(int size, bool[] array)
    {
        bool retVal = true;
        //Check the Input
        if (SIZE != size)
        {
            retVal = false;
            //TestFramework.LogError("004","Failed on the Managed Side:TestMethod_CallBackOut:Parameter Size is wrong");
        }

        for (int i = 0; i < SIZE; ++i) //Reverse PInvoke, true,true,true true,true
        {
            array[i] = true;
        }
        return retVal;
    }

    [DllImport("MarshalBoolArrayNative")]
    private static extern bool DoCallBackInOut(CallBackInOut callback);
    private delegate bool CallBackInOut([In]int size, [In, Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I1, SizeConst = SIZE)] bool[] array);

    private static bool TestMethod_CallBackInOut(int size, bool[] array)
    {
        bool retVal = true;
        //Check the Input
        if (SIZE != size)
        {
            retVal = false;
            //TestFramework.LogError("005","Failed on the Managed Side:TestMethod_CallBackInOut:Parameter Size is wrong");
        }
        for (int i = 0; i < SIZE; ++i) //Reverse PInvoke, true,false,true false,true
        {
            if ((0 == i % 2) && !array[i])
            {
                retVal = false;
                TestFramework.LogError("006","Failed on the Managed Side:TestMethod_CallBackInOut. The " + (i + 1) + "st Item failed");
            }
            else if ((1 == i % 2) && array[i])
            {
                retVal = false;
                //TestFramework.LogError("007","Failed on the Managed Side:TestMethod_CallBackInOut. The " + (i + 1) + "st Item failed");
            }
        }

        //Check the output
        for (int i = 0; i < size; ++i) //Reverse PInvoke, true,true,true true,true
        {
            array[i] = true;
        }
        return retVal;
    }
    #endregion

    #region"Bool Array Reference"
    [DllImport("MarshalBoolArrayNative")]
    private static extern bool DoCallBackRefIn(CallBackRefIn callback);
    private delegate bool CallBackRefIn([In]int size, [In, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1)] ref bool[] array);

    private static bool TestMethod_CallBackRefIn(int size, ref bool[] array)
    {
        bool retVal = true;
        //Check the Input
        if (SIZE != size)
        {
            retVal = false;
            //TestFramework.LogError("008","Failed on the Managed Side:TestMethod_CallBackRefIn:Parameter Size is wrong");
        }
        //TODO: UnComment these line if the SizeConst attributes is support
        //Since now the sizeconst doesnt support on ref,so only check the first item instead.
        //Unhandled Exception: System.Runtime.InteropServices.MarshalDirectiveException: Cannot marshal 'parameter #2': Cannot use SizeParamIndex for ByRef array parameters.
        //for (int i = 0; i < size; ++i) //Reverse PInvoke, true,false,true false,true
        //{
        //    if ((0 == i % 2) && !array[i])
        //    {
        //      ReportFailure("Failed on the Managed Side:TestMethod_CallBackRefIn. The " + (i + 1) + "st Item failed", true.ToString(), false.ToString());
        //    }
        //    else if ((1 == i % 2) && array[i])
        //    {
        //     ReportFailure("Failed on the Managed Side:TestMethod_CallBackRefIn. The " + (i + 1) + "st Item failed", false.ToString(), true.ToString());
        //    }
        //  }
        if (!array[0])
        {
            retVal = false;
            //TestFramework.LogError("009","Failed on the Managed Side:TestMethod_CallBackRefIn. The first Item failed");
        }
        return retVal;
    }



    [DllImport("MarshalBoolArrayNative")]
    private static extern bool DoCallBackRefOut(CallBackRefOut callback);
    private delegate bool CallBackRefOut([In]int size, [Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I1)] out bool[] array);

    private static bool TestMethod_CallBackRefOut(int size, out bool[] array)
    {
        bool retVal = true;

        //Check the Input
        if (size != SIZE)
        {
            retVal = false;
            //TestFramework.LogError("010","Failed on the Managed Side:TestMethod_CallBackRefOut:Parameter Size is wrong");
        }

        array = new bool[SIZE];
        for (int i = 0; i < SIZE; ++i) //Reverse PInvoke, true,true,true true,true
        {
            array[i] = true;
        }
        return retVal;
    }

    [DllImport("MarshalBoolArrayNative")]
    private static extern bool DoCallBackRefInOut(CallBackRefInOut callback);
    private delegate bool CallBackRefInOut([In]int size, [In, Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U1)] ref bool[] array);

    private static bool TestMethod_CallBackRefInOut(int size, ref bool[] array)
    {
        bool retVal = true;
        //Check the Input
        if (SIZE != size)
        {
            retVal = false;
            //TestFramework.LogError("011","Failed on the Managed Side:TestMethod_CallBackRefInOut:Parameter Size is wrong");
        }
        //TODO: UnComment these line if the SizeConst attributes is support
        //Since now the sizeconst doesnt support on ref,so only check the first item instead.
        //Unhandled Exception: System.Runtime.InteropServices.MarshalDirectiveException: Cannot marshal 'parameter #2': Cannot use SizeParamIndex for ByRef array parameters.
        //for (int i = 0; i < size; ++i) //Reverse PInvoke, true,false,true false,true
        //{
        //    if ((0 == i % 2) && !array[i])
        //    {
        //        ReportFailure("Failed on the Managed Side:TestMethod_CallBackRefInOut. The " + (i + 1) + "st Item failed", true.ToString(), false.ToString());
        //    }
        //    else if ((1 == i % 2) && array[i])
        //    {
        //        ReportFailure("Failed on the Managed Side:TestMethod_CallBackRefInOut. The " + (i + 1) + "st Item failed", false.ToString(), true.ToString());
        //    }
        //  }
        if (!array[0])
        {
            retVal = false;
            //TestFramework.LogError("012","Failed on the Managed Side:TestMethod_CallBackRefInOut. The first Item failed");
        }

        //Output
        array = new bool[SIZE];
        for (int i = 0; i < size; ++i) //Reverse PInvoke, true,true,true true,true
        {
            array[i] = true;
        }
        return retVal;
    }
    #endregion

    #endregion

    [System.Security.SecuritySafeCritical]
    [Fact]
    [OuterLoop]
    public static void TestEntryPoint()
    {
        bool retVal = true;

        //TestFramework.BeginScenario("Reverse PInvoke with In attribute");
        if (!DoCallBackIn(new CallBackIn(TestMethod_CallBackIn)))
        {
            retVal = false;
            //TestFramework.LogError("013","Error happens in Native side:DoCallBackIn");
        }

        //TestFramework.BeginScenario("Reverse PInvoke with Out attribute");
        if (!DoCallBackOut(new CallBackOut(TestMethod_CallBackOut)))
        {
            retVal = false;
            //TestFramework.LogError("014","Error happens in Native side:DoCallBackOut");
        }

       // TestFramework.BeginScenario("Reverse PInvoke with InOut attribute");
        if (!DoCallBackInOut(new CallBackInOut(TestMethod_CallBackInOut)))
        {
            retVal = false;
            TestFramework.LogError("015","Error happens in Native side:DoCallBackInOut");
        }

       // TestFramework.BeginScenario("Reverse PInvoke Reference In");
        if (!DoCallBackRefIn(new CallBackRefIn(TestMethod_CallBackRefIn)))
        {
            retVal = false;
            //TestFramework.LogError("016","Error happens in Native side:DoCallBackRefIn");
        }

       // TestFramework.BeginScenario("Reverse PInvoke Reference Out");
        if (!DoCallBackRefOut(new CallBackRefOut(TestMethod_CallBackRefOut)))
        {
            retVal = false;
            //TestFramework.LogError("017","Error happens in Native side:DoCallBackRefOut");
        }

        //TestFramework.BeginScenario("Reverse PInvoke Reference InOut");
        if (!DoCallBackRefInOut(new CallBackRefInOut(TestMethod_CallBackRefInOut)))
        {
            retVal = false;
            //TestFramework.LogError("019","Error happens in Native side:DoCallBackRefInOut");
        }

        if(!retVal)
        {
          throw new Exception("Failed");
        }
    }
}
