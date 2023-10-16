// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Reflection;
using System.Runtime.InteropServices;

using Xunit;

public class FakeNativeLibrary
{
    public const string Name = "FakeNativeLibrary";
    public const string RedirectName = "FakeNativeLibraryRedirect";

    public static readonly IntPtr Handle = new IntPtr(ResolveUnmanagedDllTests.rand.Next());
}

public class ALC : AssemblyLoadContext
{
    private List<string> invocations = new List<string>();

    public void Reset()
    {
        invocations.Clear();
    }

    public void Validate(params string[] expectedNames)
    {
        AssertExtensions.CollectionEqual(expectedNames, invocations);
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        invocations.Add(unmanagedDllName);

        if (string.Equals(unmanagedDllName, NativeLibraryToLoad.InvalidName))
            return LoadUnmanagedDllFromPath(NativeLibraryToLoad.GetFullPath());

        if (string.Equals(unmanagedDllName, FakeNativeLibrary.Name))
            return FakeNativeLibrary.Handle;

        if (string.Equals(unmanagedDllName, FakeNativeLibrary.RedirectName))
        {
            IntPtr ptr;
            if (NativeLibrary.TryLoad(FakeNativeLibrary.Name, Assemblies.First(), null, out ptr))
                return ptr;
        }

        return IntPtr.Zero;
    }
}

