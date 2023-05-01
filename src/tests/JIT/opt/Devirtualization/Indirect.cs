// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public static unsafe class Program
{
    public static int Main()
    {
        _ = Test.ExecuteCctor();
        Test.Run();
        return 100;
    }
}

public static unsafe class Test
{
    // valid ptrs
    private static readonly delegate*<int> Ptr = &A;

    // invalid ptrs
    private static readonly delegate*<int> PtrNull = (delegate*<int>)0;
    private static readonly delegate*<int> PtrPlus1 = (delegate*<int>)(((nuint)(delegate*<int>)(&A)) + 1);
    private static readonly delegate*<int> PtrMinus1 = (delegate*<int>)(((nuint)(delegate*<int>)(&A)) - 1);
    private static readonly delegate*<int> PtrPlus16 = (delegate*<int>)(((nuint)(delegate*<int>)(&A)) + 16);
    private static readonly delegate*<int> PtrMinus16 = (delegate*<int>)(((nuint)(delegate*<int>)(&A)) - 16);
    private static readonly delegate*<int> PtrPlus32 = (delegate*<int>)(((nuint)(delegate*<int>)(&A)) + 32);
    private static readonly delegate*<int> PtrMinus32 = (delegate*<int>)(((nuint)(delegate*<int>)(&A)) - 32);
    private static readonly delegate*<int> PtrSmall = (delegate*<int>)4096;
    private static readonly delegate*<int> PtrDeadBeef = (delegate*<int>)0xDEADBEEFU;

    // valid but failing checks
    private static readonly delegate*<int> PtrWrongSig = (delegate*<int>)(delegate*<void>)&C;

    private static int A() => 1;
    private static int B() => 2;
    private static int A(int a, int b) => a + b + 1;
    private static int B(int a, int b) => a + b + 2;
    private static void C() { }
    private static void C(int a, int b) { }

    private static int D() => 3;
    [UnmanagedCallersOnly]
    private static int UnmanagedDefault() => D();
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static int UnmanagedCdecl() => D();
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int UnmanagedStdcall() => D();

    private static int D(int a, int b) => a + b + 3;
    [UnmanagedCallersOnly]
    private static int UnmanagedDefault(int a, int b) => D(a, b);
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static int UnmanagedCdecl(int a, int b) => D(a, b);
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int UnmanagedStdcall(int a, int b) => D(a, b);

