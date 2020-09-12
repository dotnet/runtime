// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using TestLibrary;

public class Program
{
    public static class UnmanagedCallersOnlyDll
    {
        [DllImport(nameof(UnmanagedCallersOnlyDll))]
        public static extern int DoubleImplNative(int n);

        [DllImport(nameof(UnmanagedCallersOnlyDll))]
        public static extern int CallManagedProc(IntPtr callbackProc, int n);

        [DllImport(nameof(UnmanagedCallersOnlyDll))]
        public static extern int CallManagedProcMultipleTimes(int m, IntPtr callbackProc, int n);

        [DllImport(nameof(UnmanagedCallersOnlyDll))]
        public static extern int CallManagedProcOnNewThread(IntPtr callbackProc, int n);

        [DllImport(nameof(UnmanagedCallersOnlyDll))]
        // Returns -1 if exception was throw and caught.
        public static extern int CallManagedProcCatchException(IntPtr callbackProc, int n);

        [UnmanagedCallersOnly]
        [DllImport(nameof(UnmanagedCallersOnlyDll), EntryPoint = "DoesntExist")]
        public static extern int PInvokeMarkedWithUnmanagedCallersOnly(int n);
    }

    private const string InvalidCSharpAssemblyName = "InvalidCSharp";

    public static Type GetCallbacksType()
    {
        var asm = Assembly.Load(InvalidCSharpAssemblyName);
        return asm.GetType("InvalidCSharp.Callbacks");
    }

    public static Type GetGenericClassOfIntType()
    {
        var asm = Assembly.Load(InvalidCSharpAssemblyName);
        return asm.GetType("InvalidCSharp.GenericClass`1").MakeGenericType(typeof(int));
    }

    private delegate int IntNativeMethodInvoker();
    private delegate void NativeMethodInvoker();