public class ResolveUnmanagedDllTests
{
    private static readonly int seed = 123;
    internal static readonly Random rand = new Random(seed);

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            ValidateLoadUnmanagedDll();
            ValidateResolvingUnmanagedDllEvent();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }

        return 100;
    }

    public static void ValidateLoadUnmanagedDll()
    {
        Console.WriteLine($"Running {nameof(ValidateLoadUnmanagedDll)}...");

        ALC alc = new ALC();
        var asm = alc.LoadFromAssemblyPath(Assembly.GetExecutingAssembly().Location);

        Console.WriteLine(" -- Validate explicit load...");

        // ALC implementation returns a fake handle value
        IntPtr ptr = NativeLibrary.Load(FakeNativeLibrary.Name, asm, null);
        alc.Validate(FakeNativeLibrary.Name);
        Assert.Equal(FakeNativeLibrary.Handle, ptr);

        alc.Reset();
        ptr = IntPtr.Zero;

        bool success = NativeLibrary.TryLoad(FakeNativeLibrary.Name, asm, null, out ptr);
        Assert.True(success, $"NativeLibrary.TryLoad should have succeeded");
        alc.Validate(FakeNativeLibrary.Name);
        Assert.Equal(FakeNativeLibrary.Handle, ptr);

        alc.Reset();

        // ALC implementation calls NativeLibrary.TryLoad with a different name
        ptr = NativeLibrary.Load(FakeNativeLibrary.RedirectName, asm, null);
        alc.Validate(FakeNativeLibrary.RedirectName, FakeNativeLibrary.Name);
        Assert.Equal(FakeNativeLibrary.Handle, ptr);

        alc.Reset();
        ptr = IntPtr.Zero;

        success = NativeLibrary.TryLoad(FakeNativeLibrary.RedirectName, asm, null, out ptr);
        Assert.True(success, $"NativeLibrary.TryLoad should have succeeded");
        alc.Validate(FakeNativeLibrary.RedirectName, FakeNativeLibrary.Name);
        Assert.Equal(FakeNativeLibrary.Handle, ptr);

        alc.Reset();

        Console.WriteLine(" -- Validate p/invoke...");
        int addend1 = rand.Next(int.MaxValue / 2);
        int addend2 = rand.Next(int.MaxValue / 2);
        int expected = addend1 + addend2;

        int value = NativeSumInAssemblyLoadContext(alc, addend1, addend2);
        alc.Validate(NativeLibraryToLoad.InvalidName);
        Assert.Equal(expected, value);
    }

    public static void ValidateResolvingUnmanagedDllEvent()
    {
        Console.WriteLine($"Running {nameof(ValidateResolvingUnmanagedDllEvent)}...");

        Console.WriteLine(" -- Validate explicit load: custom ALC...");
        AssemblyLoadContext alcExplicitLoad = new AssemblyLoadContext(nameof(ValidateResolvingUnmanagedDllEvent));
        var asm = alcExplicitLoad.LoadFromAssemblyPath(Assembly.GetExecutingAssembly().Location);
        ValidateResolvingUnmanagedDllEvent_ExplicitLoad(asm);

        Console.WriteLine(" -- Validate explicit load: default ALC...");
        ValidateResolvingUnmanagedDllEvent_ExplicitLoad(Assembly.GetExecutingAssembly());

        Console.WriteLine(" -- Validate p/invoke: custom ALC...");
        AssemblyLoadContext alcPInvoke = new AssemblyLoadContext(nameof(ValidateResolvingUnmanagedDllEvent));
        ValidateResolvingUnmanagedDllEvent_PInvoke(alcPInvoke);

        Console.WriteLine(" -- Validate p/invoke: default ALC...");
        ValidateResolvingUnmanagedDllEvent_PInvoke(AssemblyLoadContext.Default);
    }

    private static void ValidateResolvingUnmanagedDllEvent_ExplicitLoad(Assembly assembly)
    {
        AssemblyLoadContext alc = AssemblyLoadContext.GetLoadContext(assembly);
        using (var handler = new Handlers(alc, returnValid: false))
        {
            Assert.Throws<DllNotFoundException>(() => NativeLibrary.Load(FakeNativeLibrary.Name, assembly, null));
            Assert.True(handler.EventHandlerInvoked);
        }

        using (var handler = new Handlers(alc, returnValid: true))
        {
            IntPtr ptr = NativeLibrary.Load(FakeNativeLibrary.Name, assembly, null);
            Assert.True(handler.EventHandlerInvoked);
            Assert.Equal(FakeNativeLibrary.Handle, ptr);
        }
    }

    private static void ValidateResolvingUnmanagedDllEvent_PInvoke(AssemblyLoadContext alc)
    {
        int addend1 = rand.Next(int.MaxValue / 2);
        int addend2 = rand.Next(int.MaxValue / 2);
        int expected = addend1 + addend2;

        using (var handler = new Handlers(alc, returnValid: false))
        {
            if (alc == AssemblyLoadContext.Default)
            {
                Assert.Throws<DllNotFoundException>(() => NativeSum(addend1, addend2));
            }
            else
            {
                TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() => NativeSumInAssemblyLoadContext(alc, addend1, addend2));
                Assert.Equal(typeof(DllNotFoundException), ex.InnerException.GetType());
            }

            Assert.True(handler.EventHandlerInvoked);
        }

        // Multiple handlers - first valid result is used
        // Test using valid handlers is done last, as the result will be cached for the ALC
        using (var handlerInvalid = new Handlers(alc, returnValid: false))
        using (var handlerValid1 = new Handlers(alc, returnValid: true))
        using (var handlerValid2 = new Handlers(alc, returnValid: true))
        {
            int value = alc == AssemblyLoadContext.Default
                ? NativeSum(addend1, addend2)
                : NativeSumInAssemblyLoadContext(alc, addend1, addend2);

            Assert.True(handlerInvalid.EventHandlerInvoked);
            Assert.True(handlerValid1.EventHandlerInvoked);
            Assert.False(handlerValid2.EventHandlerInvoked);
            Assert.Equal(expected, value);
        }
    }

    private static int NativeSumInAssemblyLoadContext(AssemblyLoadContext alc, int addend1, int addend2)
    {
        string currentDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var assembly = alc.LoadFromAssemblyPath(Path.Combine(currentDir, "TestAsm.dll"));
        var type = assembly.GetType("TestAsm");
        var method = type.GetMethod("Sum");

        int value = (int)method.Invoke(null, new object[] { addend1, addend2 });
        return value;
    }

    [DllImport(NativeLibraryToLoad.InvalidName)]
    static extern int NativeSum(int arg1, int arg2);

    private class Handlers : IDisposable
    {
        private AssemblyLoadContext alc;
        private bool returnValid;

        public bool EventHandlerInvoked { get; private set; }

        public Handlers(AssemblyLoadContext alc, bool returnValid)
        {
            this.alc = alc;
            this.returnValid = returnValid;
            this.EventHandlerInvoked = false;
            this.alc.ResolvingUnmanagedDll += OnResolvingUnmanagedDll;
        }

        public void Dispose()
        {
            this.alc.ResolvingUnmanagedDll -= OnResolvingUnmanagedDll;
        }

        private IntPtr OnResolvingUnmanagedDll(Assembly assembly, string libraryName)
        {
            EventHandlerInvoked = true;

            if (!this.returnValid)
                return IntPtr.Zero;

            if (string.Equals(libraryName, NativeLibraryToLoad.InvalidName))
                return NativeLibrary.Load(NativeLibraryToLoad.Name, assembly, null);

            if (string.Equals(libraryName, FakeNativeLibrary.Name))
                return FakeNativeLibrary.Handle;

            return IntPtr.Zero;
        }
    }
}
