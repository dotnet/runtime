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
    private const string UnusedArraySourceExport = "UnmanagedCallersOnlyAssociatedSourceType_UnusedArraySource";
    private const string DynamicSourceExport = "UnmanagedCallersOnlyAssociatedSourceType_DynamicSource";
    private const string DynamicArraySourceExport = "UnmanagedCallersOnlyAssociatedSourceType_DynamicArraySource";
    private const string UsedAsElementOnlyArrayExport = "UnmanagedCallersOnlyAssociatedSourceType_UsedAsElementOnlyArray";
    private const string UsedAsElementOnly2ArrayExport = "UnmanagedCallersOnlyAssociatedSourceType_UsedAsElementOnly2Array";
    private const string StructUsedAsElementOnlyArrayExport = "UnmanagedCallersOnlyAssociatedSourceType_StructUsedAsElementOnlyArray";

    public static unsafe int Run()
    {
        // ActiveIssue: https://github.com/dotnet/runtime/issues/129366
        if (OperatingSystem.IsAndroid())
            return 100;

        GC.KeepAlive(new UsedSource());
        typeof(UnmanagedCallersOnlyAssociatedSourceType).GetMethod(nameof(CreateDynamicSource))!.MakeGenericMethod([GetDynamicAtom()]).Invoke(null, []);
        typeof(UnmanagedCallersOnlyAssociatedSourceType).GetMethod(nameof(CreateDynamicSourceArray))!.MakeGenericMethod([GetDynamicAtom()]).Invoke(null, []);
        typeof(UnmanagedCallersOnlyAssociatedSourceType).GetMethod(nameof(CreateArray))!.MakeGenericMethod([typeof(UsedAsElementOnly)]).Invoke(null, []);
        typeof(UnmanagedCallersOnlyAssociatedSourceType).GetMethod(nameof(CreateTripleArray))!.MakeGenericMethod([typeof(UsedAsElementOnly2)]).Invoke(null, []);
        GC.KeepAlive(typeof(StructUsedAsElementOnly));

        IntPtr programHandle = NativeLibrary.GetMainProgramHandle();

        AssertExportReturns(programHandle, UnconditionalExport, 1);
        AssertExportReturns(programHandle, UsedSourceExport, 2);
        AssertExportReturns(programHandle, DynamicSourceExport, 4);
        AssertExportReturns(programHandle, DynamicArraySourceExport, 5);
        AssertExportReturns(programHandle, UsedAsElementOnlyArrayExport, 7);
        AssertExportReturns(programHandle, UsedAsElementOnly2ArrayExport, 8);
        AssertNoExport(programHandle, UnusedSourceExport);
        AssertNoExport(programHandle, UnusedArraySourceExport);
        AssertNoExport(programHandle, StructUsedAsElementOnlyArrayExport);

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object CreateDynamicSource<T>() where T : class => new DynamicSource<T>();

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object CreateDynamicSourceArray<T>() where T : class => new DynamicSource<T>[1, 1];

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object CreateArray<T>() where T : class => new T[0];

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object CreateTripleArray<T>() where T : class => new T[0][][];

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

    [UnmanagedCallersOnly(EntryPoint = UnusedSourceExport, AssociatedSourceType = typeof(UnusedNonArraySource))]
    private static int UnusedSourceEntryPoint() => 3;

    [UnmanagedCallersOnly(EntryPoint = UnusedArraySourceExport, AssociatedSourceType = typeof(UnusedSource[]))]
    private static int UnusedArraySourceEntryPoint() => 6;

    [UnmanagedCallersOnly(EntryPoint = DynamicSourceExport, AssociatedSourceType = typeof(DynamicSource<DynamicAtom>))]
    private static int DynamicSourceEntryPoint() => 4;

    [UnmanagedCallersOnly(EntryPoint = DynamicArraySourceExport, AssociatedSourceType = typeof(DynamicSource<DynamicAtom>[,]))]
    private static int DynamicArraySourceEntryPoint() => 5;

    [UnmanagedCallersOnly(EntryPoint = UsedAsElementOnlyArrayExport, AssociatedSourceType = typeof(UsedAsElementOnly[]))]
    private static int UsedAsElementOnlyArrayEntryPoint() => 7;

    [UnmanagedCallersOnly(EntryPoint = UsedAsElementOnly2ArrayExport, AssociatedSourceType = typeof(UsedAsElementOnly2[][][]))]
    private static int UsedAsElementOnly2ArrayEntryPoint() => 8;

    [UnmanagedCallersOnly(EntryPoint = StructUsedAsElementOnlyArrayExport, AssociatedSourceType = typeof(StructUsedAsElementOnly[]))]
    private static int StructUsedAsElementOnlyArrayEntryPoint() => 9;

    private sealed class UsedSource;

    private sealed class UnusedNonArraySource;

    private sealed class UnusedSource;

    private sealed class UsedAsElementOnly;

    private sealed class UsedAsElementOnly2;

    private struct StructUsedAsElementOnly;

    public sealed class DynamicSource<T>;

    public sealed class DynamicAtom;
}
