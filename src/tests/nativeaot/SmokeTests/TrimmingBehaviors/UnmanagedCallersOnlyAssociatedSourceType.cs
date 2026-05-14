// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class UnmanagedCallersOnlyAssociatedSourceType
{
    private const string UnconditionalExport = "UnmanagedCallersOnlyAssociatedSourceType_Unconditional";
    private const string UsedSourceExport = "UnmanagedCallersOnlyAssociatedSourceType_UsedSource";
    private const string UnusedSourceExport = "UnmanagedCallersOnlyAssociatedSourceType_UnusedSource";
    private const string DynamicSourceExport = "UnmanagedCallersOnlyAssociatedSourceType_DynamicSource";

    public static unsafe int Run()
    {
        GC.KeepAlive(new UsedSource());
        typeof(UnmanagedCallersOnlyAssociatedSourceType).GetMethod(nameof(CreateDynamicSource))!.MakeGenericMethod([GetDynamicAtom()]).Invoke(null, []);

        IntPtr programHandle = NativeLibrary.GetMainProgramHandle();

        AssertExportReturns(programHandle, UnconditionalExport, 1);
        AssertExportReturns(programHandle, UsedSourceExport, 2);
        AssertExportReturns(programHandle, DynamicSourceExport, 4);
        AssertNoExport(programHandle, UnusedSourceExport);

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object CreateDynamicSource<T>() => new DynamicSource<T>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Type GetDynamicAtom() => typeof(DynamicAtom);

    private static unsafe void AssertExportReturns(IntPtr programHandle, string exportName, int expected)
    {
        IntPtr methodAddress = NativeLibrary.GetExport(programHandle, exportName);
        if (methodAddress == IntPtr.Zero)
            throw new Exception($"Expected export '{exportName}'.");

        var method = (delegate* unmanaged<int>)methodAddress;
        int actual = method();
        if (actual != expected)
            throw new Exception($"Export '{exportName}' returned {actual}; expected {expected}.");
    }

    private static void AssertNoExport(IntPtr programHandle, string exportName)
    {
        try
        {
            NativeLibrary.GetExport(programHandle, exportName);
        }
        catch (EntryPointNotFoundException)
        {
            return;
        }

        throw new Exception($"Unexpected export '{exportName}'.");
    }

    [UnmanagedCallersOnly(EntryPoint = UnconditionalExport)]
    private static int UnconditionalEntryPoint() => 1;

    [UnmanagedCallersOnly(EntryPoint = UsedSourceExport, AssociatedSourceType = typeof(UsedSource))]
    private static int UsedSourceEntryPoint() => 2;

    [UnmanagedCallersOnly(EntryPoint = UnusedSourceExport, AssociatedSourceType = typeof(UnusedSource))]
    private static int UnusedSourceEntryPoint() => 3;

    [UnmanagedCallersOnly(EntryPoint = DynamicSourceExport, AssociatedSourceType = typeof(DynamicSource<DynamicAtom>))]
    private static int DynamicSourceEntryPoint() => 4;

    private sealed class UsedSource;

    private sealed class UnusedSource;

    public sealed class DynamicSource<T>;

    public sealed class DynamicAtom;
}
