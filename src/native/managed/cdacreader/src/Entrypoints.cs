// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader;

internal static class Entrypoints
{
    private const string CDAC = "cdac_reader_";

    [UnmanagedCallersOnly(EntryPoint = $"{CDAC}init")]
    private static unsafe int Init(ulong descriptor, delegate* unmanaged<ulong, byte*, uint, void*, int> readFromTarget, void* readContext, IntPtr* handle)
    {
        // TODO: [cdac] Better error code/details
        if (!Target.TryCreate(descriptor, (address, buffer, bufferLength) => readFromTarget(address, buffer, bufferLength, readContext), out Target? target))
            return -1;

        GCHandle gcHandle = GCHandle.Alloc(target);
        *handle = GCHandle.ToIntPtr(gcHandle);
        return 0;
    }

    [UnmanagedCallersOnly(EntryPoint = $"{CDAC}free")]
    private static unsafe int Free(IntPtr handle)
    {
        GCHandle h = GCHandle.FromIntPtr(handle);
        h.Free();
        return 0;
    }

    /// <summary>
    /// Get the SOS-DAC interface implementation.
    /// </summary>
    /// <param name="handle">Handle crated via cdac initialization</param>
    /// <param name="obj"><c>IUnknown</c> pointer that can be queried for ISOSDacInterface*</param>
    /// <returns></returns>
    [UnmanagedCallersOnly(EntryPoint = $"{CDAC}get_sos_interface")]
    private static unsafe int GetSOSInterface(IntPtr handle, nint* obj)
    {
        ComWrappers cw = new StrategyBasedComWrappers();
        Target? target = GCHandle.FromIntPtr(handle).Target as Target;
        if (target == null)
            return -1;

        Legacy.SOSDacImpl impl = new(target);
        nint ptr = cw.GetOrCreateComInterfaceForObject(impl, CreateComInterfaceFlags.None);
        *obj = ptr;
        return 0;
    }
}
