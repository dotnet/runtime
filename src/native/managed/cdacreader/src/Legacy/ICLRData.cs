// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

// This file contains managed declarations for the IXCLRData interfaces.
// See src/coreclr/inc/clrdata.idl

[GeneratedComInterface]
[Guid("471c35b4-7c2f-4ef0-a945-00f8c38056f1")]
internal unsafe partial interface ICLRDataEnumMemoryRegions
{
    [PreserveSig]
    int EnumMemoryRegions(/*ICLRDataEnumMemoryRegionsCallback*/ void* callback, uint miniDumpFlags, /*CLRDataEnumMemoryFlags*/ int clrFlags);
}
