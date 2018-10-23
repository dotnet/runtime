// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using TestLibrary;

class AsInterfaceTest
{
    public delegate void Dele();

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static bool Take_DelegatePtrByValParam([MarshalAs(UnmanagedType.Interface)] Dele dele);

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static bool Take_DelegatePtrByRefParam([MarshalAs(UnmanagedType.Interface)] ref Dele dele);

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static bool Take_DelegatePtrByInValParam([In, MarshalAs(UnmanagedType.Interface)] Dele dele);

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static bool Take_DelegatePtrByInRefParam([In, MarshalAs(UnmanagedType.Interface)] ref Dele dele);

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static bool Take_DelegatePtrByOutValParam([Out, MarshalAs(UnmanagedType.Interface)] Dele dele);

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static bool Take_DelegatePtrByOutRefParam([Out, MarshalAs(UnmanagedType.Interface)]out Dele dele, [MarshalAs(UnmanagedType.Interface)] Dele deleHelper);

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static bool Take_DelegatePtrByInOutValParam([In, Out, MarshalAs(UnmanagedType.Interface)] Dele dele);

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static bool Take_DelegatePtrByInOutRefParam([In, Out, MarshalAs(UnmanagedType.Interface)] ref Dele dele);

    [DllImport("PInvoke_Delegate_AsParam")]
    [return: MarshalAs(UnmanagedType.Interface)]
    extern static Dele ReturnDelegatePtrByVal([MarshalAs(UnmanagedType.Interface)] Dele dele);

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static int RetFieldResult1();

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static int RetFieldResult2();

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static int RetFieldResult3();

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static void CommonMethod1();

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static void CommonMethod2();

    [DllImport("PInvoke_Delegate_AsParam")]
    extern static void CommonMethod3();

    const int COMMONMETHOD1_RESULT = 10;
    const int COMMONMETHOD2_RESULT = 20;
    const int COMMONMETHOD3_RESULT = 30;

    static int Main()
    {
        try{
            Console.WriteLine("Scenario 1 : Delegate marshaled by val with attribute [MarshalAs(UnmanagedType.Interface)].");
            Dele dele1 = new Dele(CommonMethod1);
            dele1 += CommonMethod2;
            dele1 += CommonMethod3;
            Assert.IsTrue(Take_DelegatePtrByValParam(dele1), "Take_DelegatePtrByValParam");

            Console.WriteLine("\n\nScenario 2 : Delegate marshaled by ref with attribute [MarshalAs(MarshalAs(UnmanagedType.Interface)].");
            Dele dele2 = new Dele(CommonMethod1);
            dele2 += CommonMethod2;
            dele2 += CommonMethod3;
            Assert.IsTrue(Take_DelegatePtrByRefParam(ref dele2), "Take_DelegatePtrByRefParam");
            Assert.IsNull( dele2, "dele2 should equal to null");

            Console.WriteLine("\n\nScenario 3 : Delegate marshaled by val with attribute [In,MarshalAs(UnmanagedType.Interface)].");
            Dele dele3 = new Dele(CommonMethod1);
            dele3 += CommonMethod2;
            dele3 += CommonMethod3;
            Assert.IsTrue(Take_DelegatePtrByInValParam(dele3), "Take_DelegatePtrByInValParam");

            Console.WriteLine("\n\nScenario 4 : Delegate marshaled by ref with attribute [In,MarshalAs(UnmanagedType.Interface)].");
            Dele dele4 = new Dele(CommonMethod1);
            dele4 += CommonMethod2;
            dele4 += CommonMethod3;
            Assert.IsTrue(Take_DelegatePtrByInRefParam(ref dele4), "Take_DelegatePtrByInRefParam");
            Assert.IsNotNull(dele4, "dele4 does't set to null correctly.");

            Console.WriteLine("\n\nScenario 5 : Delegate marshaled by val with attribute [Out,MarshalAs(UnmanagedType.Interface)].");
            Dele dele5 = new Dele(CommonMethod1);
            dele5 += CommonMethod2;
            dele5 += CommonMethod3;
            Assert.IsTrue(Take_DelegatePtrByOutValParam(dele5), "Take_DelegatePtrByOutValParam");
            Assert.IsNotNull(dele5, "dele5 does't set to null correctly");

            Console.WriteLine("\n\nScenario 6 : Delegate marshaled by ref with attribute [Out,MarshalAs(UnmanagedType.Interface)].");
            Dele dele6 = null;
            Dele deleHelper = new Dele(CommonMethod1);
            deleHelper += CommonMethod2;
            Assert.IsTrue(Take_DelegatePtrByOutRefParam(out dele6, deleHelper), "Take_DelegatePtrByOutRefParam");
            dele6();
            Assert.AreEqual(COMMONMETHOD1_RESULT, RetFieldResult1(), "RetFieldResult1 return value is wrong");
            Assert.AreEqual(COMMONMETHOD2_RESULT, RetFieldResult2(), "RetFieldResult2 return value is wrong ");

            Console.WriteLine("\n\nScenario 7 : Delegate marshaled by val with attribute [In,OutMarshalAs(UnmanagedType.Interface)].");
            Dele dele7 = new Dele(CommonMethod1);
            dele7 += CommonMethod2;
            dele7 += CommonMethod3;
            Assert.IsTrue(Take_DelegatePtrByInOutValParam(dele7), "Take_DelegatePtrByInOutValParam");

            Console.WriteLine("\n\nScenario 8 : Delegate marshaled by ref with attribute [In,OutMarshalAs(MarshalAs(UnmanagedType.Interface)].");
            Dele dele8 = new Dele(CommonMethod1);
            dele8 += CommonMethod2;
            dele8 += CommonMethod3;
            Assert.IsTrue(Take_DelegatePtrByInOutRefParam(ref dele8), "Take_DelegatePtrByInOutRefParam");
            Assert.IsTrue(dele8 == null, "dele8 does't set to null correctly.");

            Console.WriteLine("\n\nScenario 9 : return Delegate marshaled by val with attribute [return:MarshalAs(UnmanagedType.Interface)].");
            Dele dele9 = new Dele(CommonMethod1);
            dele9 += CommonMethod2;
            dele9 += CommonMethod3;
            Dele tempDele = ReturnDelegatePtrByVal(dele9);
            tempDele();
            Assert.AreEqual(COMMONMETHOD1_RESULT, RetFieldResult1(), "RetFieldResult1() return value is wrong");
            Assert.AreEqual(COMMONMETHOD2_RESULT, RetFieldResult2(), "RetFieldResult2() return value is wrong");
            Assert.AreEqual(COMMONMETHOD3_RESULT, RetFieldResult3(), "RetFieldResult3() return value is wrong");

            return 100;
        } catch (Exception e){
            Console.WriteLine($"Test Failure: {e}"); 
            return 101; 
        }
    }
}
