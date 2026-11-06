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

    public static readonly IntPtr Forwarders = Shim_GetForwarders();

    [StructLayout(LayoutKind.Sequential)]
    public struct JitCompilerCallbacks
    {
        public delegate* unmanaged<IntPtr, IntPtr*, IntPtr, CORINFO_METHOD_INFO*, uint, byte**, uint*, CorJitResult> compileMethod;
        public delegate* unmanaged<IntPtr, IntPtr, void> ProcessShutdownWork;
        public delegate* unmanaged<IntPtr, Guid*, void> getVersionIdentifier;
        public delegate* unmanaged<IntPtr, CORINFO_OS, void> setTargetOS;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct JitHostCallbacks
    {
        public delegate* unmanaged<IntPtr, byte*, int, int> getIntConfigValue;
        public delegate* unmanaged<IntPtr, byte*, byte*> getStringConfigValue;
        public delegate* unmanaged<IntPtr, byte*, void> freeStringConfigValue;
    }

    [LibraryImport(Library)]
    private static partial IntPtr Shim_GetForwarders();

    [LibraryImport(Library)]
    public static partial void Shim_SetUnloadCallback(delegate* unmanaged<void> callback);

    [LibraryImport(Library)]
    public static partial IntPtr Shim_AllocError(uint code);

    [LibraryImport(Library)]
    public static partial uint Shim_GetExceptionCode(IntPtr exception);

    [LibraryImport(Library)]
    public static partial void Shim_FreeException(IntPtr exception);

    [LibraryImport(Library)]
    public static partial IntPtr Shim_CreateJitCompiler(IntPtr thisHandle, JitCompilerCallbacks* callbacks);

    [LibraryImport(Library)]
    public static partial IntPtr Shim_CreateJitHost(IntPtr original, IntPtr thisHandle, JitHostCallbacks* callbacks);

    [LibraryImport(Library)]
    public static partial CorJitResult Shim_CompileMethod(IntPtr* exception, IntPtr jit, IntPtr comp, CORINFO_METHOD_INFO* info,
        uint flags, byte** nativeEntry, uint* nativeSizeOfCode);

    [LibraryImport(Library)]
    public static partial CorJitResult Shim_CompileMethodWithJitInfo(IntPtr* exception, IntPtr jit, IntPtr thisHandle, IntPtr callbacks,
        CORINFO_METHOD_INFO* info, uint flags, byte** nativeEntry, uint* nativeSizeOfCode);

    [LibraryImport(Library)]
    public static partial void Shim_ProcessShutdownWork(IntPtr jit, IntPtr info);

    [LibraryImport(Library)]
    public static partial void Shim_GetVersionIdentifier(IntPtr jit, Guid* versionIdentifier);

    [LibraryImport(Library)]
    public static partial void Shim_SetTargetOS(IntPtr jit, CORINFO_OS os);

    [LibraryImport(Library)]
    public static partial byte Shim_RunWithSPMIErrorTrap(IntPtr* exception, void* function, void* parameter, uint errorCode);

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

    // Compiles the method with the real JIT, rethrowing what escapes it.
    public static CorJitResult CompileMethod(IntPtr jit, IntPtr comp, CORINFO_METHOD_INFO* info, uint flags, byte** nativeEntry, uint* nativeSizeOfCode)
    {
        IntPtr exception = 0;
        CorJitResult result = Shim_CompileMethod(&exception, jit, comp, info, flags, nativeEntry, nativeSizeOfCode);
        if (exception != 0)
            throw new EEException(exception);
        return result;
    }

    // The native exception to rethrow through the JIT (and to the EE) for an exception of the managed shim.
    public static IntPtr ToNativeException(Exception ex) => ex switch
    {
        EEException e => e.NativeException,
        SpmiException e => Shim_AllocError(e.Code),
        _ => Fail(ex),
    };

    // An unexpected failure of the shim, which ends the process as an unhandled exception would.
    [DoesNotReturn]
    private static IntPtr Fail(Exception ex)
    {
        Logger.Flush();
        Environment.FailFast($"SuperPMI shim failure: {ex}");
        return 0;
    }
}

// An exception that the EE (or the JIT) threw, captured by the native shim to be rethrown where the call returns to
// the EE; or an error of the shim, such as a method context error, that escaped the JIT.
internal sealed class EEException : Exception
{
    public EEException(IntPtr nativeException) => NativeException = nativeException;

    public IntPtr NativeException { get; }

    // The exception code that the method contexts record.
    public uint Code => ShimInterop.Shim_GetExceptionCode(NativeException);
}
