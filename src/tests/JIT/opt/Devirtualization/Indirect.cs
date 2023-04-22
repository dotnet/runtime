// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    private static void C() { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static delegate*<int> ExecuteCctor() => Ptr;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Run()
    {
        AssertThrowsNullReferenceException(() => ((delegate*<int>)0)());
        AssertThrowsNullReferenceException(() => PtrNull());

        AreSame(A(), Invoke(() => ((delegate*<int>)&A)()));
        AreSame(B(), Invoke(() => ((delegate*<int>)&B)()));

        AreSame(A(), Invoke(() => Ptr()));

        static int CallPtr(delegate*<int> ptr) => ptr();
        AreSame(A(), Invoke(() => CallPtr(&A)));
        AreSame(B(), Invoke(() => CallPtr(&B)));

        static delegate*<int> ReturnA() => &A;
        AreSame(A(), Invoke(() => ReturnA()()));
        static delegate*<int> ReturnAStatic() => Ptr;
        AreSame(A(), Invoke(() => ReturnAStatic()()));

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
}
