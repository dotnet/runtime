// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using TestLibrary;

class AsDefaultTest
{
    [DllImport("PInvoke_Delegate_AsField")]
    extern static int CommonMethod();

    [DllImport("PInvoke_Delegate_AsField")]
    extern static bool TakeDelegateAsFieldInStruct_Seq(Struct2_FuncPtrAsField1_Seq s);

    [DllImport("PInvoke_Delegate_AsField")]
    extern static bool TakeDelegateAsFieldInStruct_Exp(Struct2_FuncPtrAsField2_Exp s);

    [DllImport("PInvoke_Delegate_AsField")]
    extern static bool TakeDelegateAsFieldInClass_Seq(Class2_FuncPtrAsField3_Seq s);

    [DllImport("PInvoke_Delegate_AsField", EntryPoint = "TakeDelegateAsFieldInClass_Seq")]
    extern static bool TakeDelegateAsFieldInPreMarshalledClass_Seq(IntPtr ptr);

    [DllImport("PInvoke_Delegate_AsField")]
    extern static bool TakeDelegateAsFieldInClass_Exp(Class2_FuncPtrAsField4_Exp s);

    static int Main()
    {
        try{
            Console.WriteLine("Scenario 1 : Delegate marshaled as field in struct with Sequential.");
            Struct2_FuncPtrAsField1_Seq s = new Struct2_FuncPtrAsField1_Seq();
            s.verification = true;
            s.dele = new Dele(CommonMethod);
            Assert.IsTrue(TakeDelegateAsFieldInStruct_Seq(s), "Delegate marshaled as field in struct with Sequential.");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("\n\nScenario 2 : Delegate marshaled as field in struct with Explicit.");
                Struct2_FuncPtrAsField2_Exp s2 = new Struct2_FuncPtrAsField2_Exp();
                s2.verification = true;
                s2.dele = new Dele(CommonMethod);
                Assert.IsTrue(TakeDelegateAsFieldInStruct_Exp(s2), "Delegate marshaled as field in struct with Explicit");
            }

            Console.WriteLine("\n\nScenario 3 : Delegate marshaled as field in class with Sequential.");
            Class2_FuncPtrAsField3_Seq c3 = new Class2_FuncPtrAsField3_Seq();
            c3.verification = true;
            c3.dele = new Dele(CommonMethod);
            Assert.IsTrue(TakeDelegateAsFieldInClass_Seq(c3), "Delegate marshaled as field in class with Sequential.");

            Console.WriteLine("\n\nScenario 4: Delegate marshaled as field in pre-marshalled class with Sequential.");
            Class2_FuncPtrAsField3_Seq c4 = new Class2_FuncPtrAsField3_Seq();
            c4.verification = true;
            c4.dele = new Dele(CommonMethod);
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<Class2_FuncPtrAsField3_Seq>());
            Marshal.StructureToPtr(c4, ptr, false);
            Assert.IsTrue(TakeDelegateAsFieldInPreMarshalledClass_Seq(ptr), "Delegate marshaled as field in pre-marshalled class with Sequential.");
            Marshal.FreeHGlobal(ptr);
            GC.KeepAlive(c4);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("\n\nScenario 5 : Delegate marshaled as field in class with Explicit.");
                Class2_FuncPtrAsField4_Exp c5 = new Class2_FuncPtrAsField4_Exp();
                c5.verification = true;
                c5.dele = new Dele(CommonMethod);
                Assert.IsTrue(TakeDelegateAsFieldInClass_Exp(c5), "Delegate marshaled as field in class with Explicit.");
            }
            return 100;
        } catch (Exception e){
            Console.WriteLine($"Test Failure: {e}"); 
            return 101; 
        }
    }
}
