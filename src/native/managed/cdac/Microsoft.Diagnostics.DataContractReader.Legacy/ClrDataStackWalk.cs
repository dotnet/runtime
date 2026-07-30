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

    private bool _currentFrameIsValid;
    private readonly IEnumerator<IStackDataFrameHandle> _dataFrames;

    public ClrDataStackWalk(TargetPointer threadAddr, uint flags, Target target)
    {
        _threadAddr = threadAddr;
        _flags = flags;
        _target = target;

        ThreadData threadData = _target.Contracts.Thread.GetThreadData(_threadAddr);
        _dataFrames = _target.Contracts.StackWalk.CreateStackWalk(threadData).GetEnumerator();

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



        return hr;
    }

    int IXCLRDataStackWalk.GetFrame(DacComNullableByRef<IXCLRDataFrame> frame)
    {
        int hr = HResults.S_OK;

        try
        {
            if (!_currentFrameIsValid)
                throw new ArgumentException();

            frame.Interface = new ClrDataFrame(_target, _dataFrames.Current);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        return hr;
    }
    int IXCLRDataStackWalk.GetFrameType(uint* simpleType, uint* detailedType)
        => HResults.E_NOTIMPL;
    int IXCLRDataStackWalk.GetStackSizeSkipped(ulong* stackSizeSkipped)
        => HResults.E_NOTIMPL;
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
        // GetArgumentByIndex/GetLocalVariableByIndex to it. If we don't advance

        return hr;
    }
    int IXCLRDataStackWalk.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
    {
        const uint DACSTACKPRIV_REQUEST_FRAME_DATA = 0xf0000000;

        int hr = HResults.S_OK;
        try
        {
            if (inBufferSize != 0 || inBuffer != null)
                throw new ArgumentException("Invalid input buffer parameters");
            switch (reqCode)
            {
                case (uint)CLRDataGeneralRequest.CLRDATA_REQUEST_REVISION:
                    if (outBufferSize != sizeof(uint))
                        throw new ArgumentException("Invalid buffer parameters for CLRDATA_REQUEST_REVISION");
                    *(uint*)outBuffer = 1;
                    hr = HResults.S_OK;
                    break;
                case DACSTACKPRIV_REQUEST_FRAME_DATA:
                    if (outBufferSize != sizeof(ulong))
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
                    throw new NotImplementedException();
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        return hr;
    }
    int IXCLRDataStackWalk.SetContext(uint contextSize, [In, MarshalUsing(CountElementName = "contextSize")] byte[] context)
        => HResults.E_NOTIMPL;
    int IXCLRDataStackWalk.SetContext2(uint flags, uint contextSize, [In, MarshalUsing(CountElementName = "contextSize")] byte[] context)
        => HResults.E_NOTIMPL;
}
