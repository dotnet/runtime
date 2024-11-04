// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
internal sealed unsafe partial class ClrDataStackWalk : IXCLRDataStackWalk
{
    private readonly TargetPointer _threadAddr;
    private readonly uint _flags;
    private readonly Target _target;
    private readonly IXCLRDataStackWalk? _legacyImpl;

    public ClrDataStackWalk(TargetPointer threadAddr, uint flags, Target target, IXCLRDataStackWalk? legacyImpl)
    {
        _threadAddr = threadAddr;
        _flags = flags;
        _target = target;
        _legacyImpl = legacyImpl;
    }

    int IXCLRDataStackWalk.GetContext(uint contextFlags, uint contextBufSize, uint* contextSize, [MarshalUsing(CountElementName = "contextBufSize"), Out] byte[] contextBuf)
        => _legacyImpl is not null ? _legacyImpl.GetContext(contextFlags, contextBufSize, contextSize, contextBuf) : HResults.E_NOTIMPL;
    int IXCLRDataStackWalk.GetFrame(void** frame)
        => _legacyImpl is not null ? _legacyImpl.GetFrame(frame) : HResults.E_NOTIMPL;
    int IXCLRDataStackWalk.GetFrameType(uint* simpleType, uint* detailedType)
        => _legacyImpl is not null ? _legacyImpl.GetFrameType(simpleType, detailedType) : HResults.E_NOTIMPL;
    int IXCLRDataStackWalk.GetStackSizeSkipped(ulong* stackSizeSkipped)
        => _legacyImpl is not null ? _legacyImpl.GetStackSizeSkipped(stackSizeSkipped) : HResults.E_NOTIMPL;
    int IXCLRDataStackWalk.Next()
        => _legacyImpl is not null ? _legacyImpl.Next() : HResults.E_NOTIMPL;
    int IXCLRDataStackWalk.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
        => _legacyImpl is not null ? _legacyImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;
    int IXCLRDataStackWalk.SetContext(uint contextSize, [In, MarshalUsing(CountElementName = "contextSize")] byte[] context)
        => _legacyImpl is not null ? _legacyImpl.SetContext(contextSize, context) : HResults.E_NOTIMPL;
    int IXCLRDataStackWalk.SetContext2(uint flags, uint contextSize, [In, MarshalUsing(CountElementName = "contextSize")] byte[] context)
        => _legacyImpl is not null ? _legacyImpl.SetContext2(flags, contextSize, context) : HResults.E_NOTIMPL;
}
