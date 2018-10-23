// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using TestLibrary;

class AsFunctionPtrTest
{
    [DllImport("PInvoke_Delegate_AsParam")]
    extern static int CommonMethodCalled1();

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static int CommonMethodCalled2();

    delegate int Dele();

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static bool TakeDelegateByValParam([MarshalAs(UnmanagedType.FunctionPtr)]Dele dele);

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static bool TakeDelegateByInValParam([In, MarshalAs(UnmanagedType.FunctionPtr)]Dele dele);

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static bool TakeDelegateByOutValParam([Out, MarshalAs(UnmanagedType.FunctionPtr)]Dele dele);

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static bool TakeDelegateByInOutValParam([In, Out, MarshalAs(UnmanagedType.FunctionPtr)]Dele dele);

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static bool TakeDelegateByRefParam([MarshalAs(UnmanagedType.FunctionPtr)]ref Dele dele);

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static bool TakeDelegateByInRefParam([In, MarshalAs(UnmanagedType.FunctionPtr)]ref Dele dele);

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static bool TakeDelegateByOutRefParam([MarshalAs(UnmanagedType.FunctionPtr)]out Dele dele);

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static bool TakeDelegateByInOutRefParam([In, Out, MarshalAs(UnmanagedType.FunctionPtr)]ref Dele dele);

    [DllImport("PInvoke_Delegate_AsParam")]
    [return: MarshalAs(UnmanagedType.FunctionPtr)]
    extern static Dele ReturnDelegateByVal();

    const int COMMONMETHODCALLED1_RIGHT_RETVAL = 10;
    const int COMMONMETHODCALLED2_RIGHT_RETVAL = 20;

    static int Main(string[] args)
    {
        try{
            Console.WriteLine("Scenario 1 : Delegate marshaled by val with attribute [MarshalAs(UnmanagedType.FunctionPtr)].");
            Dele dele1 = new Dele(CommonMethodCalled1);
            Assert.IsTrue(TakeDelegateByValParam(dele1), "TakeDelegateByValParam");

            Console.WriteLine("\n\nScenario 2 : Delegate marshaled by ref with attribute [MarshalAs(UnmanagedType.FunctionPtr)].");
            Dele dele2 = new Dele(CommonMethodCalled1);
            Assert.IsTrue(TakeDelegateByRefParam(ref dele2), "TakeDelegateByRefParam");
            Assert.AreEqual(COMMONMETHODCALLED2_RIGHT_RETVAL, dele2(), "dele2 is not point to method CommonMethodCalled2() correctly.");

            Console.WriteLine("\n\nScenario 3 : Delegate marshaled by val with attribute [In,MarshalAs(UnmanagedType.FunctionPtr)].");
            Dele dele3 = new Dele(CommonMethodCalled1);
            Assert.IsTrue(TakeDelegateByInValParam(dele3), "TakeDelegateByInValParam");

            Console.WriteLine("\n\nScenario 4 : Delegate marshaled by ref with attribute [In,MarshalAs(UnmanagedType.FunctionPtr)].");
            Dele dele4 = new Dele(CommonMethodCalled1);
            Dele tempDele4 = dele4;
            Assert.IsTrue(TakeDelegateByInRefParam(ref dele4), "TakeDelegateByInRefParam");
            Assert.AreEqual(tempDele4, dele4, "dele4 isnt equal to tempDele4");

            Console.WriteLine("\n\nScenario 5 : Delegate marshaled by val with attribute [Out,MarshalAs(UnmanagedType.FunctionPtr)].");
            Dele dele5 = new Dele(CommonMethodCalled1);
            Assert.IsTrue(TakeDelegateByOutValParam(dele5), "TakeDelegateByOutValParam");

            Console.WriteLine("Scenario 6 : Delegate marshaled by ref with attribute [Out,MarshalAs(UnmanagedType.FunctionPtr)].");
            Dele dele6 = new Dele(CommonMethodCalled1);
            Dele tempDele6 = new Dele(CommonMethodCalled1);
            Assert.IsTrue(TakeDelegateByOutRefParam(out dele6), "TakeDelegateByOutRefParam");
            Assert.AreNotEqual(tempDele6, dele6, "dele6 shouldn't equal to tempDele6");
            Assert.AreEqual(COMMONMETHODCALLED2_RIGHT_RETVAL, dele6(), "dele6 is not point to method CommonMethodCalled2() correctly.");

            Console.WriteLine("\n\nScenario 7 : Delegate marshaled by val with attribute [In,Out,MarshalAs(UnmanagedType.FunctionPtr)].");
            Dele dele7 = new Dele(CommonMethodCalled1);
            Assert.IsTrue(TakeDelegateByInOutValParam(dele7), "TakeDelegateByInOutValParam");
            Assert.IsNotNull(dele7, "dele7 is null");

            Console.WriteLine("\n\nScenario 8 : Delegate marshaled  by ref with attribute [In,Out,MarshalAs(UnmanagedType.FunctionPtr)].");
            Dele dele8 = new Dele(CommonMethodCalled1);
            Assert.IsTrue(TakeDelegateByInOutRefParam(ref dele8), "TakeDelegateByInOutRefParam");
            Assert.AreEqual(COMMONMETHODCALLED2_RIGHT_RETVAL, dele8(), "dele8 is not point to method CommonMethodCalled2() correctly");

            Console.WriteLine("\n\nScenario 9 : return Delegate marshaled by val with attribute [return:MarshalAs(UnmanagedType.FunctionPtr)].");
            Dele dele9 = ReturnDelegateByVal();
            Assert.AreEqual(COMMONMETHODCALLED1_RIGHT_RETVAL, dele9(), "dele9() return wrong value");
            
            return 100;
        } catch (Exception e){
            Console.WriteLine($"Test Failure: {e}"); 
            return 101; 
        }
    }
}
