// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader;

internal sealed unsafe class Target
{
    private readonly delegate* unmanaged<ulong, byte*, uint, void*, int> _readFromTarget;
    private readonly void* _readContext;

    private bool _isLittleEndian;
    private int _pointerSize;

    public Target(ulong _, delegate* unmanaged<ulong, byte*, uint, void*, int> readFromTarget, void* readContext)
    {
        _readFromTarget = readFromTarget;
        _readContext = readContext;

        // TODO: [cdac] Populate from descriptor
        _isLittleEndian = BitConverter.IsLittleEndian;
        _pointerSize = IntPtr.Size;
    }
}