    public static int Main(string[] args)
    {
        try
        {
            TestUnmanagedCallersOnlyValid();
            TestUnmanagedCallersOnlyValid_OnNewNativeThread();
            TestUnmanagedCallersOnlyValid_PrepareMethod();
            TestUnmanagedCallersOnlyMultipleTimesValid();
            NegativeTest_NonStaticMethod();
            NegativeTest_ViaDelegate();
            NegativeTest_NonBlittable();
            NegativeTest_NonInstantiatedGenericArguments();
            NegativeTest_InstantiatedGenericArguments();
            NegativeTest_FromInstantiatedGenericClass();
            TestUnmanagedCallersOnlyViaUnmanagedCalli();
            TestPInvokeMarkedWithUnmanagedCallersOnly();

            // Exception handling is only supported on Windows.
            if (TestLibrary.Utilities.IsWindows)
            {
                TestUnmanagedCallersOnlyValid_ThrowException();
                TestUnmanagedCallersOnlyViaUnmanagedCalli_ThrowException();
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

    [UnmanagedCallersOnly]
    public static int ManagedDoubleCallback(int n)
    {
        return DoubleImpl(n);
    }

    private static int DoubleImpl(int n)
    {
        return 2 * n;
    }

    public static void TestUnmanagedCallersOnlyValid()
    {
        Console.WriteLine($"Running {nameof(TestUnmanagedCallersOnlyValid)}...");

        /*
           void UnmanagedCallersOnly()
           {
                .locals init ([0] native int ptr)
                IL_0000:  nop
                IL_0001:  ldftn      int32 ManagedDoubleCallback(int32)
                IL_0007:  stloc.0

                IL_0008:  ldloc.0
                IL_0009:  ldc.i4     <n> local
                IL_000e:  call       bool UnmanagedCallersOnlyDll::CallManagedProc(native int, int)

                IL_0013:  ret
             }
        */
        DynamicMethod testUnmanagedCallersOnly = new DynamicMethod("UnmanagedCallersOnly", typeof(int), null, typeof(Program).Module);
        ILGenerator il = testUnmanagedCallersOnly.GetILGenerator();
        il.DeclareLocal(typeof(IntPtr));
        il.Emit(OpCodes.Nop);

        // Get native function pointer of the callback
        il.Emit(OpCodes.Ldftn, typeof(Program).GetMethod(nameof(ManagedDoubleCallback)));
        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Ldloc_0);

        int n = 12345;
        il.Emit(OpCodes.Ldc_I4, n);
        il.Emit(OpCodes.Call, typeof(UnmanagedCallersOnlyDll).GetMethod("CallManagedProc"));
        il.Emit(OpCodes.Ret);
        var testNativeMethod = (IntNativeMethodInvoker)testUnmanagedCallersOnly.CreateDelegate(typeof(IntNativeMethodInvoker));

        int expected = DoubleImpl(n);
        Assert.AreEqual(expected, testNativeMethod());
    }

    public static void TestUnmanagedCallersOnlyValid_OnNewNativeThread()
    {
        Console.WriteLine($"Running {nameof(TestUnmanagedCallersOnlyValid_OnNewNativeThread)}...");

        /*
           void UnmanagedCallersOnlyOnNewNativeThread()
           {
                .locals init ([0] native int ptr)
                IL_0000:  nop
                IL_0001:  ldftn      int32 ManagedDoubleCallback(int32)
                IL_0007:  stloc.0

                IL_0008:  ldloc.0
                IL_0009:  ldc.i4     <n> local
                IL_000e:  call       bool UnmanagedCallersOnlyDll::CallManagedProcOnNewThread(native int, int)

                IL_0013:  ret
             }
        */
        DynamicMethod testUnmanagedCallersOnly = new DynamicMethod("UnmanagedCallersOnlyOnNewNativeThread", typeof(int), null, typeof(Program).Module);
        ILGenerator il = testUnmanagedCallersOnly.GetILGenerator();
        il.DeclareLocal(typeof(IntPtr));
        il.Emit(OpCodes.Nop);

        // Get native function pointer of the callback
        il.Emit(OpCodes.Ldftn, typeof(Program).GetMethod(nameof(ManagedDoubleCallback)));
        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Ldloc_0);

        int n = 12345;
        il.Emit(OpCodes.Ldc_I4, n);
        il.Emit(OpCodes.Call, typeof(UnmanagedCallersOnlyDll).GetMethod("CallManagedProcOnNewThread"));
        il.Emit(OpCodes.Ret);
        var testNativeMethod = (IntNativeMethodInvoker)testUnmanagedCallersOnly.CreateDelegate(typeof(IntNativeMethodInvoker));

        int expected = DoubleImpl(n);
        Assert.AreEqual(expected, testNativeMethod());
    }

    [UnmanagedCallersOnly]
    public static int ManagedCallback_Prepared(int n)
    {
        return DoubleImpl(n);
    }

    // This test is about the interaction between Tiered Compilation and the UnmanagedCallersOnlyAttribute.
    public static void TestUnmanagedCallersOnlyValid_PrepareMethod()
    {
        Console.WriteLine($"Running {nameof(TestUnmanagedCallersOnlyValid_PrepareMethod)}...");

        /*
           void UnmanagedCallersOnlyOnNewNativeThread()
           {
                .locals init ([0] native int ptr)
                IL_0000:  nop
                IL_0001:  ldftn      int32 ManagedCallback_Prepared(int32)
                IL_0007:  stloc.0

                IL_0008:  ldloc.0
                IL_0009:  ldc.i4     <n> local
                IL_000e:  call       bool UnmanagedCallersOnlyDll::CallManagedProcOnNewThread(native int, int)

                IL_0013:  ret
             }
        */
        DynamicMethod testUnmanagedCallersOnly = new DynamicMethod("UnmanagedCallersOnlyValid_PrepareMethod", typeof(int), null, typeof(Program).Module);
        ILGenerator il = testUnmanagedCallersOnly.GetILGenerator();
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
        il.Emit(OpCodes.Call, typeof(UnmanagedCallersOnlyDll).GetMethod("CallManagedProcOnNewThread"));
        il.Emit(OpCodes.Ret);
        var testNativeMethod = (IntNativeMethodInvoker)testUnmanagedCallersOnly.CreateDelegate(typeof(IntNativeMethodInvoker));

        // Call enough to attempt to trigger Tiered Compilation from a new thread.
        for (int i = 0; i < 100; ++i)
        {
            testNativeMethod();
        }
    }

    [UnmanagedCallersOnly]
    public static int ManagedDoubleInNativeCallback(int n)
    {
        // This callback is designed to test if the JIT handles
        // cases where a P/Invoke is inlined into a function
        // marked with UnmanagedCallersOnly.
        return UnmanagedCallersOnlyDll.DoubleImplNative(n);
    }

    public static void TestUnmanagedCallersOnlyMultipleTimesValid()
    {
        Console.WriteLine($"Running {nameof(TestUnmanagedCallersOnlyMultipleTimesValid)}...");

        /*
           void UnmanagedCallersOnly()
           {
                .locals init ([0] native int ptr)
                nop

                ldftn      int32 ManagedDoubleInNativeCallback(int32)
                stloc.0

                ldc.i4     <m> call count
                ldloc.0
                ldc.i4     <n> local
                call       bool UnmanagedCallersOnlyDll::CallManagedProcMultipleTimes(int, native int, int)

                ret
             }
        */
        DynamicMethod testUnmanagedCallersOnly = new DynamicMethod("UnmanagedCallersOnly", typeof(int), null, typeof(Program).Module);
        ILGenerator il = testUnmanagedCallersOnly.GetILGenerator();
        il.DeclareLocal(typeof(IntPtr));
        il.Emit(OpCodes.Nop);

        // Get native function pointer of the callback
        il.Emit(OpCodes.Ldftn, typeof(Program).GetMethod(nameof(ManagedDoubleInNativeCallback)));
        il.Emit(OpCodes.Stloc_0);

        int callCount = 7;
        il.Emit(OpCodes.Ldc_I4, callCount);

        il.Emit(OpCodes.Ldloc_0);

        int n = 12345;
        il.Emit(OpCodes.Ldc_I4, n);
        il.Emit(OpCodes.Call, typeof(UnmanagedCallersOnlyDll).GetMethod("CallManagedProcMultipleTimes"));
        il.Emit(OpCodes.Ret);
        var testNativeMethod = (IntNativeMethodInvoker)testUnmanagedCallersOnly.CreateDelegate(typeof(IntNativeMethodInvoker));

        int expected = 0;
        for (int i = 0; i < callCount; ++i)
        {
            expected += DoubleImpl(n);
        }
        Assert.AreEqual(expected, testNativeMethod());
    }

    private const int CallbackThrowsErrorCode = 27;

    [UnmanagedCallersOnly]
    public static int CallbackThrows(int val)
    {
        throw new Exception() { HResult = CallbackThrowsErrorCode };
    }

    public static void TestUnmanagedCallersOnlyValid_ThrowException()
    {
        Console.WriteLine($"Running {nameof(TestUnmanagedCallersOnlyValid_ThrowException)}...");

        /*
           void UnmanagedCallersOnlyValid_ThrowException()
           {
                .locals init ([0] native int ptr)
                IL_0000:  nop
                IL_0001:  ldftn      int32 CallbackThrows(int32)
                IL_0007:  stloc.0

                IL_0008:  ldloc.0
                IL_0009:  ldc.i4     <n> local
                IL_000e:  call       bool UnmanagedCallersOnlyDll::CallManagedProcCatchException(native int, int)

                IL_0013:  ret
             }
        */
        DynamicMethod testUnmanagedCallersOnly = new DynamicMethod("UnmanagedCallersOnlyValid_ThrowException", typeof(int), null, typeof(Program).Module);
        ILGenerator il = testUnmanagedCallersOnly.GetILGenerator();
        il.DeclareLocal(typeof(IntPtr));
        il.Emit(OpCodes.Nop);

        // Get native function pointer of the callback
        il.Emit(OpCodes.Ldftn, typeof(Program).GetMethod(nameof(CallbackThrows)));
        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Ldloc_0);

        int n = 12345;
        il.Emit(OpCodes.Ldc_I4, n);
        il.Emit(OpCodes.Call, typeof(UnmanagedCallersOnlyDll).GetMethod("CallManagedProcCatchException"));
        il.Emit(OpCodes.Ret);
        var testNativeMethod = (IntNativeMethodInvoker)testUnmanagedCallersOnly.CreateDelegate(typeof(IntNativeMethodInvoker));

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


    public static void NegativeTest_NonStaticMethod()
    {
        Console.WriteLine($"Running {nameof(NegativeTest_NonStaticMethod)}...");

        /*
           void TestUnmanagedCallersOnlyNonStatic()
           {
                .locals init ([0] native int ptr)
                nop
                ldftn      int GetCallbacksType().CallbackNonStatic(int)
                stloc.0

                ldloc.0
                ldc.i4     <n> local
                call       bool UnmanagedCallersOnlyDll::CallManagedProc(native int, int)
                pop

                ret
             }
        */
        DynamicMethod testUnmanagedCallersOnly = new DynamicMethod("TestUnmanagedCallersOnlyNonStatic", null, null, typeof(Program).Module);
        ILGenerator il = testUnmanagedCallersOnly.GetILGenerator();
        il.DeclareLocal(typeof(IntPtr));
        il.Emit(OpCodes.Nop);

        // Get native function pointer of the callback
        il.Emit(OpCodes.Ldftn, GetCallbacksType().GetMethod("CallbackNonStatic"));
        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Ldloc_0);

        int n = 12345;
        il.Emit(OpCodes.Ldc_I4, n);
        il.Emit(OpCodes.Call, typeof(UnmanagedCallersOnlyDll).GetMethod("CallManagedProc"));
        il.Emit(OpCodes.Pop);
        il.Emit(OpCodes.Ret);

        var testNativeMethod = (NativeMethodInvoker)testUnmanagedCallersOnly.CreateDelegate(typeof(NativeMethodInvoker));

        // Try invoking method
        Assert.Throws<InvalidProgramException>(() => { testNativeMethod(); });
    }

