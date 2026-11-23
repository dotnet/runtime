// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Internal.JitInterface;

namespace SuperPmi.Shim;

// The native side of the shims (superpmi/jitinterface/shim.cpp), statically linked into each of them.
internal static unsafe partial class ShimInterop
{
    private const string Library = "superpmi-shim-jitinterface";

    // The callbacks of the collector's RecordingJitCompiler.
    [StructLayout(LayoutKind.Sequential)]
    public struct RecordingCompilerCallbacks
    {
        public delegate* unmanaged<IntPtr, IntPtr, CORINFO_METHOD_INFO*, uint, IntPtr*, IntPtr> beginCompile;
        public delegate* unmanaged<IntPtr, IntPtr, CorJitResult, byte*, uint, void> endCompile;
        public delegate* unmanaged<IntPtr, CORINFO_OS, void> setTargetOS;
        public delegate* unmanaged<IntPtr, IntPtr*, CORINFO_CONST_LOOKUP*, CORINFO_CONST_LOOKUP*, uint, void> recordUpdateEntryPointForTailCall;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct JitHostCallbacks
    {
        public delegate* unmanaged<IntPtr, byte*, int, int> getIntConfigValue;
        public delegate* unmanaged<IntPtr, byte*, byte*> getStringConfigValue;
        public delegate* unmanaged<IntPtr, byte*, void> freeStringConfigValue;
    }

    // superpmi-shim-counter: counts the calls of the forwarding JIT and JIT host, and writes the counts to 'fileName'
    // when the shim is unloaded.
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    public static partial void Shim_EnableCallCounting(string fileName);

    [LibraryImport(Library)]
    public static partial IntPtr Shim_AllocError(uint code);

    [LibraryImport(Library)]
    public static partial void Shim_FreeException(IntPtr exception);

    // The JIT and the JIT host of the forwarding shims, which count the calls if Shim_EnableCallCounting was called.
    [LibraryImport(Library)]
    public static partial IntPtr Shim_CreateForwardingJitCompiler(IntPtr jit);

    [LibraryImport(Library)]
    public static partial IntPtr Shim_CreateForwardingJitHost(IntPtr original);

    [LibraryImport(Library)]
    public static partial IntPtr Shim_CreateRecordingJitCompiler(IntPtr jit, IntPtr thisHandle, RecordingCompilerCallbacks* callbacks);

    [LibraryImport(Library)]
    public static partial IntPtr Shim_CreateJitHost(IntPtr original, IntPtr thisHandle, JitHostCallbacks* callbacks);

    // The name of the method and of its class, for the collection filter, without recording them; null if the EE throws.
    [LibraryImport(Library)]
    public static partial byte* Shim_GetMethodNameFromMetadata(IntPtr comp, CORINFO_METHOD_STRUCT_* ftn, byte** className);

    // The length of the GC layout of the class, in pointers of the target, without recording the queries for it.
    [LibraryImport(Library)]
    public static partial uint Shim_GetClassGCLayoutLength(IntPtr comp, CORINFO_CLASS_STRUCT_* cls);

    [LibraryImport(Library)]
    public static partial int Shim_HostGetIntConfigValue(IntPtr host, byte* name, int defaultValue);

    [LibraryImport(Library)]
    public static partial byte* Shim_HostGetStringConfigValue(IntPtr host, byte* name);

    [LibraryImport(Library)]
    public static partial void Shim_HostFreeStringConfigValue(IntPtr host, byte* value);

    // Allocated once, for the lifetime of the process.
    public static T* Allocate<T>(T value) where T : unmanaged
    {
        var p = (T*)NativeMemory.Alloc((nuint)sizeof(T));
        *p = value;
        return p;
    }

    // The native exception to rethrow through the JIT for an error of the managed shim.
    public static IntPtr ToNativeException(Exception ex) => ex switch
    {
        SpmiException e => Shim_AllocError(e.Code),
        _ => Fail(ex),
    };

    // An unexpected failure of the shim, which ends the process as an unhandled exception would.
    [DoesNotReturn]
    public static IntPtr Fail(Exception ex)
    {
        Logger.Flush();
        Environment.FailFast($"SuperPMI shim failure: {ex}");
        return 0;
    }
}
