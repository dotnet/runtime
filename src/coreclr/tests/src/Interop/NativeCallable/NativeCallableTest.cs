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
using TestLibrary;

using Console = Internal.Console;

public class Program
{
    public static class NativeMethods
    {
        [DllImport("NativeCallableDll")]
        public static extern int CallManagedProc(IntPtr callbackProc, int n);
    }

    private delegate int IntNativeMethodInvoker();
    private delegate void NativeMethodInvoker();

    public static int Main(string[] args)
    {
        try
        {
            TestNativeCallableValid();
            NegativeTest_ViaDelegate();
            NegativeTest_NonBlittable();
            NegativeTest_GenericArguments();
            NativeCallableViaUnmanagedCalli();

            if (args.Length != 0 && args[0].Equals("calli"))
            {
                NegativeTest_ViaCalli();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }

        return 100;
    }

    [NativeCallable]
    public static int ManagedDoubleCallback(int n)
    {
        return DoubleImpl(n);
    }

    private static int DoubleImpl(int n)
    {
        return 2 * n;
    }

    public static void TestNativeCallableValid()
    {
        Console.WriteLine($"{nameof(NativeCallableAttribute)} function");

        /*
           void TestNativeCallable()
           {
                .locals init ([0] native int ptr)
                IL_0000:  nop
                IL_0001:  ldftn      int32 ManagedDoubleCallback(int32)
                IL_0007:  stloc.0

                IL_0008:  ldloc.0
                IL_0009:  ldc.i4     <n> local
                IL_000e:  call       bool NativeMethods::CallManagedProc(native int, int)

                IL_0013:  ret
             }
        */
        DynamicMethod testNativeCallable = new DynamicMethod("TestNativeCallable", typeof(int), null, typeof(Program).Module);
        ILGenerator il = testNativeCallable.GetILGenerator();
        il.DeclareLocal(typeof(IntPtr));
        il.Emit(OpCodes.Nop);

        // Get native function pointer of the callback
        il.Emit(OpCodes.Ldftn, typeof(Program).GetMethod(nameof(ManagedDoubleCallback)));
        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Ldloc_0);

        int n = 12345;
        il.Emit(OpCodes.Ldc_I4, n);
        il.Emit(OpCodes.Call, typeof(NativeMethods).GetMethod("CallManagedProc"));
        il.Emit(OpCodes.Ret);
        var testNativeMethod = (IntNativeMethodInvoker)testNativeCallable.CreateDelegate(typeof(IntNativeMethodInvoker));

        int expected = DoubleImpl(n);
        Assert.AreEqual(expected, testNativeMethod());
    }

    public static void NegativeTest_ViaDelegate()
    {
        Console.WriteLine($"{nameof(NativeCallableAttribute)} function as delegate");

        // Try invoking method directly
        try
        {
            CallAsDelegate();
            Assert.Fail($"Invalid to call {nameof(ManagedDoubleCallback)} as delegate");
        }
        catch (NotSupportedException)
        {
        }

        // Local function to delay exception thrown during JIT
        void CallAsDelegate()
        {
            Func<int, int> invoker = ManagedDoubleCallback;
            invoker(0);
        }
    }

    [NativeCallable]
    public static int CallbackMethodNonBlittable(bool x1)
    {
        Assert.Fail($"Functions with attribute {nameof(NativeCallableAttribute)} cannot have non-blittable arguments");
        return -1;
    }

    public static void NegativeTest_NonBlittable()
    {
        Console.WriteLine($"{nameof(NativeCallableAttribute)} function with non-blittable arguments");

        /*
           void TestNativeCallableNonBlittable()
           {
                .locals init ([0] native int ptr)
                IL_0000:  nop
                IL_0001:  ldftn      int32 CallbackMethodNonBlittable(bool)
                IL_0007:  stloc.0
                IL_0008:  ret
             }
        */
        DynamicMethod testNativeCallable = new DynamicMethod("TestNativeCallableNonBlittable", null, null, typeof(Program).Module);
        ILGenerator il = testNativeCallable.GetILGenerator();
        il.DeclareLocal(typeof(IntPtr));
        il.Emit(OpCodes.Nop);

        // Get native function pointer of the callback
        il.Emit(OpCodes.Ldftn, typeof(Program).GetMethod(nameof(CallbackMethodNonBlittable)));
        il.Emit(OpCodes.Stloc_0);

        il.Emit(OpCodes.Ret);
        var testNativeMethod = (NativeMethodInvoker)testNativeCallable.CreateDelegate(typeof(NativeMethodInvoker));

        // Try invoking method
        try
        {
            testNativeMethod();
            Assert.Fail($"Function {nameof(CallbackMethodNonBlittable)} has non-blittable types");
        }
        catch (NotSupportedException)
        {
        }
    }

    [NativeCallable]
    public static int CallbackMethodGeneric<T>(T arg)
    {
        Assert.Fail($"Functions with attribute {nameof(NativeCallableAttribute)} cannot have generic arguments");
        return -1;
    }

