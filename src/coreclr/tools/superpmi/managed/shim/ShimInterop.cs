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

    // The callbacks of the collector's RecordingJitCompiler.
    [StructLayout(LayoutKind.Sequential)]
    public struct RecordingCompilerCallbacks
    {
        public delegate* unmanaged<IntPtr, IntPtr, PendingCall*, CORINFO_METHOD_INFO*, uint, IntPtr*, IntPtr> beginCompile;
        public delegate* unmanaged<IntPtr, IntPtr, CorJitResult, byte*, uint, void> endCompile;
        public delegate* unmanaged<IntPtr, CORINFO_OS, void> setTargetOS;
        public delegate* unmanaged<int, void> pendingCallNotTaken;
        public delegate* unmanaged<IntPtr, IntPtr*, CORINFO_CONST_LOOKUP*, CORINFO_CONST_LOOKUP*, void> recordUpdateEntryPointForTailCall;
    }

    // The call that RecordingJitInfo made for the JIT, whose result OriginalJitInfo returns to the collector.
    //
    // The collector's method for the call must take it, once, before it calls any other method of OriginalJitInfo:
    // otherwise, what it records isn't what the JIT got. RecordingJitInfo checks that the call was taken.
    [StructLayout(LayoutKind.Sequential)]
    public struct PendingCall
    {
        // The index of the method, or -1 when there is no pending call.
        public int Index;
        public int Taken;
        public uint ExceptionCode;
        public void* Result;

        // No pending call, for an OriginalJitInfo without one; allocated once, for the lifetime of the process.
        public static readonly PendingCall* None = Allocate(new PendingCall { Index = -1 });

        // Returns whether the call to the method of OriginalJitInfo is the pending call, whose result it returns;
        // throws if the pending call threw.
        public bool TryTake(int index)
        {
            if (!Take(index))
                return false;
            if (ExceptionCode != 0)
                throw new EEException(ExceptionCode);
            return true;
        }

        // Returns whether the call to the method of OriginalJitInfo is the pending call, whose result it returns.
        public bool Take(int index)
        {
            if (Index < 0)
                return false;

            if (Taken == 0)
            {
                if (index != Index)
                    throw new InvalidOperationException($"The collector called {OriginalJitInfo.GetMethodName(index)} before it took the pending call to {OriginalJitInfo.GetMethodName(Index)}");
                Taken = 1;
                return true;
            }

            if (index == Index)
                throw new InvalidOperationException($"The collector called {OriginalJitInfo.GetMethodName(index)} again after it took the pending call");
            return false;
        }
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

    // The JIT and the JIT host of the forwarding shims, which call 'onCall' (if not null) with the name of each method
    // that the runtime or the JIT calls.
    [LibraryImport(Library)]
    public static partial IntPtr Shim_CreateForwardingJitCompiler(IntPtr jit, delegate* unmanaged<byte*, void> onCall);

    [LibraryImport(Library)]
    public static partial IntPtr Shim_CreateForwardingJitHost(IntPtr original, delegate* unmanaged<byte*, void> onCall);

    [LibraryImport(Library)]
    public static partial IntPtr Shim_CreateRecordingJitCompiler(IntPtr jit, IntPtr thisHandle, RecordingCompilerCallbacks* callbacks);

    [LibraryImport(Library)]
    public static partial IntPtr Shim_CreateJitHost(IntPtr original, IntPtr thisHandle, JitHostCallbacks* callbacks);

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

    // The native exception to rethrow through the JIT (and to the EE) for an exception of the managed shim; none for
    // the exception of a pending call, which already propagates natively.
    public static IntPtr ToNativeException(Exception ex) => ex switch
    {
        EEException e => e.NativeException,
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

// An exception that the EE threw for a call of the managed shim, captured by the native shim; or an error of the
// shim, such as a method context error. Without a native exception, the exception of a pending call (that
// RecordingJitInfo made), which propagates to the JIT natively.
internal sealed class EEException : Exception
{
    private readonly uint _code;

    public EEException(IntPtr nativeException) => NativeException = nativeException;

    public EEException(uint code) => _code = code;

    public IntPtr NativeException { get; }

    // The exception code that the method contexts record.
    public uint Code => NativeException != 0 ? ShimInterop.Shim_GetExceptionCode(NativeException) : _code;
}
