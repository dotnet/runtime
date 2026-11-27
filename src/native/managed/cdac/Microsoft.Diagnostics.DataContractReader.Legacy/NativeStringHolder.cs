// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Creates a native-memory object that mimics the C++ IStringHolder vtable layout.
/// DacDbiImpl.StringHolderAssignCopy reads: objPtr -> vtable -> AssignCopy fn ptr.
/// This helper allocates that exact structure in unmanaged memory so we can test
/// string-returning DacDbiImpl methods directly (without COM marshaling).
/// </summary>
internal sealed class NativeStringHolder : IDisposable
{
    // Layout in native memory:
    //   _objectPtr  -> [vtablePtr]  (nint)
    //   _vtablePtr  -> [fnPtr]      (nint, the AssignCopy function pointer)
    private readonly IntPtr _objectPtr;
    private readonly IntPtr _vtablePtr;
    private readonly GCHandle _delegateHandle;
    private bool _disposed;

    // Delegate matching the native IStringHolder::AssignCopy(this, const WCHAR* psz) signature.
    // Use ThisCall to match the C++ virtual method calling convention (thiscall on x86, no-op on x64/arm64).
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate int AssignCopyDelegate(IntPtr thisPtr, IntPtr psz);

    public string? Value { get; private set; }

    public NativeStringHolder()
    {
        AssignCopyDelegate assignCopy = AssignCopyImpl;
        _delegateHandle = GCHandle.Alloc(assignCopy);
        IntPtr fnPtr = Marshal.GetFunctionPointerForDelegate(assignCopy);

        // Allocate vtable (one slot: AssignCopy)
        _vtablePtr = Marshal.AllocHGlobal(IntPtr.Size);
        Marshal.WriteIntPtr(_vtablePtr, fnPtr);

        // Allocate object (one field: vtable pointer)
        _objectPtr = Marshal.AllocHGlobal(IntPtr.Size);
        Marshal.WriteIntPtr(_objectPtr, _vtablePtr);
    }

    /// <summary>
    /// The native pointer to pass as the nint IStringHolder parameter.
    /// </summary>
    public nint Ptr => _objectPtr;

    private int AssignCopyImpl(IntPtr thisPtr, IntPtr psz)
    {
        Value = Marshal.PtrToStringUni(psz);
        return HResults.S_OK;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Marshal.FreeHGlobal(_objectPtr);
            Marshal.FreeHGlobal(_vtablePtr);
            _delegateHandle.Free();
            _disposed = true;
        }
    }
}
