// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
public sealed unsafe partial class ClrDataStackWalk : IXCLRDataStackWalk
{
    private readonly TargetPointer _threadAddr;
    private readonly uint _flags;
    private readonly Target _target;
    private readonly IXCLRDataStackWalk? _legacyImpl;
    private readonly ThreadData _threadData;

    private bool _currentFrameIsValid;
    private IEnumerator<IStackDataFrameHandle> _dataFrames;

    public ClrDataStackWalk(TargetPointer threadAddr, uint flags, Target target, IXCLRDataStackWalk? legacyImpl)
    {
        _threadAddr = threadAddr;
        _flags = flags;
        _target = target;
        _legacyImpl = legacyImpl;

        _threadData = _target.Contracts.Thread.GetThreadData(_threadAddr);
        _dataFrames = _target.Contracts.StackWalk.CreateStackWalk(_threadData).GetEnumerator();

        // IEnumerator<T> begins before the first element.
        // Call MoveNext() to set _dataFrames.Current to the first element.
        _currentFrameIsValid = MoveNextLegacyVisible();
    }

    /// <summary>
    /// Advance the enumerator to the next frame that the legacy SOSDAC stack walker
    /// would have surfaced.
    /// </summary>
    private bool MoveNextLegacyVisible()
    {
        while (_dataFrames.MoveNext())
        {
            if (IsLegacyVisible(_dataFrames.Current))
            {
                return true;
            }
        }
        return false;
    }

    internal static bool IsLegacyVisible(IStackDataFrameHandle frame)
        => frame.State is StackWalkState.Frameless
                       or StackWalkState.Frame
                       or StackWalkState.SkippedFrame;

    private void Reseed(byte[] context, bool isFirst)
    {
        _dataFrames.Dispose();
        _dataFrames = _target.Contracts.StackWalk.CreateStackWalk(_threadData, context, isFirst).GetEnumerator();
        _currentFrameIsValid = MoveNextLegacyVisible();
    }

