// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.Loader;
using System.Reflection;
using System.Runtime.InteropServices;

using TestLibrary;

public class ALC : AssemblyLoadContext
{
    public bool LoadUnmanagedDllCalled { get; private set; }

    public void Reset()
    {
        LoadUnmanagedDllCalled = false;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        LoadUnmanagedDllCalled = true;

        if (string.Equals(unmanagedDllName, NativeLibraryToLoad.InvalidName))
            return LoadUnmanagedDllFromPath(NativeLibraryToLoad.GetFullPath());

        if (string.Equals(unmanagedDllName, FakeNativeLibrary.Name))
            return FakeNativeLibrary.Handle;

        return IntPtr.Zero;
    }
}

public class ResolveUnmanagedDllTests
{
    private static readonly int seed = 123;
    private static readonly Random rand = new Random(seed);

    public static int Main()
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
        IntPtr ptr = NativeLibrary.Load(FakeNativeLibrary.Name, asm, null);
        Assert.IsTrue(alc.LoadUnmanagedDllCalled, "AssemblyLoadContext.LoadUnmanagedDll should have been called.");
        Assert.AreEqual(FakeNativeLibrary.Handle, ptr, $"Unexpected return value for {nameof(NativeLibrary.Load)}");

        alc.Reset();
        ptr = IntPtr.Zero;

        bool success = NativeLibrary.TryLoad(FakeNativeLibrary.Name, asm, null, out ptr);
        Assert.IsTrue(success, $"NativeLibrary.TryLoad should have succeeded");
        Assert.IsTrue(alc.LoadUnmanagedDllCalled, "AssemblyLoadContext.LoadUnmanagedDll should have been called.");
        Assert.AreEqual(FakeNativeLibrary.Handle, ptr, $"Unexpected return value for {nameof(NativeLibrary.Load)}");
        alc.Reset();

        Console.WriteLine(" -- Validate p/invoke...");
        int addend1 = rand.Next(int.MaxValue / 2);
        int addend2 = rand.Next(int.MaxValue / 2);
        int expected = addend1 + addend2;

        int value = NativeSumInAssemblyLoadContext(alc, addend1, addend2);
        Assert.IsTrue(alc.LoadUnmanagedDllCalled, "AssemblyLoadContext.LoadUnmanagedDll should have been called.");
        Assert.AreEqual(expected, value, $"Unexpected return value for {nameof(NativeSum)}");
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
            Assert.IsTrue(handler.EventHandlerInvoked, "Event handler should have been invoked");
        }

        using (var handler = new Handlers(alc, returnValid: true))
        {
            IntPtr ptr = NativeLibrary.Load(FakeNativeLibrary.Name, assembly, null);
            Assert.IsTrue(handler.EventHandlerInvoked, "Event handler should have been invoked");
            Assert.AreEqual(FakeNativeLibrary.Handle, ptr, $"Unexpected return value for {nameof(NativeLibrary.Load)}");
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
                Assert.AreEqual(typeof(DllNotFoundException), ex.InnerException.GetType());
            }

            Assert.IsTrue(handler.EventHandlerInvoked, "Event handler should have been invoked");
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

            Assert.IsTrue(handlerInvalid.EventHandlerInvoked, "Event handler should have been invoked");
            Assert.IsTrue(handlerValid1.EventHandlerInvoked, "Event handler should have been invoked");
            Assert.IsFalse(handlerValid2.EventHandlerInvoked, "Event handler should not have been invoked");
            Assert.AreEqual(expected, value, $"Unexpected return value for {nameof(NativeSum)} in {alc}");
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
