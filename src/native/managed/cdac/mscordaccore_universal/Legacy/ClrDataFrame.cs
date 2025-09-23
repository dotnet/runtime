// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
internal sealed unsafe partial class ClrDataFrame : IXCLRDataFrame, IXCLRDataFrame2
{
    private readonly Target _target;
    private readonly IXCLRDataFrame? _legacyImpl;
    private readonly IXCLRDataFrame2? _legacyImpl2;

    private readonly IStackDataFrameHandle _dataFrame;

    public ClrDataFrame(Target target, IStackDataFrameHandle dataFrame, IXCLRDataFrame? legacyImpl)
    {
        _target = target;
        _legacyImpl = legacyImpl;
        _legacyImpl2 = legacyImpl as IXCLRDataFrame2;

        _dataFrame = dataFrame;
    }

    // IXCLRDataFrame implementation
    int IXCLRDataFrame.GetFrameType(uint* simpleType, uint* detailedType)
        => _legacyImpl is not null ? _legacyImpl.GetFrameType(simpleType, detailedType) : HResults.E_NOTIMPL;

    int IXCLRDataFrame.GetContext(
        uint contextFlags,
        uint contextBufSize,
        uint* contextSize,
        [Out, MarshalUsing(CountElementName = nameof(contextBufSize))] byte[] contextBuf)
        => _legacyImpl is not null ? _legacyImpl.GetContext(contextFlags, contextBufSize, contextSize, contextBuf) : HResults.E_NOTIMPL;

    int IXCLRDataFrame.GetAppDomain(void** appDomain)
        => _legacyImpl is not null ? _legacyImpl.GetAppDomain(appDomain) : HResults.E_NOTIMPL;

    int IXCLRDataFrame.GetNumArguments(uint* numArgs)
        => _legacyImpl is not null ? _legacyImpl.GetNumArguments(numArgs) : HResults.E_NOTIMPL;

    int IXCLRDataFrame.GetArgumentByIndex(
        uint index,
        void** arg,
        uint bufLen,
        uint* nameLen,
        char* name)
        => _legacyImpl is not null ? _legacyImpl.GetArgumentByIndex(index, arg, bufLen, nameLen, name) : HResults.E_NOTIMPL;

    int IXCLRDataFrame.GetNumLocalVariables(uint* numLocals)
        => _legacyImpl is not null ? _legacyImpl.GetNumLocalVariables(numLocals) : HResults.E_NOTIMPL;

    int IXCLRDataFrame.GetLocalVariableByIndex(
        uint index,
        void** localVariable,
        uint bufLen,
        uint* nameLen,
        char* name)
        => _legacyImpl is not null ? _legacyImpl.GetLocalVariableByIndex(index, localVariable, bufLen, nameLen, name) : HResults.E_NOTIMPL;

    int IXCLRDataFrame.GetCodeName(
        uint flags,
        uint bufLen,
        uint* nameLen,
        char* nameBuf)
        => _legacyImpl is not null ? _legacyImpl.GetCodeName(flags, bufLen, nameLen, nameBuf) : HResults.E_NOTIMPL;

    int IXCLRDataFrame.GetMethodInstance(out IXCLRDataMethodInstance? method)
    {
        int hr = HResults.S_OK;
        method = null;

        int hrLocal = HResults.S_OK;
        IXCLRDataMethodInstance? legacyMethod = null;
        if (_legacyImpl is not null)
        {
            hrLocal = _legacyImpl.GetMethodInstance(out legacyMethod);
        }

        try
        {
            IStackWalk stackWalk = _target.Contracts.StackWalk;
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;

            TargetPointer methodDesc = stackWalk.GetMethodDescPtr(_dataFrame);

            if (methodDesc == TargetPointer.Null)
                throw Marshal.GetExceptionForHR(/*E_NOINTERFACE*/ HResults.COR_E_INVALIDCAST)!;

            MethodDescHandle mdh = rts.GetMethodDescHandle(methodDesc);
            TargetPointer appDomain = _target.ReadPointer(
                _target.ReadGlobalPointer(Constants.Globals.AppDomain));

            method = new ClrDataMethodInstance(_target, mdh, appDomain, legacyMethod);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
        }
#endif

        return hr;
    }

    int IXCLRDataFrame.Request(
        uint reqCode,
        uint inBufferSize,
        byte* inBuffer,
        uint outBufferSize,
        byte* outBuffer)
        => _legacyImpl is not null ? _legacyImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;

    int IXCLRDataFrame.GetNumTypeArguments(uint* numTypeArgs)
        => _legacyImpl is not null ? _legacyImpl.GetNumTypeArguments(numTypeArgs) : HResults.E_NOTIMPL;

    int IXCLRDataFrame.GetTypeArgumentByIndex(uint index, void** typeArg)
        => _legacyImpl is not null ? _legacyImpl.GetTypeArgumentByIndex(index, typeArg) : HResults.E_NOTIMPL;

    // IXCLRDataFrame2 implementation
    int IXCLRDataFrame2.GetExactGenericArgsToken(void** genericToken)
        => _legacyImpl2 is not null ? _legacyImpl2.GetExactGenericArgsToken(genericToken) : HResults.E_NOTIMPL;
}
