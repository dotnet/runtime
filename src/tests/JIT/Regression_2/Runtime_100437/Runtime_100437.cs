// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Xunit;

public class Runtime_100437
{
    [Fact]
    [SkipOnMono("PlatformDetection.IsPreciseGcSupported false on mono", TestPlatforms.Any)]
    public static void TestNonCollectibleType() => TestCollectibleReadOnlyStatics(nameof(NonCollectibleType));

    [Fact]
    [SkipOnMono("PlatformDetection.IsPreciseGcSupported false on mono", TestPlatforms.Any)]
    public static void TestNonCollectibleTypeInSharedGenericCode() => TestCollectibleReadOnlyStatics(nameof(NonCollectibleTypeInSharedGenericCode));

    [Fact]
    [SkipOnMono("PlatformDetection.IsPreciseGcSupported false on mono", TestPlatforms.Any)]
    public static void TestNonCollectibleArrayTypeInSharedGenericCode() => TestCollectibleReadOnlyStatics(nameof(NonCollectibleArrayTypeInSharedGenericCode));

    [Fact]
    [SkipOnMono("PlatformDetection.IsPreciseGcSupported false on mono", TestPlatforms.Any)]
    public static void TestCollectibleEmptyArray() => TestCollectibleReadOnlyStatics(nameof(CollectibleEmptyArray));

    private static void TestCollectibleReadOnlyStatics(string methodName)
    {
        string assemblyPath = typeof(Runtime_100437).Assembly.Location;

        // Skip this test for single file
        if (string.IsNullOrEmpty(assemblyPath))
            return;

        WeakReference wr = CreateReadOnlyStaticWeakReference();

        for (int i = 0; i < 10; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            if (!IsTargetAlive(wr))
                return;
        }

        throw new Exception("Test failed - readonly static has not been collected.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        WeakReference CreateReadOnlyStaticWeakReference()
        {
            AssemblyLoadContext alc = new CollectibleAssemblyLoadContext();
            Assembly a = alc.LoadFromAssemblyPath(assemblyPath);
            return (WeakReference)a.GetType(nameof(Runtime_100437)).GetMethod(methodName).Invoke(null, new object[] { typeof(Runtime_100437).Assembly });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        bool IsTargetAlive(WeakReference wr)
        {
            return wr.Target != null;
        }
    }

    public static WeakReference NonCollectibleType(Assembly assemblyInDefaultContext)
    {
        return new WeakReference(Holder.Singleton, trackResurrection: true);
    }

    public static WeakReference NonCollectibleTypeInSharedGenericCode(Assembly assemblyInDefaultContext)
    {
        // Create instance of a non-collectible generic type definition over a collectible type
        var type = assemblyInDefaultContext.GetType("Runtime_100437+GenericHolder`1", throwOnError: true).MakeGenericType(typeof(Runtime_100437));
        var field = type.GetField("Singleton", BindingFlags.Static | BindingFlags.Public);
        return new WeakReference(field.GetValue(null), trackResurrection: true);
    }

    public static WeakReference NonCollectibleArrayTypeInSharedGenericCode(Assembly assemblyInDefaultContext)
    {
        // Create instance of a non-collectible generic type definition over a collectible type
        var type = assemblyInDefaultContext.GetType("Runtime_100437+GenericArrayHolder`1", throwOnError: true).MakeGenericType(typeof(Runtime_100437));
        var field = type.GetField("Singleton", BindingFlags.Static | BindingFlags.Public);
        return new WeakReference(field.GetValue(null), trackResurrection: true);
    }

    public static WeakReference CollectibleEmptyArray(Assembly assemblyInDefaultContext)
    {
        return new WeakReference(Array.Empty<Runtime_100437>(), trackResurrection: true);
    }

    private class CollectibleAssemblyLoadContext : AssemblyLoadContext
    {
        public CollectibleAssemblyLoadContext()
            : base(isCollectible: true)
        {
        }
    }

    private class Holder
    {
        public static readonly object Singleton = new object();
    }

    private class GenericHolder<T>
    {
        public static readonly object Singleton = new object();
    }

    private class GenericArrayHolder<T>
    {
        public static readonly int[] Singleton = new int[0];
    }
}
