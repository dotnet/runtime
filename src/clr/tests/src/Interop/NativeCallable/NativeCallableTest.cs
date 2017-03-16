// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading;

using Console = Internal.Console;

public class Program
{
    public static class NativeMethods
    {
        [DllImport("NativeCallableDll")]
        public static extern int CallManagedAdd(IntPtr callbackProc, int n);
    }

    private delegate int IntNativeMethodInvoker();    
    private delegate void NativeMethodInvoker();

    public static int Main()
    {
        int ret;
        //NegativeTest_NonBlittable();
        ret = TestNativeCallableValid();
        if (ret != 100)
            return ret;
        //NegativeTest_ViaDelegate();
        //NegativeTest_ViaLdftn();
        return 100;
    }

    public static int TestNativeCallableValid()
    {
        /*
           void TestNativeCallable()
           {
                   .locals init ([0] native int ptr)
                   IL_0000:  nop  
                   IL_0002:  ldftn      int32 CallbackMethod(int32)

                   IL_0012:  stloc.0
                   IL_0013:  ldloc.0
                   IL_0014:  ldc.i4     100
                   IL_0019:  call       bool NativeMethods::CallNativeAdd(native int, int)
                   IL_001e:  pop
                   IL_001f:  ret
             }
           */
        DynamicMethod testNativeCallable = new DynamicMethod("TestNativeCallable", typeof(int), null, typeof(Program).Module);
        ILGenerator il = testNativeCallable.GetILGenerator();
        il.DeclareLocal(typeof(IntPtr));
        il.Emit(OpCodes.Nop);

        // Get native function pointer of the callback
        il.Emit(OpCodes.Ldftn, typeof(Program).GetMethod("ManagedAddCallback"));
        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Ldloc_0);

        // return 111+100
        il.Emit(OpCodes.Ldc_I4, 111);
        il.Emit(OpCodes.Call, typeof(NativeMethods).GetMethod("CallManagedAdd"));
        il.Emit(OpCodes.Ret);
        IntNativeMethodInvoker testNativeMethod = (IntNativeMethodInvoker)testNativeCallable.CreateDelegate(typeof(IntNativeMethodInvoker));
        if (testNativeMethod() != 211)
            return 0;
        return 100;
    }

    public static void NegativeTest_ViaDelegate()
    {
        // Try invoking method directly 
        try
        {
            Func<int, int> invoker = ManagedAddCallback;
            invoker(0);
        }
        catch (Exception)
        {

        }
    }

    public static void NegativeTest_NonBlittable()
    {
        // Try invoking method directly 
        try
        {
            Func<bool, int> invoker = CallbackMethodNonBlitabble;
            invoker(true);
        }
        catch (Exception)
        {
            Console.WriteLine(":bla");
        }
    }


    public static void NegativeTest_ViaLdftn()
    {
        /*
           .locals init (native int V_0)
           IL_0000:  nop
           IL_0001:  ldftn      void ConsoleApplication1.Program::callback(int32)
           IL_0007:  stloc.0
           IL_0008:  ldc.i4.s   12
           IL_000a:  ldloc.0
           IL_000b:  calli      void(int32)
           IL_0010:  nop
           IL_0011:  ret
       */
        DynamicMethod testNativeCallable = new DynamicMethod("TestNativeCallableLdftn", null, null, typeof(Program).Module);
        ILGenerator il = testNativeCallable.GetILGenerator();
        il.DeclareLocal(typeof(IntPtr));
        il.Emit(OpCodes.Nop);
        il.Emit(OpCodes.Ldftn, typeof(Program).GetMethod("LdftnCallback"));
        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Ldc_I4,12);
        il.Emit(OpCodes.Ldloc_0);

        SignatureHelper sig =  SignatureHelper.GetMethodSigHelper(typeof(Program).Module, null, new Type[] { typeof(int) });
        sig.AddArgument(typeof(int));

        // il.EmitCalli is not available  and the below is not correct
        il.Emit(OpCodes.Calli,sig);
        il.Emit(OpCodes.Nop);
        il.Emit(OpCodes.Ret);

        NativeMethodInvoker testNativeMethod = (NativeMethodInvoker)testNativeCallable.CreateDelegate(typeof(NativeMethodInvoker));
        testNativeMethod();

    }

    #region callbacks
    [NativeCallable]
    public static void LdftnCallback(int val)
    {
    }

    [NativeCallable]
    public static int ManagedAddCallback(int n)
    {
        return n + 100;
    }

    [NativeCallable]
    public static int CallbackMethodGeneric<T>(IntPtr hWnd, IntPtr lParam)
    {
        return 1;
    }

    [NativeCallable]
    public static int CallbackMethodNonBlitabble(bool x1)
    {
        return 1;
    }
    #endregion //callbacks

}
