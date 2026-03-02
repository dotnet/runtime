// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[StructLayout(LayoutKind.Sequential)]
public struct DbiVersion
{
    public uint m_dwFormat;
    public uint m_dwDbiVersion;
    public uint m_dwProtocolBreakingChangeCounter;
    public uint m_dwReservedMustBeZero1;
}

// Prefix projection of IDacDbiInterface used to validate COM binding in managed cDAC.
// The full interface surface is intentionally staged in follow-up changes.
[ComImport]
[Guid("B7A6D3F5-6B46-4DD4-8AF1-0D4A2AFB98C1")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public unsafe interface IDacDbiInterfaceControl
{
    [PreserveSig]
    int CheckDbiVersion(DbiVersion* pVersion);

    [PreserveSig]
    int FlushCache();

    [PreserveSig]
    int DacSetTargetConsistencyChecks([MarshalAs(UnmanagedType.Bool)] bool fEnableAsserts);

    [PreserveSig]
    int Destroy();
}
