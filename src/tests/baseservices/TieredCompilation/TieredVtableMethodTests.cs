// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Xunit;

public static class TieredVtableMethodTests
{
    private const int CallCountPerIteration = 8;

    private static StringBuilder s_expectedCallSequence = new StringBuilder();
    private static StringBuilder s_actualCallSequence = new StringBuilder();

    [Fact]
    public static int TestEntryPoint()
    {
        const int Pass = 100, Fail = 101;

        var baseObj = new Base();
        var derivedObj = new Derived();
        var derivedForDevirtualizationObj = new DerivedForDevirtualization();

        PromoteToTier1(
            () => CallVirtualMethod(baseObj),
            () => CallVirtualMethod(derivedObj),
            () => CallGenericVirtualMethodWithValueType(baseObj),
            () => CallGenericVirtualMethodWithValueType(derivedObj),
            () => CallGenericVirtualMethodWithReferenceType(baseObj),
            () => CallGenericVirtualMethodWithReferenceType(derivedObj),
            () => CallVirtualMethodForDevirtualization(derivedForDevirtualizationObj),
            () => CallInterfaceVirtualMethodPolymorhpic(baseObj),
            () => CallInterfaceVirtualMethodPolymorhpic(derivedObj));

        for (int i = 0; i < 4; ++i)
        {
            CallVirtualMethod(baseObj, CallCountPerIteration);
            CallVirtualMethod(derivedObj, CallCountPerIteration);
            CallGenericVirtualMethodWithValueType(baseObj, CallCountPerIteration);
            CallGenericVirtualMethodWithValueType(derivedObj, CallCountPerIteration);
            CallGenericVirtualMethodWithReferenceType(baseObj, CallCountPerIteration);
            CallGenericVirtualMethodWithReferenceType(derivedObj, CallCountPerIteration);
            CallVirtualMethodForDevirtualization(derivedForDevirtualizationObj, CallCountPerIteration);
            CallInterfaceVirtualMethodMonomorphicOnBase(baseObj, CallCountPerIteration);
            CallInterfaceVirtualMethodMonomorphicOnDerived(derivedObj, CallCountPerIteration);
            CallInterfaceVirtualMethodPolymorhpic(baseObj, CallCountPerIteration);
            CallInterfaceVirtualMethodPolymorhpic(derivedObj, CallCountPerIteration);

            for (int j = 0; j < 2; ++j)
            {
                RunCollectibleIterations();

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.WaitForPendingFinalizers();
            }
        }

        if (s_actualCallSequence.Equals(s_expectedCallSequence))
        {
            return Pass;
        }

        Console.WriteLine($"Expected:  {s_expectedCallSequence}");
        Console.WriteLine($"Actual:    {s_actualCallSequence}");
        return Fail;
    }

    /// Creates a collectible type deriving from <see cref="Base"/> similar to <see cref="Derived"/>. The collectible derived
    /// type inherits vtable slots from the base. After multiple iterations of the test, the collectible type will be collected
    /// and replaced with another new collectible type. This is used to cover vtable slot backpatching and cleanup of recorded
    /// slots in collectible types.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RunCollectibleIterations()
    {
        Base collectibleDerivedObj = CreateCollectibleDerived();

        PromoteToTier1(
            () => CallVirtualMethod(collectibleDerivedObj),
            () => CallGenericVirtualMethodWithValueType(collectibleDerivedObj),
            () => CallGenericVirtualMethodWithReferenceType(collectibleDerivedObj),
            () => CallInterfaceVirtualMethodPolymorhpic(collectibleDerivedObj));

        CallVirtualMethod(collectibleDerivedObj, CallCountPerIteration);
        CallGenericVirtualMethodWithValueType(collectibleDerivedObj, CallCountPerIteration);
        CallGenericVirtualMethodWithReferenceType(collectibleDerivedObj, CallCountPerIteration);
        CallInterfaceVirtualMethodPolymorhpic(collectibleDerivedObj, CallCountPerIteration);
    }

    public interface IBase
    {
        void InterfaceVirtualMethod();
    }

