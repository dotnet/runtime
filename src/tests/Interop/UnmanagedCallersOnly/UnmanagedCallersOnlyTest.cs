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
using Xunit;
using InvalidCSharp;

public unsafe class Program
{
    public static class UnmanagedCallersOnlyDll
    {
        [DllImport(nameof(UnmanagedCallersOnlyDll))]
        public static extern int CallManagedProc(IntPtr callbackProc, int n);

        [DllImport(nameof(UnmanagedCallersOnlyDll))]
        // Returns -1 if exception was throw and caught.
        public static extern int CallManagedProcCatchException(IntPtr callbackProc, int n);
    }

    private delegate int IntNativeMethodInvoker();
    private delegate void NativeMethodInvoker();

    public static int Main(string[] args)
    {
        try
        {
            NegativeTest_NonStaticMethod();
            NegativeTest_ViaDelegate();
            NegativeTest_NonBlittable();
            NegativeTest_InstantiatedGenericArguments();
            NegativeTest_FromInstantiatedGenericClass();
            TestUnmanagedCallersOnlyViaUnmanagedCalli();
            TestPInvokeMarkedWithUnmanagedCallersOnly();

            // Exception handling is only supported on CoreCLR Windows.
            if (TestLibrary.Utilities.IsWindows && !TestLibrary.Utilities.IsMonoRuntime)
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

    private static int DoubleImpl(int n)
    {
        return 2 * n;
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

        int n = 12345;
        // Method should have thrown and caught an exception.
        Assert.Equal(-1, UnmanagedCallersOnlyDll.CallManagedProcCatchException((IntPtr)(delegate* unmanaged<int, int>)&CallbackThrows, n));
    }

    public static void NegativeTest_ViaDelegate()
    {
        Console.WriteLine($"Running {nameof(NegativeTest_ViaDelegate)}...");

        // Try invoking method directly
        Assert.Throws<NotSupportedException>(() => { CallAsDelegate(); });

        // Local function to delay exception thrown during JIT
        void CallAsDelegate()
        {
            Func<int, int> invoker = CallingUnmanagedCallersOnlyDirectly.GetDoubleDelegate();
            invoker(0);
        }
    }

    public static void NegativeTest_NonStaticMethod()
    {
        Console.WriteLine($"Running {nameof(NegativeTest_NonStaticMethod)}...");

        int n = 12345;
        Assert.Throws<InvalidProgramException>(() => { UnmanagedCallersOnlyDll.CallManagedProc(Callbacks.GetNonStaticCallbackFunctionPointer(), n); });
    }

    [UnmanagedCallersOnly]
    public static int CallbackMethodNonBlittable(bool x1)
    {
        Assert.True(false, $"Functions with attribute {nameof(UnmanagedCallersOnlyAttribute)} cannot have non-blittable arguments");
        return -1;
    }

    public static void NegativeTest_NonBlittable()
    {
        Console.WriteLine($"Running {nameof(NegativeTest_NonBlittable)}...");

        int n = 12345;
        // Try invoking method
        Assert.Throws<InvalidProgramException>(() => { UnmanagedCallersOnlyDll.CallManagedProc((IntPtr)(delegate* unmanaged<bool, int>)&CallbackMethodNonBlittable, n); });
        Assert.Throws<InvalidProgramException>(() => { UnmanagedCallersOnlyDll.CallManagedProc(UnmanagedCallersOnlyWithByRefs.GetWithByRefFunctionPointer(), n); });
        Assert.Throws<InvalidProgramException>(() => { UnmanagedCallersOnlyDll.CallManagedProc(UnmanagedCallersOnlyWithByRefs.GetWithByRefInFunctionPointer(), n); });
        Assert.Throws<InvalidProgramException>(() => { UnmanagedCallersOnlyDll.CallManagedProc(UnmanagedCallersOnlyWithByRefs.GetWithByRefOutFunctionPointer(), n); });
    }

    public static void NegativeTest_InstantiatedGenericArguments()
    {
        Console.WriteLine($"Running {nameof(NegativeTest_InstantiatedGenericArguments)}...");

        int n = 12345;
        // Try invoking method
        Assert.Throws<InvalidProgramException>(() => { UnmanagedCallersOnlyDll.CallManagedProc((IntPtr)(delegate* unmanaged<int, int>)&Callbacks.CallbackMethodGeneric<int>, n); });
    }

    public static void NegativeTest_FromInstantiatedGenericClass()
    {
        Console.WriteLine($"Running {nameof(NegativeTest_FromInstantiatedGenericClass)}...");

        int n = 12345;
        // Try invoking method
        Assert.Throws<InvalidProgramException>(() => { UnmanagedCallersOnlyDll.CallManagedProc((IntPtr)(delegate* unmanaged<int, int>)&GenericClass<int>.CallbackMethod, n); });
    }

    [UnmanagedCallersOnly]
    public static void CallbackViaCalli(int val)
    {
        Assert.True(false, $"Functions with attribute {nameof(UnmanagedCallersOnlyAttribute)} cannot be called via calli");
    }

    public static void NegativeTest_ViaCalli()
    {
        Console.WriteLine($"{nameof(NegativeTest_ViaCalli)} function via calli instruction. The CLR _will_ crash.");

        // It is not possible to catch the resulting ExecutionEngineException exception.
        // To observe the crashing behavior set a breakpoint in the ReversePInvokeBadTransition() function
        // located in src/vm/dllimportcallback.cpp.
        TestNativeMethod();

        static void TestNativeMethod()
        {
            ((delegate*<int, void>)(delegate* unmanaged<int, void>)&CallbackViaCalli)(1234);
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    public static int CallbackViaUnmanagedCalli(int val)
    {
        return DoubleImpl(val);
    }

    public static void TestUnmanagedCallersOnlyViaUnmanagedCalli()
    {
        Console.WriteLine($"Running {nameof(TestUnmanagedCallersOnlyViaUnmanagedCalli)}...");

        int n = 1234;
        int expected = DoubleImpl(n);
        delegate* unmanaged[Stdcall]<int, int> nativeMethod = &CallbackViaUnmanagedCalli;
        Assert.Equal(expected, nativeMethod(n));
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    public static int CallbackViaUnmanagedCalliThrows(int val)
    {
        throw new Exception() { HResult = CallbackThrowsErrorCode };
    }

    public static void TestUnmanagedCallersOnlyViaUnmanagedCalli_ThrowException()
    {
        Console.WriteLine($"Running {nameof(TestUnmanagedCallersOnlyViaUnmanagedCalli_ThrowException)}...");

        delegate* unmanaged[Stdcall]<int, int> testNativeMethod = &CallbackViaUnmanagedCalliThrows;

        int n = 1234;
        try
        {
            testNativeMethod(n);
            Assert.True(false, $"Function {nameof(CallbackViaUnmanagedCalliThrows)} should throw");
        }
        catch (Exception e)
        {
            Assert.Equal(CallbackThrowsErrorCode, e.HResult);
        }
    }

    public static void TestPInvokeMarkedWithUnmanagedCallersOnly()
    {
        Console.WriteLine($"Running {nameof(TestPInvokeMarkedWithUnmanagedCallersOnly)}...");

        // Call P/Invoke directly
        Assert.Throws<NotSupportedException>(() => CallingUnmanagedCallersOnlyDirectly.CallPInvokeMarkedWithUnmanagedCallersOnly(0));

        // Call P/Invoke via reflection
        var method = typeof(CallingUnmanagedCallersOnlyDirectly).GetMethod(nameof(CallingUnmanagedCallersOnlyDirectly.PInvokeMarkedWithUnmanagedCallersOnly));
        Assert.Throws<NotSupportedException>(() => method.Invoke(null, BindingFlags.DoNotWrapExceptions, null, new[] { (object)0 }, null));

        // Call P/Invoke as a function pointer
        int n = 1234;
        Assert.Throws<NotSupportedException>(() => ((delegate* unmanaged<int, int>)&CallingUnmanagedCallersOnlyDirectly.PInvokeMarkedWithUnmanagedCallersOnly)(n));
    }
}
