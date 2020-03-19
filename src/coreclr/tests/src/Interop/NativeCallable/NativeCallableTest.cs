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

public class Program
{
    public static class NativeCallableDll
    {
        [DllImport(nameof(NativeCallableDll))]
        public static extern int CallManagedProc(IntPtr callbackProc, int n);

        [DllImport(nameof(NativeCallableDll))]
        public static extern int CallManagedProcOnNewThread(IntPtr callbackProc, int n);

        [DllImport(nameof(NativeCallableDll))]
        // Returns -1 if exception was throw and caught.
        public static extern int CallManagedProcCatchException(IntPtr callbackProc, int n);
    }

    private delegate int IntNativeMethodInvoker();
    private delegate void NativeMethodInvoker();

    public static int Main(string[] args)
    {
        try
        {
            TestNativeCallableValid();
            TestNativeCallableValid_OnNewNativeThread();
            TestNativeCallableValid_PrepareMethod();
            NegativeTest_NonStaticMethod();
            NegativeTest_ViaDelegate();
            NegativeTest_NonBlittable();
            NegativeTest_NonInstantiatedGenericArguments();
            NegativeTest_InstantiatedGenericArguments();
            NegativeTest_FromInstantiatedGenericClass();
            TestNativeCallableViaUnmanagedCalli();

            // Exception handling is only supported on Windows.
            if (TestLibrary.Utilities.IsWindows)
            {
                TestNativeCallableValid_ThrowException();
                TestNativeCallableViaUnmanagedCalli_ThrowException();
            }

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
        Console.WriteLine($"Running {nameof(TestNativeCallableValid)}...");

        /*
           void NativeCallable()
           {
                .locals init ([0] native int ptr)
                IL_0000:  nop
                IL_0001:  ldftn      int32 ManagedDoubleCallback(int32)
                IL_0007:  stloc.0

                IL_0008:  ldloc.0
                IL_0009:  ldc.i4     <n> local
                IL_000e:  call       bool NativeCallableDll::CallManagedProc(native int, int)

                IL_0013:  ret
             }
        */
        DynamicMethod testNativeCallable = new DynamicMethod("NativeCallable", typeof(int), null, typeof(Program).Module);
        ILGenerator il = testNativeCallable.GetILGenerator();
        il.DeclareLocal(typeof(IntPtr));
        il.Emit(OpCodes.Nop);

        // Get native function pointer of the callback
        il.Emit(OpCodes.Ldftn, typeof(Program).GetMethod(nameof(ManagedDoubleCallback)));
        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Ldloc_0);

        int n = 12345;
        il.Emit(OpCodes.Ldc_I4, n);
        il.Emit(OpCodes.Call, typeof(NativeCallableDll).GetMethod("CallManagedProc"));
        il.Emit(OpCodes.Ret);
        var testNativeMethod = (IntNativeMethodInvoker)testNativeCallable.CreateDelegate(typeof(IntNativeMethodInvoker));

        int expected = DoubleImpl(n);
        Assert.AreEqual(expected, testNativeMethod());
    }

    public static void TestNativeCallableValid_OnNewNativeThread()
    {
        Console.WriteLine($"Running {nameof(TestNativeCallableValid_OnNewNativeThread)}...");

        /*
           void NativeCallableOnNewNativeThread()
           {
                .locals init ([0] native int ptr)
                IL_0000:  nop
                IL_0001:  ldftn      int32 ManagedDoubleCallback(int32)
                IL_0007:  stloc.0

                IL_0008:  ldloc.0
                IL_0009:  ldc.i4     <n> local
                IL_000e:  call       bool NativeCallableDll::CallManagedProcOnNewThread(native int, int)

                IL_0013:  ret
             }
        */
        DynamicMethod testNativeCallable = new DynamicMethod("NativeCallableOnNewNativeThread", typeof(int), null, typeof(Program).Module);
        ILGenerator il = testNativeCallable.GetILGenerator();
        il.DeclareLocal(typeof(IntPtr));
        il.Emit(OpCodes.Nop);

        // Get native function pointer of the callback
        il.Emit(OpCodes.Ldftn, typeof(Program).GetMethod(nameof(ManagedDoubleCallback)));
        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Ldloc_0);

        int n = 12345;
        il.Emit(OpCodes.Ldc_I4, n);
        il.Emit(OpCodes.Call, typeof(NativeCallableDll).GetMethod("CallManagedProcOnNewThread"));
        il.Emit(OpCodes.Ret);
        var testNativeMethod = (IntNativeMethodInvoker)testNativeCallable.CreateDelegate(typeof(IntNativeMethodInvoker));

