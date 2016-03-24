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

public class Program
{
    public static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern int EnumWindows(IntPtr enumProc, IntPtr lParam);
    }

    private delegate void NativeMethodInvoker();
    static EventWaitHandle waitHandle = new AutoResetEvent(false);

    public static int Main()
    {
        //NegativeTest_NonBlittable();
        TestNativeCallableValid();
        //NegativeTest_ViaDelegate();
        //NegativeTest_ViaLdftn();
        return 100;
    }

    public static void TestNativeCallableValid()
    {
        /*
           void TestNativeCallable()
           {
                   .locals init ([0] native int ptr)
                   IL_0000:  nop  
                   IL_0002:  ldftn      int32 CallbackMethod(native int,native int)

                   IL_0012:  stloc.0
                   IL_0013:  ldloc.0
                   IL_0014:  ldsfld     native int [mscorlib]System.IntPtr::Zero
                   IL_0019:  call       bool NativeMethods::EnumWindows(native int,
                                                                                      native int)
                   IL_001e:  pop
                   IL_001f:  ret
             }
           */
        DynamicMethod testNativeCallable = new DynamicMethod("TestNativeCallable", null, null, typeof(Program).Module);
        ILGenerator il = testNativeCallable.GetILGenerator();
        il.DeclareLocal(typeof(IntPtr));
        il.Emit(OpCodes.Nop);
        il.Emit(OpCodes.Ldftn, typeof(Program).GetMethod("CallbackMethod"));
        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Ldloc_0);
        il.Emit(OpCodes.Ldsfld, typeof(IntPtr).GetField("Zero"));
        il.Emit(OpCodes.Call, typeof(NativeMethods).GetMethod("EnumWindows"));
        il.Emit(OpCodes.Pop);
        il.Emit(OpCodes.Ret);
        NativeMethodInvoker testNativeMethod = (NativeMethodInvoker)testNativeCallable.CreateDelegate(typeof(NativeMethodInvoker));
        testNativeMethod();
    }

    public static void NegativeTest_ViaDelegate()
    {
        // Try invoking method directly 
        try
        {
            Func<IntPtr, IntPtr, int> invoker = CallbackMethod;
            invoker(IntPtr.Zero, IntPtr.Zero);
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
    public static int CallbackMethod(IntPtr hWnd, IntPtr lParam)
    {
        waitHandle.Set();
        return 1;
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