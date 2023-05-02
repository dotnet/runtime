// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;

using Xunit;

[ConditionalClass(typeof(StartupHookTests), nameof(StartupHookTests.IsSupported))]
public unsafe class StartupHookTests
{
    private const string StartupHookKey = "STARTUP_HOOKS";

    private static Type s_startupHookProvider = typeof(object).Assembly.GetType("System.StartupHookProvider", throwOnError: true);

    private static delegate*<void> ProcessStartupHooks = (delegate*<void>)s_startupHookProvider.GetMethod("ProcessStartupHooks", BindingFlags.NonPublic | BindingFlags.Static).MethodHandle.GetFunctionPointer();

    public static bool IsSupported = ((delegate*<bool>)s_startupHookProvider.GetProperty(nameof(IsSupported), BindingFlags.NonPublic | BindingFlags.Static).GetMethod.MethodHandle.GetFunctionPointer())();

    [Fact]
    public static void ValidHookName()
    {
        Console.WriteLine($"Running {nameof(ValidHookName)}...");

        // Basic hook uses the simple name
        Hook hook = Hook.Basic;
        Assert.False(Path.IsPathRooted(hook.Value));
        AppContext.SetData(StartupHookKey, hook.Value);
        hook.CallCount = 0;

        Assert.Equal(0, hook.CallCount);
        ProcessStartupHooks();
        Assert.Equal(1, hook.CallCount);
    }

    [Fact]
    public static void ValidHookPath()
    {
        Console.WriteLine($"Running {nameof(ValidHookPath)}...");

        // Private hook uses a path. It is in a subdirectory and would not be found via default probing.
        Hook hook = Hook.PrivateInitialize;
        Assert.True(Path.IsPathRooted(hook.Value));
        AppContext.SetData(StartupHookKey, hook.Value);
        hook.CallCount = 0;

        Assert.Equal(0, hook.CallCount);
        ProcessStartupHooks();
        Assert.Equal(1, hook.CallCount);
    }