    [DllImport("IndirectNative", EntryPoint = "E")]
    private static extern int E();
    [DllImport("IndirectNative", EntryPoint = "EParam")]
    private static extern int E(int a, int b);
    [DllImport("IndirectNative", EntryPoint = "EPtrs")]
    private static extern int E(ref int a, ref int b);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static delegate*<int> ExecuteCctor() => Ptr;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Run()
    {
        AssertThrowsNullReferenceException(() => ((delegate*<int>)0)());
        AssertThrowsNullReferenceException(() => PtrNull());

        AreSame(A(), Invoke(() => ((delegate*<int>)&A)()));
        AreSame(B(), Invoke(() => ((delegate*<int>)&B)()));
        AreSame(A(), Invoke(() => {
            ((delegate*<void>)&C)();
            return A();
        }));

        AreSame(D(), Invoke(() => ((delegate* unmanaged<int>)&UnmanagedDefault)()));
        AreSame(D(), Invoke(() => ((delegate* unmanaged[Cdecl]<int>)&UnmanagedCdecl)()));
        AreSame(D(), Invoke(() => ((delegate* unmanaged[Stdcall]<int>)&UnmanagedStdcall)()));

        AreSame(E(), Invoke(() => ((delegate*<int>)&E)()));

        AreSame(A(8, 9), Invoke(() => ((delegate*<int, int, int>)&A)(8, 9)));
        AreSame(B(8, 9), Invoke(() => ((delegate*<int, int, int>)&B)(8, 9)));
        AreSame(A(8, 9), Invoke(() => {
            ((delegate*<int, int, void>)&C)(8, 9);
            return A(8, 9);
        }));

        AreSame(D(8, 9), Invoke(() => ((delegate* unmanaged<int, int, int>)&UnmanagedDefault)(8, 9)));
        AreSame(D(8, 9), Invoke(() => ((delegate* unmanaged[Cdecl]<int, int, int>)&UnmanagedCdecl)(8, 9)));
        AreSame(D(8, 9), Invoke(() => ((delegate* unmanaged[Stdcall]<int, int, int>)&UnmanagedStdcall)(8, 9)));

        AreSame(E(8, 9), Invoke(() => ((delegate*<int, int, int>)&E)(8, 9)));

        AreSame(E(8, 9), Invoke(() => {
            int a = 8;
            int b = 9;
            return ((delegate*<ref int, ref int, int>)&E)(ref a, ref b);
        }));

        AreSame(A(), Invoke(() => Ptr()));

        static int CallPtr(delegate*<int> ptr) => ptr();
        AreSame(A(), Invoke(() => CallPtr(&A)));
        AreSame(B(), Invoke(() => CallPtr(&B)));

        static delegate*<int> ReturnA() => &A;
        AreSame(A(), Invoke(() => ReturnA()()));
        static delegate*<int> ReturnAStatic() => Ptr;
        AreSame(A(), Invoke(() => ReturnAStatic()()));

        static int CallPtrParam(delegate*<int, int, int> ptr, int a, int b) => ptr(a, b);
        AreSame(A(8, 9), Invoke(() => CallPtrParam(&A, 8, 9)));
        AreSame(B(8, 9), Invoke(() => CallPtrParam(&B, 8, 9)));

        static delegate*<int, int, int> ReturnAParam() => &A;
        AreSame(A(8, 9), Invoke(() => ReturnAParam()(8, 9)));

        AreSame(A(), Invoke(() =>
        {
            var ptr = (delegate*<int>)&A;
            _ = B();
            return ptr();
        }));
        AreSame(A(), Invoke(() =>
        {
            var ptr = (delegate*<int>)&A;
            _ = NoInline(B());
            return ptr();
        }));

        AreSame(A(8, 9), Invoke(() =>
        {
            var ptr = (delegate*<int, int, int>)&A;
            _ = B(8, 9);
            return ptr(8, 9);
        }));
        AreSame(A(8, 9), Invoke(() =>
        {
            var ptr = (delegate*<int, int, int>)&A;
            _ = NoInline(B(8, 9));
            return ptr(8, 9);
        }));

        static int Branch(bool a)
        {
            delegate*<int> ptr;
            if (a)
                ptr = &A;
            else
                ptr = &B;
            return ptr();
        }

        AreSame(A(), Invoke(() => Branch(true)));
        AreSame(B(), Invoke(() => Branch(false)));
        AreSame(A(), Invoke(a => Branch(a), true));
        AreSame(B(), Invoke(a => Branch(a), false));

        static int BranchParam(bool a)
        {
            delegate*<int, int, int> ptr;
            if (a)
                ptr = &A;
            else
                ptr = &B;
            return ptr(8, 9);
        }

        AreSame(A(8, 9), Invoke(() => BranchParam(true)));
        AreSame(B(8, 9), Invoke(() => BranchParam(false)));
        AreSame(A(8, 9), Invoke(a => BranchParam(a), true));
        AreSame(B(8, 9), Invoke(a => BranchParam(a), false));

        AreSame(A(), Invoke(a => a ? PtrNull() : Ptr(), false));
        AreSame(A(), Invoke(a => a ? PtrPlus1() : Ptr(), false));
        AreSame(A(), Invoke(a => a ? PtrMinus1() : Ptr(), false));
        AreSame(A(), Invoke(a => a ? PtrPlus16() : Ptr(), false));
        AreSame(A(), Invoke(a => a ? PtrMinus16() : Ptr(), false));
        AreSame(A(), Invoke(a => a ? PtrPlus32() : Ptr(), false));
        AreSame(A(), Invoke(a => a ? PtrMinus32() : Ptr(), false));
        AreSame(A(), Invoke(a => a ? PtrSmall() : Ptr(), false));
        AreSame(A(), Invoke(a => a ? PtrDeadBeef() : Ptr(), false));

        AreSame(A(), Invoke(a => a ? PtrWrongSig() : Ptr(), false));

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Assign(delegate*<int>* ptrptr, delegate*<int> ptr) => *ptrptr = ptr;

        AreSame(A(), Invoke(() =>
        {
            delegate*<int> ptr = &A;
            Assign(&ptr, &B);
            return ptr();
        }));
        AreSame(A(), Invoke(() =>
        {
            delegate*<int> ptr;
            Assign(&ptr, &B);
            ptr = &A;
            return ptr();
        }));

        static int ConditionalAddressAssign(bool a)
        {
            delegate*<int> ptr = &A;
            if (a)
                Assign(&ptr, &B);
            return ptr();
        }

        AreSame(A(), Invoke(() => ConditionalAddressAssign(false)));
        AreSame(B(), Invoke(() => ConditionalAddressAssign(true)));
        AreSame(A(), Invoke(a => ConditionalAddressAssign(a), false));
        AreSame(B(), Invoke(a => ConditionalAddressAssign(a), true));

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void AssignParam(delegate*<int, int, int>* ptrptr, delegate*<int, int, int> ptr) => *ptrptr = ptr;

        AreSame(A(8, 9), Invoke(() =>
        {
            delegate*<int, int, int> ptr = &A;
            AssignParam(&ptr, &B);
            return ptr(8, 9);
        }));
        AreSame(A(8, 9), Invoke(() =>
        {
            delegate*<int, int, int> ptr;
            AssignParam(&ptr, &B);
            ptr = &A;
            return ptr(8, 9);
        }));

        static int ConditionalAddressAssignParam(bool a, int b, int c)
        {
            delegate*<int, int, int> ptr = &A;
            if (a)
                AssignParam(&ptr, &B);
            return ptr(b, c);
        }

        AreSame(A(), Invoke(() => ConditionalAddressAssignParam(false, 8, 9)));
        AreSame(B(), Invoke(() => ConditionalAddressAssignParam(true, 8, 9)));
        AreSame(A(), Invoke(a => ConditionalAddressAssignParam(a, 8, 9), false));
        AreSame(B(), Invoke(a => ConditionalAddressAssignParam(a, 8, 9), true));

        AreSame(2, IndirectIL.StaticClass());
        AreSame(1, IndirectIL.InstanceClass());
        AreSame(1, IndirectIL.InstanceExplicitClass());
        AreSame(4, IndirectIL.StaticClassParam());
        AreSame(3, IndirectIL.InstanceClassParam());
        AreSame(3, IndirectIL.InstanceExplicitClassParam());

        AreSame(2, IndirectIL.StaticStruct());
        AreSame(1, IndirectIL.InstanceStruct());
        AreSame(1, IndirectIL.InstanceExplicitStruct());
        AreSame(4, IndirectIL.StaticStructParam());
        AreSame(3, IndirectIL.InstanceStructParam());
        AreSame(3, IndirectIL.InstanceExplicitStructParam());

        AreSame(2, Invoke(() => IndirectIL.BranchAssign(true)));
        AreSame(2, Invoke((a) => IndirectIL.BranchAssign(a), true));
        AssertThrowsNullReferenceException(() => IndirectIL.BranchAssign(false));
        AssertThrowsNullReferenceException((a) => IndirectIL.BranchAssign(a), false);

        AssertThrowsNullReferenceException(() => IndirectIL.NoAssign());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T Invoke<T>(Func<T> action) => action();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static TRet Invoke<T, TRet>(Func<T, TRet> action, T value) => action(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T NoInline<T>(T value) => value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AreSame<T>(T expected, T actual, [CallerLineNumber] int line = 0)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"Invalid value, expected {expected}, got {actual} at line {line}");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AssertThrowsNullReferenceException<T>(Func<T> a, [CallerLineNumber] int line = 0)
    {
        try
        {
            _ = a();
        }
        catch (NullReferenceException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected NullReferenceException at line {line}");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AssertThrowsNullReferenceException<TArg, T>(Func<TArg, T> a, TArg arg, [CallerLineNumber] int line = 0)
    {
        try
        {
            _ = a(arg);
        }
        catch (NullReferenceException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected NullReferenceException at line {line}");
    }
}