    int IXCLRDataStackWalk.GetContext(uint contextFlags, uint contextBufSize, uint* contextSize, [MarshalUsing(CountElementName = "contextBufSize"), Out] byte[] contextBuf)
    {
        int hr = HResults.S_OK;

        if (_currentFrameIsValid)
        {
            IStackWalk sw = _target.Contracts.StackWalk;
            IStackDataFrameHandle dataFrame = _dataFrames.Current;
            byte[] context = sw.GetRawContext(dataFrame);
            if (context.Length > contextBufSize)
                hr = HResults.E_INVALIDARG;

            if (contextSize is not null)
            {
                *contextSize = (uint)context.Length;
            }

            context.CopyTo(contextBuf);
        }
        else
        {
            hr = HResults.S_FALSE;
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

    int IXCLRDataStackWalk.GetFrame(DacComNullableByRef<IXCLRDataFrame> frame)
    {
        int hr = HResults.S_OK;

        IXCLRDataFrame? legacyFrame = null;
        if (_legacyImpl is not null)
        {
            DacComNullableByRef<IXCLRDataFrame> legacyFrameOut = new(isNullRef: false);
            int hrLocal = _legacyImpl.GetFrame(legacyFrameOut);
            if (hrLocal < 0)
                return hrLocal;
            legacyFrame = legacyFrameOut.Interface;
        }

        try
        {
            if (!_currentFrameIsValid)
                throw new ArgumentException();

            frame.Interface = new ClrDataFrame(_target, _dataFrames.Current, legacyFrame);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        return hr;
    }
    int IXCLRDataStackWalk.GetFrameType(CLRDataSimpleFrameType* simpleType, CLRDataDetailedFrameType* detailedType)
    {
        int hr = HResults.S_OK;
        try
        {
            if (_currentFrameIsValid)
            {
                IStackDataFrameHandle dataFrame = _dataFrames.Current;
                if (simpleType is not null)
                {
                    *simpleType = dataFrame.State switch
                    {
                        StackWalkState.Frameless => CLRDataSimpleFrameType.CLRDATA_SIMPFRAME_MANAGED_METHOD,
                        StackWalkState.Frame or StackWalkState.SkippedFrame => CLRDataSimpleFrameType.CLRDATA_SIMPFRAME_RUNTIME_UNMANAGED_CODE,
                        _ => CLRDataSimpleFrameType.CLRDATA_SIMPFRAME_UNRECOGNIZED,
                    };
                }

                if (detailedType is not null)
                {
                    *detailedType = dataFrame.IsExceptionFrame
                        ? CLRDataDetailedFrameType.CLRDATA_DETFRAME_EXCEPTION_FILTER
                        : CLRDataDetailedFrameType.CLRDATA_DETFRAME_UNRECOGNIZED;
                }
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
            CLRDataSimpleFrameType simpleTypeLocal = 0;
            CLRDataDetailedFrameType detailedTypeLocal = 0;
            int hrLocal = _legacyImpl.GetFrameType(&simpleTypeLocal, &detailedTypeLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                if (simpleType is not null)
                    Debug.Assert(*simpleType == simpleTypeLocal, $"cDAC: {*simpleType:x}, DAC: {simpleTypeLocal:x}");
                if (detailedType is not null)
                    Debug.Assert(*detailedType == detailedTypeLocal, $"cDAC: {*detailedType:x}, DAC: {detailedTypeLocal:x}");
            }
        }
#endif

        return hr;
    }
    int IXCLRDataStackWalk.GetStackSizeSkipped(ulong* stackSizeSkipped)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetStackSizeSkipped(stackSizeSkipped) : HResults.E_NOTIMPL;
    int IXCLRDataStackWalk.Next()
    {
        int hr;
        try
        {
            _currentFrameIsValid = MoveNextLegacyVisible();
            hr = _currentFrameIsValid ? HResults.S_OK : HResults.S_FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        // Advance the legacy stack walk to keep it in sync with the cDAC walk.
        // GetFrame() passes the legacy frame to ClrDataFrame, which delegates
        // GetArgumentByIndex/GetLocalVariableByIndex to it. If we don't advance
        // the legacy walk here, those calls operate on the wrong frame.
        if (_legacyImpl is not null)
        {
            int hrLocal = _legacyImpl.Next();
#if DEBUG
            Debug.ValidateHResult(hr, hrLocal);
#endif
        }

        return hr;
    }
    int IXCLRDataStackWalk.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
    {
        const uint DACSTACKPRIV_REQUEST_FRAME_DATA = 0xf0000000;

        int hr = HResults.S_OK;
        try
        {
            switch (reqCode)
            {
                case (uint)CLRDataGeneralRequest.CLRDATA_REQUEST_REVISION:
                    if (inBufferSize != 0 || inBuffer != null || outBufferSize != sizeof(uint) || outBuffer == null)
                        throw new ArgumentException("Invalid buffer parameters for CLRDATA_REQUEST_REVISION");
                    *(uint*)outBuffer = 1;
                    hr = HResults.S_OK;
                    break;
                case (uint)CLRDataStackWalkRequest.CLRDATA_STACK_WALK_REQUEST_SET_FIRST_FRAME:
                    if (inBufferSize != sizeof(uint) || inBuffer == null || outBufferSize != 0)
                        throw new ArgumentException("Invalid buffer parameters for CLRDATA_STACK_WALK_REQUEST_SET_FIRST_FRAME");
                    hr = HResults.S_OK; // no-op for the case where we use this
                    break;
                case DACSTACKPRIV_REQUEST_FRAME_DATA:
                    if (inBufferSize != 0 || inBuffer != null || outBufferSize != sizeof(ulong) || outBuffer == null)
                        throw new ArgumentException("Invalid buffer parameters for DACSTACKPRIV_REQUEST_FRAME_DATA");
                    if (!_currentFrameIsValid)
                        throw new ArgumentException("Invalid frame");

                    IStackWalk sw = _target.Contracts.StackWalk;
                    IStackDataFrameHandle frameData = _dataFrames.Current;
                    TargetPointer frameAddr = sw.GetFrameAddress(frameData);
                    *(ulong*)outBuffer = frameAddr.ToClrDataAddress(_target);
                    hr = HResults.S_OK;
                    break;
                default:
                    throw new ArgumentException("Invalid request code");
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            byte[] localOutBuffer = new byte[(int)outBufferSize];
            fixed (byte* localOutBufferPtr = localOutBuffer)
            {
                int hrLocal = _legacyImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, localOutBufferPtr);
                Debug.ValidateHResult(hr, hrLocal);
                if (hr == HResults.S_OK)
                    Debug.Assert(new ReadOnlySpan<byte>(outBuffer, (int)outBufferSize).SequenceEqual(localOutBuffer));
            }
        }
#endif
        return hr;
    }
    int IXCLRDataStackWalk.SetContext(uint contextSize, [In, MarshalUsing(CountElementName = "contextSize")] byte[] context)
    {
        int hr = HResults.S_OK;
        try
        {
            uint platformContextSize = IPlatformAgnosticContext.GetContextForPlatform(_target).Size;
            if (context is null || contextSize < platformContextSize)
                throw new ArgumentException("Invalid context buffer");

            bool isFirst = _currentFrameIsValid && _dataFrames.Current.IsActiveFrame;
            Reseed(context, isFirst);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        if (_legacyImpl is not null)
        {
            int hrLocal = _legacyImpl.SetContext(contextSize, context);
#if DEBUG
            Debug.ValidateHResult(hr, hrLocal);
#endif
        }

        return hr;
    }

    int IXCLRDataStackWalk.SetContext2(uint flags, uint contextSize, [In, MarshalUsing(CountElementName = "contextSize")] byte[] context)
        => HResults.E_NOTIMPL;
}
