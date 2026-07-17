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
    private const uint SimpleFrameUnrecognized = 0x1;
    private const uint SimpleFrameManagedMethod = 0x2;
    private const uint SimpleFrameRuntimeUnmanagedCode = 0x8;
    private const uint DetailedFrameUnrecognized = 0;
    private const uint DetailedFrameExceptionFilter = 3;
    private const uint StackSetCurrentContext = 0x1;
    private const uint RequestSetFirstFrame = 0xe1000000;
    private const uint RequestFrameData = 0xf0000000;

    private readonly TargetPointer _threadAddr;
    private readonly uint _flags;
    private readonly Target _target;
    private readonly IXCLRDataStackWalk? _legacyImpl;

    private bool _currentFrameIsValid;
    private bool _currentFrameIsActive;
    private bool _currentFrameIsException;
    private IEnumerator<IStackDataFrameHandle> _dataFrames;
    private byte[] _currentContext = [];
    private ulong _currentStackPointer;
    private ulong? _stackPrevious;

    public ClrDataStackWalk(TargetPointer threadAddr, uint flags, Target target, IXCLRDataStackWalk? legacyImpl)
    {
        _threadAddr = threadAddr;
        _flags = flags;
        _target = target;
        _legacyImpl = legacyImpl;

        ThreadData threadData = _target.Contracts.Thread.GetThreadData(_threadAddr);
        _dataFrames = _target.Contracts.StackWalk.CreateStackWalk(threadData).GetEnumerator();
        InitializeCurrentFrame();
    }

    private void InitializeCurrentFrame()
    {
        _currentFrameIsValid = false;
        _currentFrameIsActive = false;
        _currentFrameIsException = false;
        _currentContext = [];
        _currentStackPointer = 0;
        _stackPrevious = null;

        if (_dataFrames.MoveNext())
        {
            UpdateCurrentFrame();
            _stackPrevious = _currentStackPointer;
            _currentFrameIsValid = MoveToVisibleFrame();
        }
    }

    private bool MoveToVisibleFrame()
    {
        while (true)
        {
            if (IsVisible(_dataFrames.Current))
            {
                _currentFrameIsActive = _dataFrames.Current.IsActiveFrame;
                _currentFrameIsException = _dataFrames.Current.IsExceptionFrame;
                return true;
            }

            if (!_dataFrames.MoveNext())
            {
                _currentFrameIsActive = false;
                _currentFrameIsException = false;
                return false;
            }

            UpdateCurrentFrame();
        }
    }

    private bool MoveNextVisibleFrame()
    {
        if (!_currentFrameIsValid)
            return false;

        _stackPrevious = _currentStackPointer;
        if (!_dataFrames.MoveNext())
        {
            _currentFrameIsActive = false;
            _currentFrameIsException = false;
            return false;
        }

        UpdateCurrentFrame();
        _stackPrevious = _currentStackPointer;
        return MoveToVisibleFrame();
    }

    private bool IsVisible(IStackDataFrameHandle frame)
        => frame.State switch
        {
            StackWalkState.Frameless => (_flags & SimpleFrameManagedMethod) != 0,
            StackWalkState.Frame or StackWalkState.SkippedFrame => (_flags & SimpleFrameRuntimeUnmanagedCode) != 0,
            _ => false,
        };

    private void UpdateCurrentFrame()
    {
        _currentContext = _target.Contracts.StackWalk.GetRawContext(_dataFrames.Current);
        IPlatformAgnosticContext context = IPlatformAgnosticContext.GetContextForPlatform(_target);
        context.FillFromBuffer(_currentContext);
        _currentStackPointer = context.StackPointer.Value;
    }

    private void ResetStackWalk(byte[] context, bool isFirst)
    {
        ThreadData threadData = _target.Contracts.Thread.GetThreadData(_threadAddr);
        IEnumerator<IStackDataFrameHandle> dataFrames =
            _target.Contracts.StackWalk.CreateStackWalk(threadData, context, isFirst).GetEnumerator();

        _dataFrames.Dispose();
        _dataFrames = dataFrames;
        InitializeCurrentFrame();
    }

    int IXCLRDataStackWalk.GetContext(uint contextFlags, uint contextBufSize, uint* contextSize, [MarshalUsing(CountElementName = "contextBufSize"), Out] byte[] contextBuf)
    {
        int hr = HResults.S_OK;
        try
        {
            IPlatformAgnosticContext context = IPlatformAgnosticContext.GetContextForPlatform(_target);
            uint requiredContextSize = GetContextSizeForFlags(context, contextFlags);

            if (contextSize is not null)
                *contextSize = requiredContextSize;

            if (contextBuf is null || contextBufSize < requiredContextSize || (uint)contextBuf.Length < contextBufSize)
                throw new ArgumentException();

            if (!_currentFrameIsValid)
            {
                hr = HResults.S_FALSE;
            }
            else
            {
                Array.Copy(_currentContext, 0, contextBuf, 0, Math.Min(_currentContext.Length, (int)contextBufSize));
            }
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
    int IXCLRDataStackWalk.GetFrameType(uint* simpleType, uint* detailedType)
    {
        int hr = HResults.S_OK;
        try
        {
            if (!_currentFrameIsValid)
            {
                hr = HResults.S_FALSE;
            }
            else
            {
                if (simpleType is not null)
                {
                    *simpleType = _dataFrames.Current.State switch
                    {
                        StackWalkState.Frameless => SimpleFrameManagedMethod,
                        StackWalkState.Frame or StackWalkState.SkippedFrame => SimpleFrameRuntimeUnmanagedCode,
                        _ => SimpleFrameUnrecognized,
                    };
                }

                if (detailedType is not null)
                    *detailedType = _currentFrameIsException ? DetailedFrameExceptionFilter : DetailedFrameUnrecognized;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            uint simpleTypeLocal = 0;
            uint detailedTypeLocal = 0;
            int hrLocal = _legacyImpl.GetFrameType(
                simpleType is null ? null : &simpleTypeLocal,
                detailedType is null ? null : &detailedTypeLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                if (simpleType is not null)
                    Debug.Assert(*simpleType == simpleTypeLocal);
                if (detailedType is not null)
                    Debug.Assert(*detailedType == detailedTypeLocal);
            }
        }
#endif

        return hr;
    }

    int IXCLRDataStackWalk.GetStackSizeSkipped(ulong* stackSizeSkipped)
    {
        int hr = HResults.S_OK;
        try
        {
            if (_stackPrevious is null)
            {
                hr = HResults.S_FALSE;
            }
            else
            {
                if (stackSizeSkipped is null)
                    throw new ArgumentException();

                *stackSizeSkipped = unchecked(_currentStackPointer - _stackPrevious.Value);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            ulong stackSizeSkippedLocal = 0;
            int hrLocal = _legacyImpl.GetStackSizeSkipped(&stackSizeSkippedLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*stackSizeSkipped == stackSizeSkippedLocal);
        }
#endif

        return hr;
    }

    int IXCLRDataStackWalk.Next()
    {
        int hr;
        try
        {
            _currentFrameIsValid = MoveNextVisibleFrame();
            hr = _currentFrameIsValid ? HResults.S_OK : HResults.S_FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

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
        int hr = HResults.S_OK;
        try
        {
            switch (reqCode)
            {
                case (uint)CLRDataGeneralRequest.CLRDATA_REQUEST_REVISION:
                    if (inBufferSize != 0 || inBuffer is not null || outBufferSize != sizeof(uint))
                        throw new ArgumentException();
                    *(uint*)outBuffer = 1;
                    break;
                case RequestSetFirstFrame:
                    if (inBufferSize != sizeof(uint) || outBufferSize != 0)
                        throw new ArgumentException();
                    _currentFrameIsActive = *(uint*)inBuffer != 0
                        && _currentFrameIsValid
                        && _dataFrames.Current.State == StackWalkState.Frameless;
                    break;
                case RequestFrameData:
                    if (inBufferSize != 0 || inBuffer is not null || outBufferSize != sizeof(ulong))
                        throw new ArgumentException();
                    if (!_currentFrameIsValid)
                        throw new ArgumentException();

                    IStackWalk sw = _target.Contracts.StackWalk;
                    IStackDataFrameHandle frameData = _dataFrames.Current;
                    TargetPointer frameAddr = sw.GetFrameAddress(frameData);
                    *(ulong*)outBuffer = frameAddr.ToClrDataAddress(_target);
                    break;
                default:
                    throw new ArgumentException();
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        int hrLocal = HResults.S_OK;
        byte[]? localOutBuffer = null;
        if (_legacyImpl is not null)
        {
            localOutBuffer = new byte[outBufferSize];
            fixed (byte* localOutBufferPtr = localOutBuffer)
            {
                hrLocal = _legacyImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, localOutBufferPtr);
            }
        }

        if (_legacyImpl is not null)
        {
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                for (int i = 0; i < outBufferSize; i++)
                {
                    Debug.Assert(localOutBuffer![i] == outBuffer[i], $"cDAC: {outBuffer[i]:x}, DAC: {localOutBuffer[i]:x}");
                }
            }
        }
#else
        if (reqCode == RequestSetFirstFrame)
            _legacyImpl?.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer);
#endif
        return hr;
    }

    int IXCLRDataStackWalk.SetContext(uint contextSize, [In, MarshalUsing(CountElementName = "contextSize")] byte[] context)
    {
        int hr = SetContext(contextSize, context, _currentFrameIsActive);
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
    {
        int hr;
        if ((flags & ~StackSetCurrentContext) != 0)
        {
            hr = HResults.E_INVALIDARG;
        }
        else
        {
            hr = SetContext(contextSize, context, (flags & StackSetCurrentContext) != 0);
        }

        if (_legacyImpl is not null)
        {
            int hrLocal = _legacyImpl.SetContext2(flags, contextSize, context);
#if DEBUG
            Debug.ValidateHResult(hr, hrLocal);
#endif
        }

        return hr;
    }

    private int SetContext(uint contextSize, byte[] context, bool isFirst)
    {
        int hr = HResults.S_OK;
        try
        {
            IPlatformAgnosticContext platformContext = IPlatformAgnosticContext.GetContextForPlatform(_target);
            uint contextFlagsOffset = _target.Contracts.RuntimeInfo.GetTargetArchitecture() == RuntimeInfoArchitecture.X64 ? 0x30u : 0;
            if (context is null
                || contextSize < contextFlagsOffset + sizeof(uint)
                || contextSize > (uint)context.Length)
                throw new ArgumentException();

            platformContext.FillFromBuffer(context.AsSpan(0, (int)contextSize));
            if (contextSize < GetContextSizeForFlags(platformContext, platformContext.RawContextFlags))
                throw new ArgumentException();

            ResetStackWalk(context.AsSpan(0, (int)contextSize).ToArray(), isFirst);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        return hr;
    }

    private uint GetContextSizeForFlags(IPlatformAgnosticContext context, uint contextFlags)
    {
        const uint X86ContextExtendedRegisters = 0x00010020;
        const uint X86ExtendedRegistersOffset = 0xcc;

        return _target.Contracts.RuntimeInfo.GetTargetArchitecture() == RuntimeInfoArchitecture.X86
            && (contextFlags & X86ContextExtendedRegisters) != X86ContextExtendedRegisters
                ? X86ExtendedRegistersOffset
                : context.Size;
    }
}
