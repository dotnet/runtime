// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Security;
using System.Runtime.InteropServices;
using TestLibrary;


public class Test
{
    const int iNative = 11;//the value passed from Native side to Managed side
    const int iManaged = 10;//The value passed from Managed side to Native side

    #region "TestMethod"
    #region "TestMethod1: PInvoke,cdecl"
    //TestMethod1:Pinvoke,Cdecl
    [DllImport("RefIntNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool MarshalRefInt_Cdcel( [In, Out]ref int ri );

    
    private static bool TestMethod_PInvke_Cdecl()
    {
        TestFramework.BeginScenario("PInvoke,Cdecl");
        
        bool breturn = true;
        try
        {
            int i = 10;
            if (!MarshalRefInt_Cdcel(ref i))
            {
                breturn = false;
                TestFramework.LogError("001", "MarshalRefInt_Cdcel:The Input(From Managed To Native) is wrong");
            }

            if (iNative != i)
            {
                breturn = false;
                TestFramework.LogError("002", "MarshalRefInt_Stdcall:The value(i) hasnt changed by Native");
            }
        }
        catch (Exception e)
        {
            breturn = false;
            TestFramework.LogError("010", "UnExpected Exception" + e.ToString());
        }
        return breturn;
    }
    #endregion

    #region "TestMethod2 Pinvoke,stdcall"
    //TestMethod2:Pinvoke,stdcall
    [DllImport("RefIntNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool MarshalRefInt_Stdcall( [In, Out]ref int ri );

    
    private static bool TestMethod_PInvke_Stdcall()
    {
        TestFramework.BeginScenario("PInvoke,Stdcall");

        bool breturn = true;
        try
        {
            int i = 10;
            if (!MarshalRefInt_Stdcall(ref i))
            {
                breturn = false;
                TestFramework.LogError("003", "MarshalRefInt_Stdcall:The Input(From Managed To Native) is wrong");
            }

            if (iNative != i)
            {
                breturn = false;
                TestFramework.LogError("004", "MarshalRefInt_Stdcall:The value(i) hasnt changed by Native");
            }
        }
        catch (Exception e)
        {
            breturn = false;
            TestFramework.LogError("010", "UnExpected Exception" + e.ToString());
        }
        return breturn;
    }
    #endregion

    #region "TestMethod3,Reverse PInvoke,Cdecl"
    //TestMethod3,Reverse PInvoke,Cdecl
    [DllImport("RefIntNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool DoCallBack_MarshalRefInt_Cdecl( Cdeclcaller caller );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool Cdeclcaller( [In, Out]ref int ri );

    
    
    private static bool TestMethod_Cdecl( ref int ri )
    {
        TestFramework.BeginScenario("Reverse PInvoke,cdecl");
        bool breturn = true;

        try
        {
            //Check the Input
            if (ri != iNative)
            {
                breturn = false;
                TestFramework.LogError("005", "TestMethod_Cdcel:The reference paramter value is wrong!");
            }
            //Change the Value
            ri = iManaged;
        }
        catch (Exception e)
        {
            breturn = false;
            TestFramework.LogError("010", "UnExpected Exception" + e.ToString());
        }
        return breturn;
    }
    #endregion

    #region "TestMethod4,Reverse PInvoke,StdCall"
    [DllImport("RefIntNative", CallingConvention = CallingConvention.StdCall)]
    public static extern bool DoCallBack_MarshalRefInt_Stdcall( Stdcallcaller caller );

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool Stdcallcaller( [In, Out]ref int ri );

    
    
    private static bool TestMethod_Stdcall( ref int ri )
    {
        TestFramework.BeginScenario("Reverse PInvoke,Stdcall");
        bool breturn = true;
        try
        {
            //Check the Input
            if (ri != iNative)
            {
                breturn = false;
                TestFramework.LogError("006", "TestMethod_Stdcall:The reference paramter value is wrong!");
            }
            //Change the Value
            ri = iManaged;
        }
        catch (Exception e)
        {
            breturn = false;
            TestFramework.LogError("010", "UnExpected Exception" + e.ToString());
        }
        return breturn;
    }
    #endregion

    #region "TestMethod5,cdecl,delegate pinvoke"
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool DelegatePInvokeCdecl( [In, Out]ref int ri );

    [DllImport("RefIntNative", CallingConvention = CallingConvention.Cdecl)]
    public static extern DelegatePInvokeCdecl MarshalRefInt_DelegatePInvoke_Cdecl();

    
    private static bool TestMethod_DelegatePInvokeCdecl()
    {
        TestFramework.BeginScenario("Delegate Pinvoke Cdecl");

        bool breturn = true;
        try
        {
            DelegatePInvokeCdecl caller = MarshalRefInt_DelegatePInvoke_Cdecl();
            int i = 10;
            if (!caller(ref i))
            {
                breturn = false;
                TestFramework.LogError("007", "TestMethod_DelegatePInvokeCdecl:The return value is wrong(Managed Side)");
            }

            if (iNative != i)
            {
                breturn = false;
                TestFramework.LogError("008", "TestMethod_DelegatePInvokeCdecl:The value(i) hasnt changed by Native");
            }
        }
        catch (Exception e)
        {
            breturn = false;
            TestFramework.LogError("010", "UnExpected Exception" + e.ToString());
        }
        return breturn;
    }
    #endregion

    #region "TestMethod6: StdCall,delegate pinvoke"
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate bool DelegatePInvokeStdcall( [In, Out]ref int ri );

    [DllImport("RefIntNative", CallingConvention = CallingConvention.StdCall)]
    public static extern DelegatePInvokeStdcall MarshalRefInt_DelegatePInvoke_StdCall();

    
    private static bool TestMethod_DelegatePInvokeStdcall()
    {
        TestFramework.BeginScenario("Delegate Pinvoke stdcall");

        bool breturn = true;
        try
        {
            DelegatePInvokeStdcall caller = MarshalRefInt_DelegatePInvoke_StdCall();
            int i = 10;
            if (!caller(ref i))
            {
                breturn = false;
                TestFramework.LogError("009", "TestMethod_DelegatePInvokeStdcall:The return value is wrong(Managed Side)");
            }

            if (iNative != i)
            {
                breturn = false;
                TestFramework.LogError("010", "TestMethod_DelegatePInvokeStdcall:The value(i) hasnt changed by Native");
            }
        }
        catch (Exception e)
        {
            breturn = false;
            TestFramework.LogError("010", "UnExpected Exception" + e.ToString());
        }
        return breturn;
    }
    #endregion
    #endregion

    
    static int Main()
    {
        bool bReturn = true;

        //TestMethod1:Pinvoke,Cdecl
        bReturn = bReturn && TestMethod_PInvke_Cdecl();

        //TestMethod2:Pinvoke,stdcall
        bReturn = bReturn && TestMethod_PInvke_Stdcall();

        //TestMethod3:Reverse Pinvoke,cdecl
        bReturn = bReturn && DoCallBack_MarshalRefInt_Cdecl(new Cdeclcaller(TestMethod_Cdecl));

        //TestMethod4: Reverse Pinvoke,Stdcall
        bReturn = bReturn && DoCallBack_MarshalRefInt_Stdcall(new Stdcallcaller(TestMethod_Stdcall));

        //TestMethod5:Delegate Pinvoke Cdecl
        bReturn = bReturn && TestMethod_DelegatePInvokeCdecl();

        //TestMethod6:Delegate Pinvoke stdcall
        bReturn = bReturn && TestMethod_DelegatePInvokeStdcall();

        if(bReturn)
        {
            Console.WriteLine("Succeed!");
        }
        else
        {
            Console.WriteLine("Failed!");
        }
        return bReturn ? 100 : 101;
    }
}
