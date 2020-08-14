// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using TestLibrary;

[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
public class CallbackTests
{
    private static readonly int seed = 123;
    private static readonly Random rand = new Random(seed);

    public static int Main()
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
        Assert.Throws<ArgumentNullException>(() => NativeLibrary.SetDllImportResolver(null, resolver), "Exception expected for null assembly parameter");
        Assert.Throws<ArgumentNullException>(() => NativeLibrary.SetDllImportResolver(assembly, null), "Exception expected for null resolver parameter");

        // No callback registered yet
        Assert.Throws<DllNotFoundException>(() => NativeSum(10, 10));

        // Set a resolver callback
        NativeLibrary.SetDllImportResolver(assembly, resolver);

        // Try to set the resolver again on the same assembly
        Assert.Throws<InvalidOperationException>(() => NativeLibrary.SetDllImportResolver(assembly, resolver), "Should not be able to re-register resolver");

        // Try to set another resolver on the same assembly
        DllImportResolver anotherResolver =
            (string libraryName, Assembly asm, DllImportSearchPath? dllImportSearchPath) =>
                IntPtr.Zero;
        Assert.Throws<InvalidOperationException>(() => NativeLibrary.SetDllImportResolver(assembly, anotherResolver), "Should not be able to register another resolver");
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
        Assert.AreEqual(expected, value, $"Unexpected return value from {nameof(NativeSum)}");
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
            Assert.AreEqual(expectedNames.Length, invocations.Count, $"Unexpected invocation count for registered {nameof(DllImportResolver)}.");
            for (int i = 0; i < expectedNames.Length; i++)
                Assert.AreEqual(expectedNames[i], invocations[i], $"Unexpected library name received by registered resolver.");
        }

        private IntPtr ResolveDllImport(string libraryName, Assembly asm, DllImportSearchPath? dllImportSearchPath)
        {
            invocations.Add(libraryName);

            if (string.Equals(libraryName, NativeLibraryToLoad.InvalidName))
            {
                Assert.AreEqual(DllImportSearchPath.System32, dllImportSearchPath, $"Unexpected {nameof(dllImportSearchPath)}: {dllImportSearchPath.ToString()}");
                return NativeLibrary.Load(NativeLibraryToLoad.Name, asm, null);
            }

            return IntPtr.Zero;
        }
    }

    [DllImport(NativeLibraryToLoad.InvalidName)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    static extern int NativeSum(int arg1, int arg2);
}