    [UnmanagedCallersOnly]
    public static int CallbackMethodNonBlittable(bool x1)
    {
        Assert.Fail($"Functions with attribute {nameof(UnmanagedCallersOnlyAttribute)} cannot have non-blittable arguments");
        return -1;
    }

    public static void NegativeTest_NonBlittable()
    {
        Console.WriteLine($"Running {nameof(NegativeTest_NonBlittable)}...");

        /*
           void TestUnmanagedCallersOnlyNonBlittable()
           {
                .locals init ([0] native int ptr)
                nop
                ldftn      int CallbackMethodNonBlittable(bool)
                stloc.0

                ldloc.0
                ldc.i4     <n> local
                call       bool UnmanagedCallersOnlyDll::CallManagedProc(native int, int)
                pop

                ret
             }
        */
        DynamicMethod testUnmanagedCallersOnly = new DynamicMethod("TestUnmanagedCallersOnlyNonBlittable", null, null, typeof(Program).Module);
        ILGenerator il = testUnmanagedCallersOnly.GetILGenerator();
        il.DeclareLocal(typeof(IntPtr));
        il.Emit(OpCodes.Nop);

        // Get native function pointer of the callback
        il.Emit(OpCodes.Ldftn, typeof(Program).GetMethod(nameof(CallbackMethodNonBlittable)));
        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Ldloc_0);