    [Fact]
    public static void MultipleValidHooksAndSeparators()
    {
        Console.WriteLine($"Running {nameof(MultipleValidHooksAndSeparators)}...");

        Hook hook1 = Hook.Basic;
        Hook hook2 = Hook.PrivateInitialize;

        // Set multiple hooks with an empty entry and leading/trailing separators
        AppContext.SetData(StartupHookKey, $"{Path.PathSeparator}{hook1.Value}{Path.PathSeparator}{Path.PathSeparator}{hook2.Value}{Path.PathSeparator}");
        hook1.CallCount = 0;
        hook2.CallCount = 0;

        Assert.Equal(0, hook1.CallCount);
        Assert.Equal(0, hook2.CallCount);
        ProcessStartupHooks();
        Assert.Equal(1, hook1.CallCount);
        Assert.Equal(1, hook2.CallCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public static void MissingAssembly(bool useAssemblyName)
    {
        Console.WriteLine($"Running {nameof(MissingAssembly)}...");

        string hook = useAssemblyName ? "MissingAssembly" : Path.Combine(AppContext.BaseDirectory, "MissingAssembly.dll");
        AppContext.SetData(StartupHookKey, $"{Hook.Basic.Value}{Path.PathSeparator}{hook}");
        Hook.Basic.CallCount = 0;

        var ex = Assert.Throws<ArgumentException>(() => ProcessStartupHooks());
        Assert.Equal($"Startup hook assembly '{hook}' failed to load. See inner exception for details.", ex.Message);
        Assert.IsType<FileNotFoundException>(ex.InnerException);

        // Previous hooks should run before erroring on the missing assembly
        Assert.Equal(1, Hook.Basic.CallCount);
    }

    [Fact]
    public static void InvalidAssembly()
    {
        Console.WriteLine($"Running {nameof(InvalidAssembly)}...");

        string hook = Path.Combine(AppContext.BaseDirectory, "InvalidAssembly.dll");
        try
        {
            File.WriteAllText(hook, string.Empty);
            AppContext.SetData(StartupHookKey, $"{Hook.Basic.Value}{Path.PathSeparator}{hook}");
            Hook.Basic.CallCount = 0;

            var ex = Assert.Throws<ArgumentException>(() => ProcessStartupHooks());
            Assert.Equal($"Startup hook assembly '{hook}' failed to load. See inner exception for details.", ex.Message);
            var innerEx = ex.InnerException;
            Assert.IsType<BadImageFormatException>(ex.InnerException);

            // Previous hooks should run before erroring on the invalid assembly
            Assert.Equal(1, Hook.Basic.CallCount);
        }
        finally
        {
            File.Delete(hook);
        }
    }

    public static System.Collections.Generic.IEnumerable<object[]> InvalidSimpleAssemblyNameData()
    {
        yield return new object[] {$".{Path.DirectorySeparatorChar}Assembly", true };      // Directory separator
        yield return new object[] {$".{Path.AltDirectorySeparatorChar}Assembly", true};    // Alternative directory separator
        yield return new object[] {"Assembly,version=1.0.0.0", true};                      // Comma
        yield return new object[] {"Assembly version", true};                              // Space
        yield return new object[] {"Assembly.DLL", true};                                  // .dll suffix
        yield return new object[] {"Assembly=Name", false};                                // Invalid name
    }

    [Theory]
    [MemberData(nameof(InvalidSimpleAssemblyNameData))]
    public static void InvalidSimpleAssemblyName(string name, bool failsSimpleNameCheck)
    {
        Console.WriteLine($"Running {nameof(InvalidSimpleAssemblyName)}({name}, {failsSimpleNameCheck})...");

        AppContext.SetData(StartupHookKey, $"{Hook.Basic.Value}{Path.PathSeparator}{name}");
        Hook.Basic.CallCount = 0;

        var ex = Assert.Throws<ArgumentException>(() => ProcessStartupHooks());
        Assert.StartsWith($"The startup hook simple assembly name '{name}' is invalid.", ex.Message);
        if (failsSimpleNameCheck)
        {
            Assert.Null(ex.InnerException);
        }
        else
        {
            var innerEx = ex.InnerException;
            Assert.IsType<FileLoadException>(innerEx);
            Assert.Equal($"The given assembly name was invalid.", innerEx.Message);
        }

        // Invalid assembly name should error early such that previous hooks are not run
        Assert.Equal(0, Hook.Basic.CallCount);
    }

    [Fact]
    public static void MissingStartupHookType()
    {
        Console.WriteLine($"Running {nameof(MissingStartupHookType)}...");

        var asm = typeof(StartupHookTests).Assembly;
        string hook = asm.Location;
        AppContext.SetData(StartupHookKey, hook);
        var ex = Assert.Throws<TypeLoadException>(() => ProcessStartupHooks());
        Assert.StartsWith($"Could not load type 'StartupHook' from assembly '{asm.GetName().Name}", ex.Message);
    }

    [Fact]
    public static void MissingInitializeMethod()
    {
        Console.WriteLine($"Running {nameof(MissingInitializeMethod)}...");

        AppContext.SetData(StartupHookKey, Hook.NoInitializeMethod.Value);
        var ex = Assert.Throws<MissingMethodException>(() => ProcessStartupHooks());
        Assert.Equal($"Method 'StartupHook.Initialize' not found.", ex.Message);
    }

    public static System.Collections.Generic.IEnumerable<object[]> IncorrectInitializeSignatureData()
    {
        yield return new[] { Hook.InstanceMethod };
        yield return new[] { Hook.MultipleIncorrectSignatures };
        yield return new[] { Hook.NonVoidReturn };
        yield return new[] { Hook.NotParameterless };
    }

    [Theory]
    [MemberData(nameof(IncorrectInitializeSignatureData))]
    public static void IncorrectInitializeSignature(Hook hook)
    {
        Console.WriteLine($"Running {nameof(IncorrectInitializeSignature)}({hook.Name})...");

        AppContext.SetData(StartupHookKey, hook.Value);
        var ex = Assert.Throws<ArgumentException>(() => ProcessStartupHooks());
        Assert.Equal($"The signature of the startup hook 'StartupHook.Initialize' in assembly '{hook.Value}' was invalid. It must be 'public static void Initialize()'.", ex.Message);
    }
}
