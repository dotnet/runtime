// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Regression test for debug assertion in FindTightlyBoundWrappedMethodDesc_DEBUG
// and FindTightlyBoundUnboxingStub_DEBUG (src/coreclr/vm/genmeth.cpp).
//
// These debug-only verification functions previously used MethodTable::MethodIterator
// (slot-based) with IsVirtual()/!IsVirtual() filters that could miss certain
// MethodDescs not reachable through the expected slot range. The production functions
// use IntroducedMethodIterator (chunk-based) which correctly enumerates all methods.
// This mismatch caused debug assertions when the production function found a method
// that the debug function missed.
//
// For generic value types, the MethodTableBuilder allocates a vtable slot for the
// "unboxed copy" of each method (via cVtableSlots++), putting it in the virtual slot
// range. The old FindTightlyBoundWrappedMethodDesc_DEBUG only searched non-virtual
// slots (!IsVirtual() filter), so it missed the wrapped method and returned NULL.
//
// This test exercises the code path by calling virtual methods on generic structs.
// During JIT compilation, getCallInfo calls FindOrCreateAssociatedMethodDesc with
// allowInstParam=TRUE, which triggers FindTightlyBoundWrappedMethodDesc on the
// canonical method table. In Debug builds without the fix, this crashes with an
// assertion failure.
//

using System;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.CompilerServices;
using TestLibrary;
using Xunit;

public interface IProcessor<T>
{
    int Process(T value);
    bool Check(T value);
}

public interface IIdentity
{
    string Identify();
}

public struct Processor<T> : IProcessor<T>, IIdentity
{
    public T Value;

    public int Process(T value)
    {
        Value = value;
        return 1;
    }

    public bool Check(T value) => value is not null;

    public string Identify() => typeof(T).Name;

    public override string ToString() => Value?.ToString() ?? "";
    public override int GetHashCode() => Value?.GetHashCode() ?? 0;
    public override bool Equals(object obj) => obj is Processor<T>;
}

public class Test_Struct_UnboxingStubLookup
{
    // These methods are NoInlining to ensure each is JIT-compiled separately.
    // During JIT compilation of each method, getCallInfo resolves the virtual
    // method call on the generic struct, triggering FindOrCreateAssociatedMethodDesc
    // → FindTightlyBoundWrappedMethodDesc on the canonical method table.

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int CallProcess<T>(Processor<T> p, T val) => p.Process(val);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool CallCheck<T>(Processor<T> p, T val) => p.Check(val);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static string CallIdentify<T>(Processor<T> p) => p.Identify();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static string CallToString<T>(Processor<T> p) => p.ToString();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int CallGetHashCode<T>(Processor<T> p) => p.GetHashCode();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int BoxAndDispatch<T>(Processor<T> p, T val)
    {
        IProcessor<T> iface = p;
        return iface.Process(val);
    }

    /*public static Task<int> Main()
    {
        return Task.FromResult(TestEntryPoint());
    }*/

    [Fact]
    public static int TestEntryPoint()
    {
        bool pass = true;

        pass &= TestWithReferenceType("hello", "world");
        pass &= TestWithReferenceType<object>(new object(), new object());
        pass &= TestWithReferenceType<int[]>(new[] { 1 }, new[] { 2 });

        pass &= TestWithValueType(42, 99);
        pass &= TestWithValueType(3.14, 2.71);
        pass &= TestWithValueType(Guid.NewGuid(), Guid.NewGuid());

        // Also exercise via reflection to trigger FindTightlyBoundUnboxingStub
        // through FindOrCreateAssociatedMethodDescForReflection
        pass &= TestViaReflection<Processor<string>>(new Processor<string>());
        pass &= TestViaReflection<Processor<object>>(new Processor<object>());
        pass &= TestViaReflection<Processor<int>>(new Processor<int>());

        if (pass)
        {
            Console.WriteLine("PASS");
            return 100;
        }
        else
        {
            Console.WriteLine("FAIL");
            return 101;
        }
    }

    static bool TestWithReferenceType<T>(T val1, T val2) where T : class
    {
        Processor<T> p = new Processor<T>();

        if (CallProcess(p, val1) != 1)
        {
            Console.WriteLine($"FAIL: Process<{typeof(T).Name}>");
            return false;
        }
        if (!CallCheck(p, val2))
        {
            Console.WriteLine($"FAIL: Check<{typeof(T).Name}>");
            return false;
        }

        CallIdentify(p);
        CallToString(p);
        CallGetHashCode(p);

        if (BoxAndDispatch(p, val1) != 1)
        {
            Console.WriteLine($"FAIL: BoxAndDispatch<{typeof(T).Name}>");
            return false;
        }

        return true;
    }

    static bool TestWithValueType<T>(T val1, T val2) where T : struct
    {
        Processor<T> p = new Processor<T>();

        if (CallProcess(p, val1) != 1)
        {
            Console.WriteLine($"FAIL: Process<{typeof(T).Name}>");
            return false;
        }

        CallIdentify(p);
        CallToString(p);
        CallGetHashCode(p);

        if (BoxAndDispatch(p, val1) != 1)
        {
            Console.WriteLine($"FAIL: BoxAndDispatch<{typeof(T).Name}>");
            return false;
        }

        return true;
    }

    static bool TestViaReflection<T>(T value) where T : struct
    {
        // Enumerating interface methods via reflection triggers
        // FindOrCreateAssociatedMethodDescForReflection, which calls
        // FindOrCreateAssociatedMethodDesc with forceBoxedEntryPoint=TRUE
        // for virtual methods on value types.
        Type type = typeof(T);
        int methodCount = 0;

        foreach (Type iface in type.GetInterfaces())
        {
            try
            {
                InterfaceMapping map = type.GetInterfaceMap(iface);
                for (int i = 0; i < map.TargetMethods.Length; i++)
                {
                    if (map.TargetMethods[i] is null)
                    {
                        Console.WriteLine($"FAIL: null target for {type.Name}::{iface.Name}");
                        return false;
                    }
                    methodCount++;
                }
            }
            catch (ArgumentException)
            {
                // Some interfaces may not support GetInterfaceMap
            }
        }

        if (methodCount == 0)
        {
            Console.WriteLine($"FAIL: no interface methods resolved for {type.Name}");
            return false;
        }

        return true;
    }
}