    public static void NegativeTest_GenericArguments()
    {
        /*
           void TestNativeCallableGenericArguments()
           {
                .locals init ([0] native int ptr)
                IL_0000:  nop
                IL_0001:  ldftn      int32 CallbackMethodGeneric(T)
                IL_0007:  stloc.0
                IL_0008:  ret
             }
        */
        DynamicMethod testNativeCallable = new DynamicMethod("TestNativeCallableGenericArguments", null, null, typeof(Program).Module);
        ILGenerator il = testNativeCallable.GetILGenerator();
        il.DeclareLocal(typeof(IntPtr));
        il.Emit(OpCodes.Nop);

        // Get native function pointer of the callback
        il.Emit(OpCodes.Ldftn, typeof(Program).GetMethod(nameof(CallbackMethodGeneric)));
        il.Emit(OpCodes.Stloc_0);

        il.Emit(OpCodes.Ret);
        var testNativeMethod = (NativeMethodInvoker)testNativeCallable.CreateDelegate(typeof(NativeMethodInvoker));

        // Try invoking method
        try
        {
            testNativeMethod();
            Assert.Fail($"Function {nameof(CallbackMethodGeneric)} has generic types");
        }
        catch (InvalidProgramException)
        {
        }
    }

    [NativeCallable]
    public static void CallbackViaCalli(int val)
    {
        Assert.Fail($"Functions with attribute {nameof(NativeCallableAttribute)} cannot be called via calli");
    }

    public static void NegativeTest_ViaCalli()
    {
        Console.WriteLine($"{nameof(NativeCallableAttribute)} function via calli instruction. The CLR _will_ crash.");

        /*
           void TestNativeCallableViaCalli()
           {
                .locals init (native int V_0)
                IL_0000:  nop
                IL_0001:  ldftn      void CallbackViaCalli(int32)
                IL_0007:  stloc.0

                IL_0008:  ldc.i4     1234
                IL_000d:  ldloc.0
                IL_000e:  calli      void(int32)

                IL_0013:  nop
                IL_0014:  ret
           }
        */
        DynamicMethod testNativeCallable = new DynamicMethod("TestNativeCallableViaCalli", null, null, typeof(Program).Module);
        ILGenerator il = testNativeCallable.GetILGenerator();
        il.DeclareLocal(typeof(IntPtr));
        il.Emit(OpCodes.Nop);

        // Get native function pointer of the callback
        il.Emit(OpCodes.Ldftn, typeof(Program).GetMethod(nameof(CallbackViaCalli)));
        il.Emit(OpCodes.Stloc_0);

        il.Emit(OpCodes.Ldc_I4, 1234);
        il.Emit(OpCodes.Ldloc_0);
        il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, null, new Type[] { typeof(int) }, null);

        il.Emit(OpCodes.Nop);
        il.Emit(OpCodes.Ret);

        NativeMethodInvoker testNativeMethod = (NativeMethodInvoker)testNativeCallable.CreateDelegate(typeof(NativeMethodInvoker));

        // It is not possible to catch the resulting ExecutionEngineException exception.
        // To observe the crashing behavior set a breakpoint in the ReversePInvokeBadTransition() function
        // located in src/vm/dllimportcallback.cpp.
        testNativeMethod();
    }

    [NativeCallable(CallingConvention = CallingConvention.StdCall)]
    public static int CallbackViaUnmanagedCalli(int val)
    {
        return DoubleImpl(val);
    }

    public static void NativeCallableViaUnmanagedCalli()
    {
        Console.WriteLine($"{nameof(NativeCallableAttribute)} function via calli instruction with unmanaged calling convention.");

        /*
           void TestNativeCallableViaCalli()
           {
                .locals init (native int V_0)
                IL_0000:  nop
                IL_0001:  ldftn      int CallbackViaUnmanagedCalli(int32)
                IL_0007:  stloc.0

                IL_0008:  ldc.i4     1234
                IL_000d:  ldloc.0
                IL_000e:  calli      int32 stdcall(int32)

                IL_0014:  ret
           }
        */
        DynamicMethod testNativeCallable = new DynamicMethod("TestNativeCallableViaUnmanagedCalli", typeof(int), null, typeof(Program).Module);
        ILGenerator il = testNativeCallable.GetILGenerator();
        il.DeclareLocal(typeof(IntPtr));
        il.Emit(OpCodes.Nop);

        // Get native function pointer of the callback
        il.Emit(OpCodes.Ldftn, typeof(Program).GetMethod(nameof(CallbackViaUnmanagedCalli)));
        il.Emit(OpCodes.Stloc_0);

        int n = 1234;

        il.Emit(OpCodes.Ldc_I4, n);
        il.Emit(OpCodes.Ldloc_0);
        il.EmitCalli(OpCodes.Calli, CallingConvention.StdCall, typeof(int), new Type[] { typeof(int) });

        il.Emit(OpCodes.Ret);

        IntNativeMethodInvoker testNativeMethod = (IntNativeMethodInvoker)testNativeCallable.CreateDelegate(typeof(IntNativeMethodInvoker));

        int expected = DoubleImpl(n);
        Assert.AreEqual(expected, testNativeMethod());
    }
}
