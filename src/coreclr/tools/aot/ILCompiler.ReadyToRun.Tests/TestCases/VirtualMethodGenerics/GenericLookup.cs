// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

// Virtual method discovery for a generic type instantiation that is only ever
// reached through a GenericLookupSignature fixup.
//
// When compiling the shared TestB<__Canon, int>.TestCreate, the instance of
// TestA<T, int> is obtained through a generic dictionary lookup (a
// GenericLookupSignature fixup referencing TestA<__Canon, int>) rather than via
// a TypeFixupSignature. The virtual TestMethod on TestA<__Canon, int> must still
// be discovered and compiled.

public class TestA<T, U>
{
    public virtual void TestMethod(T item) => Console.WriteLine($"{item} / {typeof(U)}");
}

public class TestB<T, U>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public TestA<T, U> TestCreate() => new TestA<T, U>();
}

static class GenericLookupTests
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static TestB<T, U> CreateTestB<T, U>() => new TestB<T, U>();

    static void Run()
    {
        TestB<string, int> obj = CreateTestB<string, int>();
        obj.TestCreate().TestMethod("hello");
    }
}
