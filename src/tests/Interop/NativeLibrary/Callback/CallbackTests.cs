// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using Xunit;

[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
public class CallbackTests
{
    private static readonly int seed = 123;
    private static readonly Random rand = new Random(seed);

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            // The first test sets the resolver for the executing assembly
            // Subsequents tests assume the resolver has already been set.
            ValidateSetDllImportResolver();

            ValidatePInvoke();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }

        return 100;
    }

    public static void ValidateSetDllImportResolver()
    {
        Console.WriteLine($"Running {nameof(ValidateSetDllImportResolver)}...");
        Assembly assembly = Assembly.GetExecutingAssembly();
        DllImportResolver resolver = Resolver.Instance.Callback;

        // Invalid arguments
        Assert.Throws<ArgumentNullException>(() => NativeLibrary.SetDllImportResolver(null, resolver));
        Assert.Throws<ArgumentNullException>(() => NativeLibrary.SetDllImportResolver(assembly, null));

        // No callback registered yet
        Assert.Throws<DllNotFoundException>(() => NativeSum(10, 10));

        // Set a resolver callback
        NativeLibrary.SetDllImportResolver(assembly, resolver);

        // Try to set the resolver again on the same assembly
        Assert.Throws<InvalidOperationException>(() => NativeLibrary.SetDllImportResolver(assembly, resolver));

        // Try to set another resolver on the same assembly
        DllImportResolver anotherResolver =
            (string libraryName, Assembly asm, DllImportSearchPath? dllImportSearchPath) =>
                IntPtr.Zero;
        Assert.Throws<InvalidOperationException>(() => NativeLibrary.SetDllImportResolver(assembly, anotherResolver));
    }

    public static void ValidatePInvoke()
    {
        Console.WriteLine($"Running {nameof(ValidatePInvoke)}...");
        int addend1 = rand.Next(int.MaxValue / 2);
        int addend2 = rand.Next(int.MaxValue / 2);
        int expected = addend1 + addend2;

        Resolver.Instance.Reset();
        int value = NativeSum(addend1, addend2);
        Resolver.Instance.Validate(NativeLibraryToLoad.InvalidName);
        Assert.Equal(expected, value);
    }

    private class Resolver
    {
        public static Resolver Instance = new Resolver();

        public DllImportResolver Callback => ResolveDllImport;

        private List<string> invocations = new List<string>();

        public void Reset()
        {
            invocations.Clear();
        }

        public void Validate(params string[] expectedNames)
        {
            Assert.Equal(expectedNames.Length, invocations.Count);
            for (int i = 0; i < expectedNames.Length; i++)
                Assert.Equal(expectedNames[i], invocations[i]);
        }

        private IntPtr ResolveDllImport(string libraryName, Assembly asm, DllImportSearchPath? dllImportSearchPath)
        {
            invocations.Add(libraryName);

            if (string.Equals(libraryName, NativeLibraryToLoad.InvalidName))
            {
                Assert.Equal(DllImportSearchPath.System32, dllImportSearchPath);
                return NativeLibrary.Load(NativeLibraryToLoad.GetFullPath(), asm, null);
            }

            return IntPtr.Zero;
        }
    }

    [DllImport(NativeLibraryToLoad.InvalidName)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    static extern int NativeSum(int arg1, int arg2);
}
