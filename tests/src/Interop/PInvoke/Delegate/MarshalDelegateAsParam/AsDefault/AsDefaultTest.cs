// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using TestLibrary;

class AsDefaultTest
{
    [DllImport("PInvoke_Delegate_AsParam")]
    extern static int CommonMethodCalled1();

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static int CommonMethodCalled2();

    delegate int Dele();

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static bool TakeDelegateByValParam(Dele dele);

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static bool TakeDelegateByRefParam(ref Dele dele);
    [DllImport("PInvoke_Delegate_AsParam")]
    extern static bool TakeDelegateByInValParam([In] Dele dele);

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static bool TakeDelegateByInRefParam([In] ref Dele dele);

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static bool TakeDelegateByOutValParam([Out] Dele dele);

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static bool TakeDelegateByOutRefParam(out Dele dele);

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static bool TakeDelegateByInOutValParam([In, Out] Dele dele);

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static bool TakeDelegateByInOutRefParam([In, Out] ref Dele dele);

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static Dele ReturnDelegateByVal();

    #region "Auxiliary Verification Value"
    const int COMMONMETHODCALLED1_RIGHT_RETVAL = 10;
    const int COMMONMETHODCALLED2_RIGHT_RETVAL = 20;
    #endregion

    static int Main(string[] args)
    {
        try{
            Console.WriteLine("Scenario 1 : Delegate marshaled by val with default attribute.");
            Dele dele1 = new Dele(CommonMethodCalled1);
            Assert.IsTrue(TakeDelegateByValParam(dele1), "The return value is wrong in TakeDelegateByValParam.");

            Console.WriteLine("\n\nScenario 2 : Delegate marshaled by ref with default attribute.");
            Dele dele2 = new Dele(CommonMethodCalled1);
            Assert.IsTrue(TakeDelegateByRefParam(ref dele2), "Call on Native side");
            Console.WriteLine("\n\tCalling method CommonMethodCalled2() on the managed side...");
            Assert.AreEqual(COMMONMETHODCALLED2_RIGHT_RETVAL, dele2(), "Now dele2 point to method CommonMethodCalled2()");

            Console.WriteLine("\n\nScenario 3 : Delegate marshaled by val with default attribute.");
            Dele dele3 = new Dele(CommonMethodCalled1);
            Dele tempDele3 = dele3;
            Assert.IsTrue(TakeDelegateByInValParam(dele3), "Calling method CommonMethodCalled1() on the native side...");
            Assert.AreEqual<Dele>(tempDele3, dele3, "Delegate marshaled by val with default attribute.");

            Console.WriteLine("\n\nScenario 4 : Delegate marshaled by ref with default attribute.");
            Dele dele4 = new Dele(CommonMethodCalled1);
            Dele tempDele4 = dele4;
            Assert.IsTrue(TakeDelegateByInRefParam(ref dele4), "Calling method CommonMethodCalled1() on the native side");
            Assert.AreEqual<Dele>(tempDele4, dele4, "Delegate marshaled by val with default attribute.");

            Console.WriteLine("\n\nScenario 5 : Delegate marshaled by val with default attribute.");
            Dele dele5 = new Dele(CommonMethodCalled1);
            Assert.IsTrue(TakeDelegateByOutValParam(dele5), "Calling method CommonMethodCalled1() on the native side");
            Assert.AreEqual(COMMONMETHODCALLED1_RIGHT_RETVAL, dele5(), "The Delegate is wrong");

            Console.WriteLine("\n\nScenario 6 : Delegate marshaled by ref with default attribute.");
            Dele dele6 = new Dele(CommonMethodCalled1);
            Dele tempDele6 = new Dele(CommonMethodCalled1);
            Assert.IsTrue(TakeDelegateByOutRefParam(out dele6), "TakeDelegateByOutRefParam");
            Assert.AreEqual(COMMONMETHODCALLED2_RIGHT_RETVAL, dele6(), "Delegate marshaled by ref with default attribute");

            Console.WriteLine("\n\nScenario 7 : Delegate marshaled by val with default attribute.");
            Dele dele7 = new Dele(CommonMethodCalled1);
            Assert.IsTrue(TakeDelegateByInOutValParam(dele7), "TakeDelegateByInOutValParam");
            Assert.IsNotNull(dele7, "The variable dele7 is null!");

            Console.WriteLine("\n\nScenario 8 : Delegate marshaled  by ref with default attribute.");
            Dele dele8 = new Dele(CommonMethodCalled1);
            Assert.IsTrue(TakeDelegateByInOutRefParam(ref dele8), "TakeDelegateByInOutRefParam");
            Assert.AreEqual(COMMONMETHODCALLED2_RIGHT_RETVAL, dele8(), "dele8 is not point to method CommonMethodCalled2() correctly");

            Console.WriteLine("\n\nScenario 9 : return Delegate marshaled by val with default attribute.");
            Dele dele9 = ReturnDelegateByVal();
            Assert.AreEqual(COMMONMETHODCALLED1_RIGHT_RETVAL, dele9(), "return Delegate marshaled by val with default attribute");

            return 100;
        } catch (Exception e){
            Console.WriteLine($"Test Failure: {e}"); 
            return 101; 
        }
    }
}
