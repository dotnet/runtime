// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Security;
using System.Runtime.InteropServices;
using TestLibrary;
using Xunit;

public class Test_RefCharArrayTest
{
    const int LEN = 10;

    #region "Imported Func"
    //TestMethod1:Pinovke,Cdecl
    [DllImport("RefCharArrayNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalRefCharArray_Cdecl( ref char[] arr );

    //TestMethod2:Pinovke,Stdcall
    [DllImport("RefCharArrayNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalRefCharArray_Stdcall( ref char[] arr );

    //TestMethod3,ReversePinvoke,Cdecl
    [DllImport("RefCharArrayNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalRefCharArray_Cdecl( CdeclCallBack caller );

    //TestMethod4,ReversePinvoke,Stdcall
    [DllImport("RefCharArrayNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalRefCharArray_Stdcall( StdCallBack caller );

    //TestMethod5,DelegatePInvoke,Cdecl
    [DllImport("RefCharArrayNative", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegatePInvokeCdecl DelegatePinvoke_Cdecl();

    //TestMethod6,DelegatePinvoke,Stdcall
    [DllImport("RefCharArrayNative", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    public static extern DelegatePInvokeStdcall DelegatePinvoke_Stdcall();
    #endregion

    #region "TestMethod"
    //TestMethod1,Pinvoke,Cdecl

    static bool TestMethod_PInvoke_Cdecl()
    {
        TestFramework.BeginScenario("Pinvoke,Cdecl");

        bool bresult = true;
        try
        {
            char[] pCharArray = new char[LEN];
            for (int i = 0; i < LEN; i++)
            {
                pCharArray[i] = (char)( 'a' + i );
            }
            if (!MarshalRefCharArray_Cdecl(ref pCharArray))
            {
                bresult = false;
                TestFramework.LogError("001", "MarshalRefCharArray_Cdecl:The Input(From Managed To Native) is wrong");
            }

            if ('z' != pCharArray[0])
            {
                bresult = false;
                TestFramework.LogError("002", "MarshalRefCharArray_Cdecl:The value hasnt changed");
            }
        }
        catch (Exception e)
        {
            bresult = false;
            TestFramework.LogError("e01", "Unexpected Exception" + e.ToString());
        }
        return bresult;
    }

    //TestMethod2,Pinvoke,StdCall

    static bool TestMethod_PInvoke_StdCall()
    {
        TestFramework.BeginScenario("Pinvoke,StdCall");

        bool bresult = true;
        try
        {
            char[] pCharArray = new char[LEN];
            for (int i = 0; i < LEN; i++)
            {
                pCharArray[i] = (char)( 'a' + i );
            }
            if (!MarshalRefCharArray_Stdcall(ref pCharArray))
            {
                bresult = false;
                TestFramework.LogError("003", "MarshalRefCharArray_Stdcall:The Input(From Managed To Native) is wrong");
            }

            if ('z' != pCharArray[0])
            {
                bresult = false;
                TestFramework.LogError("004", "MarshalRefCharArray_Stdcall:The value hasnt changed");
            }
        }
        catch (Exception e)
        {
            bresult = false;
            TestFramework.LogError("e02", "Unexpected Exception" + e.ToString());
        }
        return bresult;
    }

    //TestMethod3:ReversePinvoke,Cdecl
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool CdeclCallBack( ref char[] arr );


    public static bool TestMethod_MarshalRefCharArray_Cdecl( ref char[] arr )
    {
        TestFramework.BeginScenario("Pinvoke,StdCall");

        bool bresult = true;
        try
        {
            //Check input
            if ('z' != arr[0])
            {
                bresult = false;
                TestFramework.LogError("005", "TestMethod_MarshalRefCharArray_Cdecl:Managed Side");
            }
            //Change value
            arr[0] = (char)( 'a' );
        }
        catch (Exception e)
        {
            bresult = false;
            TestFramework.LogError("e03", "Unexpected Exception" + e.ToString());

        }
        return bresult;
    }

    //TestMethod4: ReversePinvoke,Stdcall
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool StdCallBack( ref char[] arr );



    public static bool TestMethod_MarshalRefCharArray_Stdcall( ref char[] arr )
    {
        TestFramework.BeginScenario("ReversePinvoke,Stdcall");

        bool bresult = true;
        try
        {
            //Check input
            if ('z' != arr[0])
            {
                bresult = false;
                TestFramework.LogError("006", "TestMethod_MarshalRefCharArray_StdCall:Managed Side");
            }
            //Change value
            arr[0] = (char)( 'a' );
        }
        catch (Exception e)
        {
            bresult = false;
            TestFramework.LogError("e04", "Unexpected Exception" + e.ToString());
        }
        return bresult;
    }

    //TestMethod5: DelegatePinvoke,Cdecl
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelegatePInvokeCdecl( ref char[] arr );


    private static bool TestMethod_DelegatePInvokeCdecl()
    {
        TestFramework.BeginScenario("DelegatePinvoke,Cdecl");

        bool bresult = true;
        try
        {
            char[] p = new char[LEN];
            for (int i = 0; i < LEN; i++)
            {
                p[i] = (char)( 'a' + i );
            }

            DelegatePInvokeCdecl caller = DelegatePinvoke_Cdecl();
            if (!caller(ref p))
            {
                bresult = false;
                TestFramework.LogError("007", "TestMethod_DelegatePInvokeCdecl:The return value is wrong");
            }
            if ('z' != p[0])
            {
                bresult = false;
                TestFramework.LogError("008", "TestMethod_DelegatePInvokeCdecl:The value hasnt changed");
            }
        }
        catch (Exception e)
        {
            bresult = false;
            TestFramework.LogError("e05", "Unexpected Exception" + e.ToString());
        }
        return bresult;
    }

    //TestMethod6: DelegatePinvoke,Stdcall
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool DelegatePInvokeStdcall( ref char[] arr );


    private static bool TestMethod_DelegatePInvokeStdcall()
    {
        TestFramework.BeginScenario("DelegatePinvoke,Cdecl");

        bool bresult = true;
        try
        {
            char[] p = new char[LEN];
            for (int i = 0; i < LEN; i++)
            {
                p[i] = (char)( 'a' + i );
            }

            DelegatePInvokeStdcall caller = DelegatePinvoke_Stdcall();
            if (!caller(ref p))
            {
                bresult = false;
                TestFramework.LogError("009", "TestMethod_DelegatePInvokeStdcall:The return value is wrong");
            }
            if ('z' != p[0])
            {
                bresult = false;
                TestFramework.LogError("010", "TestMethod_DelegatePInvokeStdcall:The value hasnt changed");
            }
        }
        catch (Exception e)
        {
            bresult = false;
            TestFramework.LogError("e06", "Unexpected Exception" + e.ToString());
        }
        return bresult;
    }

    #endregion


    [Fact]
    [OuterLoop]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static int TestEntryPoint()
    {
        bool bresult = true;

        //TestMethod1,Pinvoke,Cdecl
        bresult = bresult && TestMethod_PInvoke_Cdecl();

        //TestMethod2,PInvoke,StdCall
        bresult = bresult && TestMethod_PInvoke_StdCall();

        //TestMethod3,ReversePInvoke,Cdecl
        bresult = bresult && DoCallBack_MarshalRefCharArray_Cdecl(new CdeclCallBack(TestMethod_MarshalRefCharArray_Cdecl));

        //TestMethod4,ReversePinvoke,Stdcall
        bresult = bresult && DoCallBack_MarshalRefCharArray_Stdcall(new StdCallBack(TestMethod_MarshalRefCharArray_Stdcall));

        //TestMethod5,DelegatePinvoke,Cdecl
        bresult = bresult && TestMethod_DelegatePInvokeCdecl();

        //TestMethod6,DelegatePInvoke,Stdcall
        bresult = bresult && TestMethod_DelegatePInvokeStdcall();

        if (bresult)
        {
            Console.WriteLine("Success!");
        }
        else
        {
            Console.WriteLine("Failed!");
        }
        return bresult ? 100 : 101;
    }
}