    public class Base : IBase
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual void VirtualMethod()
        {
            s_actualCallSequence.Append("v ");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual void GenericVirtualMethod<T>(T t)
        {
            s_actualCallSequence.Append(typeof(T).IsValueType ? "gvv " : "gvr ");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual void VirtualMethodForDevirtualization()
        {
            s_actualCallSequence.Append("vd ");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual void InterfaceVirtualMethod()
        {
            s_actualCallSequence.Append("iv ");
        }
    }

    private class Derived : Base
    {
        // Prevent this type from sharing the vtable chunk from the base
        public virtual void VirtualMethod2()
        {
        }
    }

    // Derived type that is sealed for testing devirtualization of calls to inherited virtual methods
    private sealed class DerivedForDevirtualization : Derived
    {
        // Prevent this type from sharing the vtable chunk from the base
        public override void VirtualMethod()
        {
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CallVirtualMethod(Base obj, int count = 1)
    {
        for (int i = 0; i < count; ++i)
        {
            s_expectedCallSequence.Append("v ");
            obj.VirtualMethod();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CallGenericVirtualMethodWithValueType(Base obj, int count = 1)
    {
        for (int i = 0; i < count; ++i)
        {
            s_expectedCallSequence.Append("gvv ");
            obj.GenericVirtualMethod(0);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CallGenericVirtualMethodWithReferenceType(Base obj, int count = 1)
    {
        var objArg = new object();
        for (int i = 0; i < count; ++i)
        {
            s_expectedCallSequence.Append("gvr ");
            obj.GenericVirtualMethod(objArg);
        }
    }

    /// The virtual call in this method may be devirtualized because <see cref="DerivedForDevirtualization"/> is sealed
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CallVirtualMethodForDevirtualization(DerivedForDevirtualization obj, int count = 1)
    {
        for (int i = 0; i < count; ++i)
        {
            s_expectedCallSequence.Append("vd ");
            obj.VirtualMethodForDevirtualization();
        }
    }

    /// The interface call site in this method is monomorphic on <see cref="Base"/> and is used to cover dispatch stub
    /// backpatching
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CallInterfaceVirtualMethodMonomorphicOnBase(IBase obj, int count = 1)
    {
        for (int i = 0; i < count; ++i)
        {
            s_expectedCallSequence.Append("iv ");
            obj.InterfaceVirtualMethod();
        }
    }

    /// The interface call site in this method is monomorphic on <see cref="Base"/> and is used to cover dispatch stub
    /// backpatching
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CallInterfaceVirtualMethodMonomorphicOnDerived(IBase obj, int count = 1)
    {
        for (int i = 0; i < count; ++i)
        {
            s_expectedCallSequence.Append("iv ");
            obj.InterfaceVirtualMethod();
        }
    }

    // The call site in this method is polymorphic and is used to cover resolve cache entry backpatching
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CallInterfaceVirtualMethodPolymorhpic(IBase obj, int count = 1)
    {
        for (int i = 0; i < count; ++i)
        {
            s_expectedCallSequence.Append("iv ");
            obj.InterfaceVirtualMethod();
        }
    }

    private static ulong s_collectibleIndex = 0;

    private static Base CreateCollectibleDerived()
    {
        ulong collectibleIndex = s_collectibleIndex++;

        var ab =
            AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName($"CollectibleDerivedAssembly{collectibleIndex}"),
                AssemblyBuilderAccess.RunAndCollect);
        var mob = ab.DefineDynamicModule($"CollectibleDerivedModule{collectibleIndex}");
        var tb =
            mob.DefineType(
                $"CollectibleDerived{collectibleIndex}",
                TypeAttributes.Class | TypeAttributes.Public,
                typeof(Base));

        /// Add a virtual method to prevent this type from sharing the vtable chunk from the base, similarly to what is done in
        /// <see cref="Derived"/>
        {
            var mb =
                tb.DefineMethod(
                    "VirtualMethod2",
                    MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot);
            var ilg = mb.GetILGenerator();
            ilg.Emit(OpCodes.Ret);
        }

        return (Base)Activator.CreateInstance(tb.CreateTypeInfo());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void PromoteToTier1(params Action[] actions)
    {
        // Call the methods once to register a call each for call counting
        foreach (Action action in actions)
        {
            action();
        }

        // Allow time for call counting to begin
        Thread.Sleep(500);

        // Call the methods enough times to trigger tier 1 promotion
        for (int i = 0; i < 100; ++i)
        {
            foreach (Action action in actions)
            {
                action();
            }
        }

        // Allow time for the methods to be jitted at tier 1
        Thread.Sleep(Math.Max(500, 100 * actions.Length));
    }
}
