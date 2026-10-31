// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: TypeMap<ConditionalTypeReferences.Map>("exact", typeof(ConditionalTypeReferences.Entry), typeof(ConditionalTypeReferences.ExactOnly))]
[assembly: TypeMap<ConditionalTypeReferences.Map>("shared", typeof(ConditionalTypeReferences.Entry), typeof(ConditionalTypeReferences.SharedOnly))]
[assembly: TypeMap<ConditionalTypeReferences.Map>("runtime", typeof(ConditionalTypeReferences.Entry), typeof(ConditionalTypeReferences.RuntimeOnly))]
[assembly: TypeMap<ConditionalTypeReferences.Map>("upgraded", typeof(ConditionalTypeReferences.Entry), typeof(ConditionalTypeReferences.Upgraded))]
[assembly: TypeMap<ConditionalTypeReferences.Map>("allocated", typeof(ConditionalTypeReferences.Entry), typeof(ConditionalTypeReferences.Allocated))]
[assembly: TypeMap<ConditionalTypeReferences.Map>("metadata", typeof(ConditionalTypeReferences.Entry), typeof(ConditionalTypeReferences.MetadataOnly))]
[assembly: TypeMap<ConditionalTypeReferences.Map>("constructed", typeof(ConditionalTypeReferences.Entry), typeof(ConditionalTypeReferences.ConstructedOnly<int>))]

class ConditionalTypeReferences
{
    internal class Map;
    internal class Entry;
    internal struct ExactOnly;
    internal class SharedOnly;
    internal class RuntimeOnly;
    internal class Upgraded;
    internal class Allocated;
    internal class MetadataOnly;
    internal struct ConstructedOnly<T>;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Exact<T>()
    {
        if (typeof(T) == typeof(int))
            return 17;
        if (typeof(T) == typeof(ExactOnly))
            return 11;
        return 7;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Constructed<T>()
    {
        // The vector-width checks in Ascii.Widen have this shape. Removing only the equality
        // operator left ordinary type loads behind, promoting them to reflection-visible types.
        if (typeof(T) == typeof(ConstructedOnly<int>))
            return 11;
        return 7;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Shared<T>() where T : class
    {
        if (typeof(T) != typeof(SharedOnly))
            return false;
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static object Runtime(object value)
    {
        // The conditional allocation must not upgrade its own guarding type comparison.
        // GetType must still run (and throw for null) if the comparison is removed.
        if (typeof(RuntimeOnly) == value.GetType())
            return new RuntimeOnly();
        return null;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Upgrade(Type value)
    {
        bool match = value == typeof(Upgraded);
        if (match)
            return true;
        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Type IndependentReference() => typeof(Upgraded);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool SharedUpgrade<T>() where T : class
    {
        if (typeof(T) == typeof(Upgraded))
            return true;
        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool AllocatedCheck(object value)
    {
        if (value.GetType() == typeof(Allocated))
            return true;
        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static object AllocateIndependently() => new Allocated();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool MetadataCheck(Type value)
    {
        if (value != typeof(MetadataOnly))
            return false;
        return true;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static int RemappedComparisons<T>(object value)
    {
        if (typeof(T) == typeof(ExactOnly))
            throw new Exception("The first dead branch should be removed.");
        try
        {
            if (typeof(RuntimeOnly) == value.GetType())
                return 11;
            return 7;
        }
        catch (NullReferenceException)
        {
            return 17;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool MergedLeftOperand(bool chooseUpgraded)
    {
        if ((chooseUpgraded ? typeof(Upgraded) : typeof(Allocated)) == typeof(Upgraded))
            return true;
        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool MergedRightOperand(bool chooseUpgraded)
    {
        if (typeof(Upgraded) == (chooseUpgraded ? typeof(Upgraded) : typeof(Allocated)))
            return true;
        return false;
    }

    public static int Run()
    {
        if (Exact<uint>() != 7 || Exact<int>() != 17 || Shared<string>() || Shared<object>() || Runtime(new object()) != null)
            throw new Exception("A comparison-only type unexpectedly matched.");
        if (Constructed<ConstructedOnly<long>>() != 7 || Constructed<ConstructedOnly<int>>() != 11)
            throw new Exception("A closed constructed-type comparison was rewritten incorrectly.");
        try
        {
            Runtime(null);
            throw new Exception("Removing a type comparison suppressed operand evaluation.");
        }
        catch (NullReferenceException) { }
        if (Upgrade(typeof(object)) || !Upgrade(IndependentReference()))
            throw new Exception("An independently referenced type did not upgrade its comparison.");
        if (!SharedUpgrade<Upgraded>() || SharedUpgrade<string>() || !AllocatedCheck(AllocateIndependently()))
            throw new Exception("An independently reachable instantiation did not upgrade its comparison.");
        if (!MetadataCheck(typeof(ConditionalTypeReferences).Assembly.GetType("ConditionalTypeReferences+MetadataOnly")))
            throw new Exception("An independently reachable metadata type did not upgrade its comparison.");
        if (RemappedComparisons<int>(new object()) != 7 || RemappedComparisons<int>(null) != 17)
            throw new Exception("Predicate rewriting did not preserve remapped code and exception regions.");
        if (!MergedLeftOperand(true) || MergedLeftOperand(false) || !MergedRightOperand(true) || MergedRightOperand(false))
            throw new Exception("A type predicate was rewritten across a control-flow join.");

        var map = TypeMapping.GetOrCreateExternalTypeMapping<Map>();
        if (map.TryGetValue("exact", out _) || map.TryGetValue("shared", out _) || map.TryGetValue("runtime", out _)
            || map.TryGetValue("constructed", out _))
            throw new Exception("A comparison-only type retained its TypeMap entry.");
        if (!map.TryGetValue("upgraded", out _) || !map.TryGetValue("allocated", out _) || !map.TryGetValue("metadata", out _))
            throw new Exception("An independently referenced type lost its TypeMap entry.");
        return 100;
    }
}