        int n = 12345;
        il.Emit(OpCodes.Ldc_I4, n);
        il.Emit(OpCodes.Call, typeof(UnmanagedCallersOnlyDll).GetMethod("CallManagedProc"));
        il.Emit(OpCodes.Pop);
        il.Emit(OpCodes.Ret);

        var testNativeMethod = (NativeMethodInvoker)testUnmanagedCallersOnly.CreateDelegate(typeof(NativeMethodInvoker));

        // Try invoking method
        Assert.Throws<InvalidProgramException>(() => { testNativeMethod(); });
    }

    public static void NegativeTest_NonInstantiatedGenericArguments()
    {
        Console.WriteLine($"Running {nameof(NegativeTest_NonInstantiatedGenericArguments)}...");

        /*
           void TestUnmanagedCallersOnlyNonInstGenericArguments()
           {
                .locals init ([0] native int ptr)
                IL_0000:  nop
                IL_0001:  ldftn      void InvalidCSharp.Callbacks.CallbackMethodGeneric(T)
                IL_0007:  stloc.0
                IL_0008:  ret
             }
        */
        DynamicMethod testUnmanagedCallersOnly = new DynamicMethod("TestUnmanagedCallersOnlyNonInstGenericArguments", null, null, typeof(Program).Module);
        ILGenerator il = testUnmanagedCallersOnly.GetILGenerator();
        il.DeclareLocal(typeof(IntPtr));
        il.Emit(OpCodes.Nop);

        // Get native function pointer of the callback
        il.Emit(OpCodes.Ldftn, GetCallbacksType().GetMethod("CallbackMethodGeneric"));
        il.Emit(OpCodes.Stloc_0);

        il.Emit(OpCodes.Ret);
        var testNativeMethod = (NativeMethodInvoker)testUnmanagedCallersOnly.CreateDelegate(typeof(NativeMethodInvoker));

        // Try invoking method
        Assert.Throws<InvalidProgramException>(() => { testNativeMethod(); });
    }

    public static void NegativeTest_InstantiatedGenericArguments()
    {
        Console.WriteLine($"Running {nameof(NegativeTest_InstantiatedGenericArguments)}...");

        /*
           void TestUnmanagedCallersOnlyInstGenericArguments()
           {
                .locals init ([0] native int ptr)
                nop
                ldftn      void InvalidCSharp.Callbacks.CallbackMethodGeneric(int)
                stloc.0

                ldloc.0
                ldc.i4     <n> local
                call       bool UnmanagedCallersOnlyDll::CallManagedProc(native int, int)
                pop

                ret
             }
        */
        DynamicMethod testUnmanagedCallersOnly = new DynamicMethod("TestUnmanagedCallersOnlyInstGenericArguments", null, null, typeof(Program).Module);
        ILGenerator il = testUnmanagedCallersOnly.GetILGenerator();
        il.DeclareLocal(typeof(IntPtr));
        il.Emit(OpCodes.Nop);

        // Get native function pointer of the instantiated generic callback
        il.Emit(OpCodes.Ldftn, GetCallbacksType().GetMethod("CallbackMethodGeneric").MakeGenericMethod(new [] { typeof(int) }));
        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Ldloc_0);

        int n = 12345;
        il.Emit(OpCodes.Ldc_I4, n);
        il.Emit(OpCodes.Call, typeof(UnmanagedCallersOnlyDll).GetMethod("CallManagedProc"));
        il.Emit(OpCodes.Pop);
        il.Emit(OpCodes.Ret);

        var testNativeMethod = (NativeMethodInvoker)testUnmanagedCallersOnly.CreateDelegate(typeof(NativeMethodInvoker));

        // Try invoking method
        Assert.Throws<InvalidProgramException>(() => { testNativeMethod(); });
    }

    public static void NegativeTest_FromInstantiatedGenericClass()
    {
        Console.WriteLine($"Running {nameof(NegativeTest_FromInstantiatedGenericClass)}...");

        /*
           void TestUnmanagedCallersOnlyInstGenericType()
           {
                .locals init ([0] native int ptr)
                nop
                ldftn      int InvalidCSharp.GenericClass<int>::CallbackMethod(int)
                stloc.0

                ldloc.0
                ldc.i4     <n> local
                call       bool UnmanagedCallersOnlyDll::CallManagedProc(native int, int)
                pop

                ret
             }
        */
        DynamicMethod testUnmanagedCallersOnly = new DynamicMethod("TestUnmanagedCallersOnlyInstGenericClass", null, null, typeof(Program).Module);
        ILGenerator il = testUnmanagedCallersOnly.GetILGenerator();
        il.DeclareLocal(typeof(IntPtr));
        il.Emit(OpCodes.Nop);

        // Get native function pointer of the callback from the instantiated generic class.
        il.Emit(OpCodes.Ldftn, GetGenericClassOfIntType().GetMethod("CallbackMethod"));
        il.Emit(OpCodes.Stloc_0);
        il.Emit(OpCodes.Ldloc_0);

        int n = 12345;
        il.Emit(OpCodes.Ldc_I4, n);
        il.Emit(OpCodes.Call, typeof(UnmanagedCallersOnlyDll).GetMethod("CallManagedProc"));
        il.Emit(OpCodes.Pop);
        il.Emit(OpCodes.Ret);

        var testNativeMethod = (NativeMethodInvoker)testUnmanagedCallersOnly.CreateDelegate(typeof(NativeMethodInvoker));

        // Try invoking method
        Assert.Throws<InvalidProgramException>(() => { testNativeMethod(); });
    }

    [UnmanagedCallersOnly]
    public static void CallbackViaCalli(int val)
    {
        Assert.Fail($"Functions with attribute {nameof(UnmanagedCallersOnlyAttribute)} cannot be called via calli");
    }

    public static void NegativeTest_ViaCalli()
    {
        Console.WriteLine($"{nameof(NegativeTest_ViaCalli)} function via calli instruction. The CLR _will_ crash.");

        /*
           void TestUnmanagedCallersOnlyViaCalli()
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
        DynamicMethod testUnmanagedCallersOnly = new DynamicMethod("TestUnmanagedCallersOnlyViaCalli", null, null, typeof(Program).Module);
        ILGenerator il = testUnmanagedCallersOnly.GetILGenerator();
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

        NativeMethodInvoker testNativeMethod = (NativeMethodInvoker)testUnmanagedCallersOnly.CreateDelegate(typeof(NativeMethodInvoker));

        // It is not possible to catch the resulting ExecutionEngineException exception.
        // To observe the crashing behavior set a breakpoint in the ReversePInvokeBadTransition() function
        // located in src/vm/dllimportcallback.cpp.
        testNativeMethod();
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    public static int CallbackViaUnmanagedCalli(int val)
    {
        return DoubleImpl(val);
    }

    public static void TestUnmanagedCallersOnlyViaUnmanagedCalli()
    {
        Console.WriteLine($"Running {nameof(TestUnmanagedCallersOnlyViaUnmanagedCalli)}...");

        /*
           void UnmanagedCallersOnlyViaCalli()
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
        DynamicMethod testUnmanagedCallersOnly = new DynamicMethod("UnmanagedCallersOnlyViaUnmanagedCalli", typeof(int), null, typeof(Program).Module);
        ILGenerator il = testUnmanagedCallersOnly.GetILGenerator();
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

        IntNativeMethodInvoker testNativeMethod = (IntNativeMethodInvoker)testUnmanagedCallersOnly.CreateDelegate(typeof(IntNativeMethodInvoker));

        int expected = DoubleImpl(n);
        Assert.AreEqual(expected, testNativeMethod());
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    public static int CallbackViaUnmanagedCalliThrows(int val)
    {
        throw new Exception() { HResult = CallbackThrowsErrorCode };
    }

    public static void TestUnmanagedCallersOnlyViaUnmanagedCalli_ThrowException()
    {
        Console.WriteLine($"Running {nameof(TestUnmanagedCallersOnlyViaUnmanagedCalli_ThrowException)}...");

        /*
           void UnmanagedCallersOnlyViaUnmanagedCalli_ThrowException()
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
        DynamicMethod testUnmanagedCallersOnly = new DynamicMethod("UnmanagedCallersOnlyViaUnmanagedCalli_ThrowException", typeof(int), null, typeof(Program).Module);
        ILGenerator il = testUnmanagedCallersOnly.GetILGenerator();
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

        IntNativeMethodInvoker testNativeMethod = (IntNativeMethodInvoker)testUnmanagedCallersOnly.CreateDelegate(typeof(IntNativeMethodInvoker));

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

    public static void TestPInvokeMarkedWithUnmanagedCallersOnly()
    {
        Console.WriteLine($"Running {nameof(TestPInvokeMarkedWithUnmanagedCallersOnly)}...");

        // Call P/Invoke directly
        Assert.Throws<NotSupportedException>(() => UnmanagedCallersOnlyDll.PInvokeMarkedWithUnmanagedCallersOnly(0));

        // Call P/Invoke via reflection
        var method = typeof(UnmanagedCallersOnlyDll).GetMethod(nameof(UnmanagedCallersOnlyDll.PInvokeMarkedWithUnmanagedCallersOnly));
        Assert.Throws<NotSupportedException>(() => method.Invoke(null, BindingFlags.DoNotWrapExceptions, null, new[] { (object)0 }, null));

        // Call P/Invoke as a function pointer
        /*
           void TestPInvokeMarkedWithUnmanagedCallersOnly_Throws()
           {
                .locals init (native int V_0)
                IL_0000:  nop
                IL_0001:  ldftn      int UnmanagedCallersOnlyDll.PInvokeMarkedWithUnmanagedCallersOnly(int32)
                IL_0007:  stloc.0

                IL_0008:  ldc.i4     1234
                IL_000d:  ldloc.0
                IL_000e:  calli      int32 stdcall(int32)

                IL_0014:  ret
           }
        */

        DynamicMethod testUnmanagedCallersOnly = new DynamicMethod("TestPInvokeMarkedWithUnmanagedCallersOnly_Throws", typeof(int), null, typeof(Program).Module);
        ILGenerator il = testUnmanagedCallersOnly.GetILGenerator();
        il.DeclareLocal(typeof(IntPtr));
        il.Emit(OpCodes.Nop);

        // Get native function pointer of the callback
        il.Emit(OpCodes.Ldftn, method);
        il.Emit(OpCodes.Stloc_0);

        int n = 1234;

        il.Emit(OpCodes.Ldc_I4, n);
        il.Emit(OpCodes.Ldloc_0);
        il.EmitCalli(OpCodes.Calli, CallingConvention.StdCall, typeof(int), new Type[] { typeof(int) });

        il.Emit(OpCodes.Ret);

        IntNativeMethodInvoker testNativeMethod = (IntNativeMethodInvoker)testUnmanagedCallersOnly.CreateDelegate(typeof(IntNativeMethodInvoker));

        Assert.Throws<NotSupportedException>(() => testNativeMethod());
    }
}
