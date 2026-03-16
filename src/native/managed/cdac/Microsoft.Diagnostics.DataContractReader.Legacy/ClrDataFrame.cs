// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
public sealed unsafe partial class ClrDataFrame : IXCLRDataFrame, IXCLRDataFrame2
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
    {
        int hr = HResults.S_OK;
        try
        {
            IStackWalk stackWalk = _target.Contracts.StackWalk;
            byte[] context = stackWalk.GetRawContext(_dataFrame);

            if (contextSize is not null)
                *contextSize = (uint)context.Length;

            // Match native DAC behavior: fail when the buffer is too small,
            // and on success copy the full context.
            if (contextBufSize < (uint)context.Length)
                throw new ArgumentException();

            if (contextBufSize > 0 && context.Length > 0)
                Array.Copy(context, 0, contextBuf, 0, context.Length);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            byte[] localContextBuf = new byte[contextBufSize];
            int hrLocal = _legacyImpl.GetContext(contextFlags, contextBufSize, null, localContextBuf);
            Debug.ValidateHResult(hr, hrLocal);

            if (hr == HResults.S_OK)
            {
                IPlatformAgnosticContext contextStruct = IPlatformAgnosticContext.GetContextForPlatform(_target);
                IPlatformAgnosticContext localContextStruct = IPlatformAgnosticContext.GetContextForPlatform(_target);
                contextStruct.FillFromBuffer(contextBuf);
                localContextStruct.FillFromBuffer(localContextBuf);

                Debug.Assert(contextStruct.Equals(localContextStruct));
            }
        }
#endif

        return hr;
    }

    int IXCLRDataFrame.GetAppDomain(DacComNullableByRef<IXCLRDataAppDomain> appDomain)
    {
        int hr = HResults.S_OK;

        int hrLegacy = HResults.S_OK;
        IXCLRDataAppDomain? legacyAppDomain = null;
        if (_legacyImpl is not null)
        {
            DacComNullableByRef<IXCLRDataAppDomain> legacyAppDomainOut = new(isNullRef: false);
            hrLegacy = _legacyImpl.GetAppDomain(legacyAppDomainOut);
            if (hrLegacy >= 0)
            {
                legacyAppDomain = legacyAppDomainOut.Interface;
            }
        }

        try
        {
            TargetPointer appDomainPointer = _target.ReadGlobalPointer(Constants.Globals.AppDomain);
            TargetPointer appDomainAddr = _target.ReadPointer(appDomainPointer);

            if (appDomainAddr != TargetPointer.Null)
            {
                appDomain.Interface = new ClrDataAppDomain(_target, appDomainAddr, legacyAppDomain);
            }
            else
            {
                hr = HResults.S_FALSE;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            Debug.ValidateHResult(hr, hrLegacy);
        }
#endif

        return hr;
    }

    int IXCLRDataFrame.GetNumArguments(uint* numArgs)
    {
        int hr = HResults.S_OK;
        try
        {
            *numArgs = 0;
            IStackWalk stackWalk = _target.Contracts.StackWalk;
            TargetPointer methodDesc = stackWalk.GetMethodDescPtr(_dataFrame);
            if (methodDesc == TargetPointer.Null)
                throw Marshal.GetExceptionForHR(/*E_NOINTERFACE*/ HResults.COR_E_INVALIDCAST)!;

            // get module and token
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            MethodDescHandle mdh = rts.GetMethodDescHandle(methodDesc);
            TargetPointer mtAddr = rts.GetMethodTable(mdh);
            TypeHandle typeHandle = rts.GetTypeHandle(mtAddr);
            TargetPointer modulePtr = rts.GetModule(typeHandle);
            ILoader loader = _target.Contracts.Loader;
            Contracts.ModuleHandle moduleHandle = loader.GetModuleHandleFromModulePtr(modulePtr);
            uint token = rts.GetMethodToken(mdh);

            IEcmaMetadata ecmaMetadataContract = _target.Contracts.EcmaMetadata;
            MethodDefinitionHandle methodDefHandle = MetadataTokens.MethodDefinitionHandle((int)token);
            MetadataReader? mdReader = ecmaMetadataContract.GetMetadata(moduleHandle);
            if (mdReader == null)
                throw new NotImplementedException();

            MethodDefinition methodDefinition = mdReader.GetMethodDefinition(methodDefHandle);
            BlobHandle methodBlob = methodDefinition.Signature;
            BlobReader blobReader = mdReader.GetBlobReader(methodBlob);

            // see ECMA-335 II.23.2.1
            SignatureHeader header = blobReader.ReadSignatureHeader();
            if (header.Kind != SignatureKind.Method)
                throw new BadImageFormatException();
            *numArgs = (uint)blobReader.ReadCompressedInteger() + (header.IsInstance ? 1u : 0u);
            if (*numArgs == 0)
                hr = HResults.S_FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacyImpl is not null)
        {
            uint numArgsLocal;
            int hrLocal = _legacyImpl.GetNumArguments(&numArgsLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*numArgs == numArgsLocal, $"cDAC: {*numArgs}, DAC: {numArgsLocal}");
        }
#endif
        return hr;
    }

    int IXCLRDataFrame.GetArgumentByIndex(
        uint index,
        DacComNullableByRef<IXCLRDataValue> arg,
        uint bufLen,
        uint* nameLen,
        char* name)
        => _legacyImpl is not null ? _legacyImpl.GetArgumentByIndex(index, arg, bufLen, nameLen, name) : HResults.E_NOTIMPL;

    int IXCLRDataFrame.GetNumLocalVariables(uint* numLocals)
    {
        int hr = HResults.S_OK;
        try
        {
            *numLocals = 0;
            IStackWalk stackWalk = _target.Contracts.StackWalk;
            TargetPointer methodDesc = stackWalk.GetMethodDescPtr(_dataFrame);
            if (methodDesc == TargetPointer.Null)
                throw Marshal.GetExceptionForHR(/*E_NOINTERFACE*/ HResults.COR_E_INVALIDCAST)!;

            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            MethodDescHandle mdh = rts.GetMethodDescHandle(methodDesc);
            if (!rts.IsIL(mdh))
                throw Marshal.GetExceptionForHR(HResults.E_FAIL)!;

            // get token and module from method desc
            TargetPointer mtAddr = rts.GetMethodTable(mdh);
            TypeHandle typeHandle = rts.GetTypeHandle(mtAddr);
            TargetPointer modulePtr = rts.GetModule(typeHandle);
            ILoader loader = _target.Contracts.Loader;
            Contracts.ModuleHandle moduleHandle = loader.GetModuleHandleFromModulePtr(modulePtr);
            uint token = rts.GetMethodToken(mdh);

            TargetPointer ilHeader = loader.GetILHeader(moduleHandle, token);
            if (ilHeader == TargetPointer.Null)
                throw Marshal.GetExceptionForHR(HResults.E_FAIL)!;

            if (!HeaderReaderHelpers.TryGetLocalVarSigToken(_target, ilHeader, out int localToken))
                throw Marshal.GetExceptionForHR(HResults.E_FAIL)!;

            IEcmaMetadata ecmaMetadataContract = _target.Contracts.EcmaMetadata;
            MetadataReader? mdReader = ecmaMetadataContract.GetMetadata(moduleHandle);
            if (mdReader == null)
                throw new NotImplementedException();

            StandaloneSignatureHandle localSignatureHandle = MetadataTokens.StandaloneSignatureHandle(localToken);
            BlobHandle localSignatureBlob = mdReader.GetStandaloneSignature(localSignatureHandle).Signature;
            BlobReader blobReader = mdReader.GetBlobReader(localSignatureBlob);

            // see ECMA-335 II.23.2.6
            SignatureHeader header = blobReader.ReadSignatureHeader();
            if (header.Kind != SignatureKind.LocalVariables)
                throw new BadImageFormatException();
            *numLocals = (uint)blobReader.ReadCompressedInteger();
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacyImpl is not null)
        {
            uint numLocalsLocal;
            int hrLocal = _legacyImpl.GetNumLocalVariables(&numLocalsLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*numLocals == numLocalsLocal, $"cDAC: {*numLocals}, DAC: {numLocalsLocal}");
        }
#endif
        return hr;
    }

    int IXCLRDataFrame.GetLocalVariableByIndex(
        uint index,
        DacComNullableByRef<IXCLRDataValue> localVariable,
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

    int IXCLRDataFrame.GetMethodInstance(DacComNullableByRef<IXCLRDataMethodInstance> method)
    {
        int hr = HResults.S_OK;

        int hrLocal = HResults.S_OK;
        IXCLRDataMethodInstance? legacyMethod = null;
        if (_legacyImpl is not null)
        {
            DacComNullableByRef<IXCLRDataMethodInstance> legacyMethodOut = new(isNullRef: false);
            hrLocal = _legacyImpl.GetMethodInstance(legacyMethodOut);
            legacyMethod = legacyMethodOut.Interface;
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

            method.Interface = new ClrDataMethodInstance(_target, mdh, appDomain, legacyMethod);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            Debug.ValidateHResult(hr, hrLocal);
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

    int IXCLRDataFrame.GetTypeArgumentByIndex(uint index, DacComNullableByRef<IXCLRDataTypeInstance> typeArg)
        => _legacyImpl is not null ? _legacyImpl.GetTypeArgumentByIndex(index, typeArg) : HResults.E_NOTIMPL;

    // IXCLRDataFrame2 implementation
    int IXCLRDataFrame2.GetExactGenericArgsToken(DacComNullableByRef<IXCLRDataValue> genericToken)
        => _legacyImpl2 is not null ? _legacyImpl2.GetExactGenericArgsToken(genericToken) : HResults.E_NOTIMPL;
}
