// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using TestLibrary;

class AsFunctionPtrTest
{
    [DllImport("PInvoke_Delegate_AsField")]
    extern static int CommonMethod();

    [DllImport("PInvoke_Delegate_AsField")]
    extern static bool TakeDelegateAsFieldInStruct_Seq(Struct1_FuncPtrAsField1_Seq s);

    [DllImport("PInvoke_Delegate_AsField")]
    extern static bool TakeDelegateAsFieldInStruct_Exp(Struct1_FuncPtrAsField2_Exp s);

    [DllImport("PInvoke_Delegate_AsField")]
    extern static bool TakeDelegateAsFieldInClass_Seq(Class1_FuncPtrAsField3_Seq s);

    [DllImport("PInvoke_Delegate_AsField")]
    extern static bool TakeDelegateAsFieldInClass_Exp(Class1_FuncPtrAsField4_Exp s);

    static int Main()
    {
        try{
            Console.WriteLine("Scenario 1 : Delegate marshaled as field in struct with Sequential.");
            Struct1_FuncPtrAsField1_Seq s1 = new Struct1_FuncPtrAsField1_Seq();
            s1.verification = true;
            s1.dele = new Dele(CommonMethod);
            Assert.IsTrue(TakeDelegateAsFieldInStruct_Seq(s1), "Delegate marshaled as field in struct with Sequential.");
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) // We don't support marshalling explicit structs by-val on the System V x64 ABI
            {
                Console.WriteLine("\n\nScenario 2 : Delegate marshaled as field in struct with Explicit.");
                Struct1_FuncPtrAsField2_Exp s2 = new Struct1_FuncPtrAsField2_Exp();
                s2.verification = true;
                s2.dele = new Dele(CommonMethod);
                Assert.IsTrue(TakeDelegateAsFieldInStruct_Exp(s2), "Delegate marshaled as field in struct with Explicit.");
            }

            Console.WriteLine("\n\nScenario 3 : Delegate marshaled as field in class with Sequential.");
            Class1_FuncPtrAsField3_Seq c3 = new Class1_FuncPtrAsField3_Seq();
            c3.verification = true;
            c3.dele = new Dele(CommonMethod);
            Assert.IsTrue(TakeDelegateAsFieldInClass_Seq(c3), "Delegate marshaled as field in class with Sequential.");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("\n\nScenario 4 : Delegate marshaled as field in class with Explicit.");
                Class1_FuncPtrAsField4_Exp c4 = new Class1_FuncPtrAsField4_Exp();
                c4.verification = true;
                c4.dele = new Dele(CommonMethod);
                Assert.IsTrue(TakeDelegateAsFieldInClass_Exp(c4), "Delegate marshaled as field in class with Explicit.");
            }
            return 100;
        } catch (Exception e){
            Console.WriteLine($"Test Failure: {e}"); 
            return 101; 
        }
    }
}
