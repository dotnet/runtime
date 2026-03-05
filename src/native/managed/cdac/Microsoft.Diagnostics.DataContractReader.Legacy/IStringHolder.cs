// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[ComImport]
[Guid("1D83B63E-D0C1-4473-9E6D-E53BFB3CF9A3")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IStringHolder
{
    [PreserveSig]
    int AssignCopy([MarshalAs(UnmanagedType.LPWStr)] string psz);
}