        int expected = DoubleImpl(n);
        Assert.AreEqual(expected, testNativeMethod());
    }

    [NativeCallable]
    public static int ManagedCallback_Prepared(int n)
    {
        return DoubleImpl(n);
    }

    // This test is about the interaction between Tiered Compilation and the NativeCallableAttribute.
    public static void TestNativeCallableValid_PrepareMethod()
    {
        Console.WriteLine($"Running {nameof(TestNativeCallableValid_PrepareMethod)}...");

        /*
           void NativeCallableOnNewNativeThread()
           {
                .locals init ([0] native int ptr)
                IL_0000:  nop
                IL_0001:  ldftn      int32 ManagedCallback_Prepared(int32)
                IL_0007:  stloc.0

                IL_0008:  ldloc.0
                IL_0009:  ldc.i4     <n> local
                IL_000e:  call       bool NativeCallableDll::CallManagedProcOnNewThread(native int, int)

                IL_0013:  ret
             }
        */
        DynamicMethod testNativeCallable = new DynamicMethod("NativeCallableValid_PrepareMethod", typeof(int), null, typeof(Program).Module);
        ILGenerator il = testNativeCallable.GetILGenerator();
        il.DeclareLocal(typeof(IntPtr));
        il.Emit(OpCodes.Nop);

        // Prepare the managed callback.
        var preparedCallback = typeof(Program).GetMethod(nameof(ManagedCallback_Prepared));
        RuntimeHelpers.PrepareMethod(preparedCallback.MethodHandle);

        // Get native function pointer of the callback
        il.Emit(OpCodes.Ldftn, preparedCallback);
        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Ldloc_0);

        int n = 12345;
        il.Emit(OpCodes.Ldc_I4, n);
        il.Emit(OpCodes.Call, typeof(NativeCallableDll).GetMethod("CallManagedProcOnNewThread"));
        il.Emit(OpCodes.Ret);
        var testNativeMethod = (IntNativeMethodInvoker)testNativeCallable.CreateDelegate(typeof(IntNativeMethodInvoker));

        // Call enough to attempt to trigger Tiered Compilation from a new thread.
        for (int i = 0; i < 100; ++i)
        {
            testNativeMethod();
        }
    }

    private const int CallbackThrowsErrorCode = 27;

    [NativeCallable]
    public static int CallbackThrows(int val)
    {
        throw new Exception() { HResult = CallbackThrowsErrorCode };
    }

    public static void TestNativeCallableValid_ThrowException()
    {
        Console.WriteLine($"Running {nameof(TestNativeCallableValid_ThrowException)}...");

        /*
           void NativeCallableValid_ThrowException()
           {
                .locals init ([0] native int ptr)
                IL_0000:  nop
                IL_0001:  ldftn      int32 CallbackThrows(int32)
                IL_0007:  stloc.0

                IL_0008:  ldloc.0
                IL_0009:  ldc.i4     <n> local
                IL_000e:  call       bool NativeCallableDll::CallManagedProcCatchException(native int, int)

                IL_0013:  ret
             }
        */
        DynamicMethod testNativeCallable = new DynamicMethod("NativeCallableValid_ThrowException", typeof(int), null, typeof(Program).Module);
        ILGenerator il = testNativeCallable.GetILGenerator();
        il.DeclareLocal(typeof(IntPtr));
        il.Emit(OpCodes.Nop);

        // Get native function pointer of the callback
        il.Emit(OpCodes.Ldftn, typeof(Program).GetMethod(nameof(CallbackThrows)));
        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Ldloc_0);

        int n = 12345;
        il.Emit(OpCodes.Ldc_I4, n);
        il.Emit(OpCodes.Call, typeof(NativeCallableDll).GetMethod("CallManagedProcCatchException"));
        il.Emit(OpCodes.Ret);
        var testNativeMethod = (IntNativeMethodInvoker)testNativeCallable.CreateDelegate(typeof(IntNativeMethodInvoker));

        // Method should have thrown and caught an exception.
        Assert.AreEqual(-1, testNativeMethod());
    }

    public static void NegativeTest_ViaDelegate()
    {
        Console.WriteLine($"Running {nameof(NegativeTest_ViaDelegate)}...");

        // Try invoking method directly
        Assert.Throws<NotSupportedException>(() => { CallAsDelegate(); });

        // Local function to delay exception thrown during JIT
        void CallAsDelegate()
        {
            Func<int, int> invoker = ManagedDoubleCallback;
            invoker(0);
        }
    }

    [NativeCallable]
    public void CallbackNonStatic()
    {
        Assert.Fail($"Instance functions with attribute {nameof(NativeCallableAttribute)} are invalid");
    }

    public static void NegativeTest_NonStaticMethod()
    {
        Console.WriteLine($"Running {nameof(NegativeTest_NonStaticMethod)}...");

        /*
           void TestNativeCallableNonStatic()
           {
                .locals init ([0] native int ptr)
                IL_0000:  nop
                IL_0001:  ldftn      void CallbackNonStatic()
                IL_0007:  stloc.0
                IL_0008:  ret
             }
        */
        DynamicMethod testNativeCallable = new DynamicMethod("TestNativeCallableNonStatic", null, null, typeof(Program).Module);
        ILGenerator il = testNativeCallable.GetILGenerator();
        il.DeclareLocal(typeof(IntPtr));
        il.Emit(OpCodes.Nop);

        // Get native function pointer of the callback
        il.Emit(OpCodes.Ldftn, typeof(Program).GetMethod(nameof(CallbackNonStatic)));
        il.Emit(OpCodes.Stloc_0);

        il.Emit(OpCodes.Ret);
        var testNativeMethod = (NativeMethodInvoker)testNativeCallable.CreateDelegate(typeof(NativeMethodInvoker));

        // Try invoking method
        Assert.Throws<InvalidProgramException>(() => { testNativeMethod(); });
    }

    [NativeCallable]
    public static void CallbackMethodNonBlittable(bool x1)
    {
        Assert.Fail($"Functions with attribute {nameof(NativeCallableAttribute)} cannot have non-blittable arguments");
    }

    public static void NegativeTest_NonBlittable()
    {
        Console.WriteLine($"Running {nameof(NegativeTest_NonBlittable)}...");

        /*
           void TestNativeCallableNonBlittable()
           {
                .locals init ([0] native int ptr)
                IL_0000:  nop
                IL_0001:  ldftn      void CallbackMethodNonBlittable(bool)
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
        Assert.Throws<InvalidProgramException>(() => { testNativeMethod(); });
    }

    [NativeCallable]
    public static void CallbackMethodGeneric<T>(T arg)
    {
        Assert.Fail($"Functions with attribute {nameof(NativeCallableAttribute)} cannot have generic arguments");
    }

    public static void NegativeTest_NonInstantiatedGenericArguments()
    {
        Console.WriteLine($"Running {nameof(NegativeTest_NonInstantiatedGenericArguments)}...");

        /*
           void TestNativeCallableNonInstGenericArguments()
           {
                .locals init ([0] native int ptr)
                IL_0000:  nop
                IL_0001:  ldftn      void CallbackMethodGeneric(T)
                IL_0007:  stloc.0
                IL_0008:  ret
             }
        */
        DynamicMethod testNativeCallable = new DynamicMethod("TestNativeCallableNonInstGenericArguments", null, null, typeof(Program).Module);
        ILGenerator il = testNativeCallable.GetILGenerator();
        il.DeclareLocal(typeof(IntPtr));
        il.Emit(OpCodes.Nop);

        // Get native function pointer of the callback
        il.Emit(OpCodes.Ldftn, typeof(Program).GetMethod(nameof(CallbackMethodGeneric)));
        il.Emit(OpCodes.Stloc_0);

        il.Emit(OpCodes.Ret);
        var testNativeMethod = (NativeMethodInvoker)testNativeCallable.CreateDelegate(typeof(NativeMethodInvoker));

        // Try invoking method
        Assert.Throws<InvalidProgramException>(() => { testNativeMethod(); });
    }

    public static void NegativeTest_InstantiatedGenericArguments()
    {
        Console.WriteLine($"Running {nameof(NegativeTest_InstantiatedGenericArguments)}...");

        /*
           void TestNativeCallableInstGenericArguments()
           {
                .locals init ([0] native int ptr)
                IL_0000:  nop
                IL_0001:  ldftn      void CallbackMethodGeneric(int)
                IL_0007:  stloc.0
                IL_0008:  ret
             }
        */
        DynamicMethod testNativeCallable = new DynamicMethod("TestNativeCallableInstGenericArguments", null, null, typeof(Program).Module);
        ILGenerator il = testNativeCallable.GetILGenerator();
        il.DeclareLocal(typeof(IntPtr));
        il.Emit(OpCodes.Nop);

        // Get native function pointer of the instantiated generic callback
        il.Emit(OpCodes.Ldftn, typeof(Program).GetMethod(nameof(CallbackMethodGeneric)).MakeGenericMethod(new [] { typeof(int) }));
        il.Emit(OpCodes.Stloc_0);

        il.Emit(OpCodes.Ret);
        var testNativeMethod = (NativeMethodInvoker)testNativeCallable.CreateDelegate(typeof(NativeMethodInvoker));

        // Try invoking method
        Assert.Throws<InvalidProgramException>(() => { testNativeMethod(); });
    }

    public class GenericClass<T>
    {
        [NativeCallable]
        public static void CallbackMethod(int n)
        {
            Assert.Fail($"Functions with attribute {nameof(NativeCallableAttribute)} within a generic type are invalid");
        }
    }

    public static void NegativeTest_FromInstantiatedGenericClass()
    {
        Console.WriteLine($"Running {nameof(NegativeTest_FromInstantiatedGenericClass)}...");

        /*
           void TestNativeCallableInstGenericType()
           {
                .locals init ([0] native int ptr)
                IL_0000:  nop
                IL_0001:  ldftn      void GenericClass<int>::CallbackMethod(int)
                IL_0007:  stloc.0
                IL_0008:  ret
             }
        */
        DynamicMethod testNativeCallable = new DynamicMethod("TestNativeCallableInstGenericClass", null, null, typeof(Program).Module);
        ILGenerator il = testNativeCallable.GetILGenerator();
        il.DeclareLocal(typeof(IntPtr));
        il.Emit(OpCodes.Nop);

        // Get native function pointer of the callback from the instantiated generic class.
        il.Emit(OpCodes.Ldftn, typeof(GenericClass<int>).GetMethod(nameof(GenericClass<int>.CallbackMethod)));
        il.Emit(OpCodes.Stloc_0);

        il.Emit(OpCodes.Ret);
        var testNativeMethod = (NativeMethodInvoker)testNativeCallable.CreateDelegate(typeof(NativeMethodInvoker));

        // Try invoking method
        Assert.Throws<InvalidProgramException>(() => { testNativeMethod(); });
    }

    [NativeCallable]
    public static void CallbackViaCalli(int val)
    {
        Assert.Fail($"Functions with attribute {nameof(NativeCallableAttribute)} cannot be called via calli");
    }

    public static void NegativeTest_ViaCalli()
    {
        Console.WriteLine($"{nameof(NegativeTest_ViaCalli)} function via calli instruction. The CLR _will_ crash.");

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

    public static void TestNativeCallableViaUnmanagedCalli()
    {
        Console.WriteLine($"Running {nameof(TestNativeCallableViaUnmanagedCalli)}...");

        /*
           void NativeCallableViaCalli()
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
        DynamicMethod testNativeCallable = new DynamicMethod("NativeCallableViaUnmanagedCalli", typeof(int), null, typeof(Program).Module);
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

    [NativeCallable(CallingConvention = CallingConvention.StdCall)]
    public static int CallbackViaUnmanagedCalliThrows(int val)
    {
        throw new Exception() { HResult = CallbackThrowsErrorCode };
    }

    public static void TestNativeCallableViaUnmanagedCalli_ThrowException()
    {
        Console.WriteLine($"Running {nameof(TestNativeCallableViaUnmanagedCalli_ThrowException)}...");

        /*
           void NativeCallableViaUnmanagedCalli_ThrowException()
           {
                .locals init (native int V_0)
                IL_0000:  nop
                IL_0001:  ldftn      int CallbackViaUnmanagedCalliThrows(int32)
                IL_0007:  stloc.0

                IL_0008:  ldc.i4     1234
                IL_000d:  ldloc.0
                IL_000e:  calli      int32 stdcall(int32)

                IL_0014:  ret
           }
        */
        DynamicMethod testNativeCallable = new DynamicMethod("NativeCallableViaUnmanagedCalli_ThrowException", typeof(int), null, typeof(Program).Module);
        ILGenerator il = testNativeCallable.GetILGenerator();
        il.DeclareLocal(typeof(IntPtr));
        il.Emit(OpCodes.Nop);

        // Get native function pointer of the callback
        il.Emit(OpCodes.Ldftn, typeof(Program).GetMethod(nameof(CallbackViaUnmanagedCalliThrows)));
        il.Emit(OpCodes.Stloc_0);

        int n = 1234;

        il.Emit(OpCodes.Ldc_I4, n);
        il.Emit(OpCodes.Ldloc_0);
        il.EmitCalli(OpCodes.Calli, CallingConvention.StdCall, typeof(int), new Type[] { typeof(int) });

        il.Emit(OpCodes.Ret);

        IntNativeMethodInvoker testNativeMethod = (IntNativeMethodInvoker)testNativeCallable.CreateDelegate(typeof(IntNativeMethodInvoker));

        try
        {
            testNativeMethod();
            Assert.Fail($"Function {nameof(CallbackViaUnmanagedCalliThrows)} should throw");
        }
        catch (Exception e)
        {
            Assert.AreEqual(CallbackThrowsErrorCode, e.HResult);
        }
    }
}
