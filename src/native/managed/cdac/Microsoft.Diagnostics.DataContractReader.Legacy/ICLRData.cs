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
public unsafe partial interface ICLRDataEnumMemoryRegions
{
    [PreserveSig]
    int EnumMemoryRegions(/*ICLRDataEnumMemoryRegionsCallback*/ void* callback, uint miniDumpFlags, /*CLRDataEnumMemoryFlags*/ int clrFlags);
}

[GeneratedComInterface]
[Guid("3e11ccee-d08b-43e5-af01-32717a64da03")]
public unsafe partial interface ICLRDataTarget
{
    [PreserveSig]
    int GetMachineType(uint* machineType);

    [PreserveSig]
    int GetPointerSize(uint* pointerSize);

    [PreserveSig]
    int GetImageBase([MarshalAs(UnmanagedType.LPWStr)] string imagePath, ulong* baseAddress);

    [PreserveSig]
    int ReadVirtual(ulong address, byte* buffer, uint bytesRequested, uint* bytesRead);

    [PreserveSig]
    int WriteVirtual(ulong address, byte* buffer, uint bytesRequested, uint* bytesWritten);

    [PreserveSig]
    int GetTLSValue(uint threadID, uint index, ulong* value);

    [PreserveSig]
    int SetTLSValue(uint threadID, uint index, ulong value);

    [PreserveSig]
    int GetCurrentThreadID(uint* threadID);

    [PreserveSig]
    int GetThreadContext(uint threadID, uint contextFlags, uint contextSize, byte* context);

    [PreserveSig]
    int SetThreadContext(uint threadID, uint contextSize, byte* context);

    [PreserveSig]
    int Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer);
}

[GeneratedComInterface]
[Guid("6d05fae3-189c-4630-a6dc-1c251e1c01ab")]
public unsafe partial interface ICLRDataTarget2 : ICLRDataTarget
{
    [PreserveSig]
    int AllocVirtual(ClrDataAddress addr, uint size, uint typeFlags, uint protectFlags, ClrDataAddress* virt);
    [PreserveSig]
    int FreeVirtual(ClrDataAddress addr, uint size, uint typeFlags);
}

// Note: ICLRDataTarget3 from clrdata.idl (guid a5664f95...) extends ICLRDataTarget2.
// IXCLRDataTarget3 from xclrdata.idl (guid 59d9b5e1...) also extends ICLRDataTarget2 but is a different interface.
// ICLRDataTarget3 is defined here; IXCLRDataTarget3 is in IXCLRData.cs.
[GeneratedComInterface]
[Guid("a5664f95-0af4-4a1b-960e-2f3346b4214c")]
public unsafe partial interface ICLRDataTarget3 : ICLRDataTarget2
{
    [PreserveSig]
    int GetExceptionRecord(uint bufferSize, uint* bufferUsed, byte* buffer);
    [PreserveSig]
    int GetExceptionContextRecord(uint bufferSize, uint* bufferUsed, byte* buffer);
    [PreserveSig]
    int GetExceptionThreadID(uint* threadID);
}

[GeneratedComInterface]
[Guid("17d5b8c6-34a9-407f-af4f-a930201d4e02")]
public unsafe partial interface ICLRContractLocator
{
    [PreserveSig]
    int GetContractDescriptor(ulong* contractAddress);
}
