// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using SuperPmi.Shim;

namespace Internal.JitInterface;

// The JIT-EE interface of the forwarding shims, whose methods (CorInfoImpl_forwarding_generated.cs) call the EE's.
internal sealed unsafe partial class CorInfoImpl
{
    private readonly OriginalJitInfo _original;

    public CorInfoImpl(OriginalJitInfo original) => _original = original;

    private static CorInfoImpl GetThis(IntPtr thisHandle) => Unsafe.Read<CorInfoImpl>((void*)thisHandle);

    private IntPtr AllocException(Exception ex) => ShimInterop.ToNativeException(ex);

    // Compiles the method with the real JIT through this JIT-EE interface, rethrowing what escapes the JIT.
    public CorJitResult CompileMethod(IntPtr jit, CORINFO_METHOD_INFO* info, uint flags, byte** nativeEntry, uint* nativeSizeOfCode)
    {
        CorInfoImpl _this = this;
        IntPtr exception = 0;
        CorJitResult result = ShimInterop.Shim_CompileMethodWithJitInfo(&exception, jit, (IntPtr)Unsafe.AsPointer(ref _this),
            GetUnmanagedCallbacks(), info, flags, nativeEntry, nativeSizeOfCode);
        if (exception != 0)
            throw new EEException(exception);
        return result;
    }
}
