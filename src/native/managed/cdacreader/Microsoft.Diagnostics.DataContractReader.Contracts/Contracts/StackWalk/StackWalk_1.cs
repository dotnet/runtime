// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct StackWalk_1 : IStackWalk
{
    private readonly Target _target;

    internal StackWalk_1(Target target)
    {
        _target = target;
    }

    public enum StackWalkState
    {
        SW_COMPLETE,
        SW_ERROR,

        // The current Context is managed
        SW_FRAMELESS,

        // The current Context is unmanaged.
        // The next update will use a Frame to get a managed context
        // When SW_FRAME, the FrameAddress is valid
        SW_FRAME,
        SW_SKIPPED_FRAME,
    }

    internal struct StackDataFrameHandle : IStackDataFrameHandle
    {
        internal IPlatformAgnosticContext Context { get; init; }
        internal StackWalkState State { get; init; }
        internal TargetPointer FrameAddress { get; init; }
    }

    internal class StackWalkHandle : IStackWalkHandle
    {
        public StackWalkState state;
        public IPlatformAgnosticContext context;
        public FrameIterator frameIter;

        public StackWalkHandle(IPlatformAgnosticContext context, FrameIterator frameIter, StackWalkState state)
        {
            this.context = context;
            this.frameIter = frameIter;
            this.state = state;
        }
    }

    IStackWalkHandle IStackWalk.CreateStackWalk(ThreadData threadData)
    {
        IPlatformAgnosticContext context = IPlatformAgnosticContext.GetContextForPlatform(_target);
        FillContextFromThread(ref context, threadData);
        StackWalkState state = IsManaged(context.InstructionPointer, out _) ? StackWalkState.SW_FRAMELESS : StackWalkState.SW_FRAME;
        return new StackWalkHandle(context, new(_target, threadData), state);
    }

    bool IStackWalk.Next(IStackWalkHandle stackWalkHandle)
    {
        StackWalkHandle handle = AssertCorrectHandle(stackWalkHandle);

        switch (handle.state)
        {
            case StackWalkState.SW_FRAMELESS:
                try
                {
                    handle.context.Unwind(_target);
                }
                catch
                {
                    handle.state = StackWalkState.SW_ERROR;
                    throw;
                }
                break;
            case StackWalkState.SW_SKIPPED_FRAME:
                handle.frameIter.Next();
                break;
            case StackWalkState.SW_FRAME:
                handle.frameIter.TryUpdateContext(ref handle.context);
                if (!handle.frameIter.IsInlineCallFrameWithActiveCall())
                {
                    handle.frameIter.Next();
                }
                break;
            case StackWalkState.SW_ERROR:
            case StackWalkState.SW_COMPLETE:
                return false;
        }
        UpdateState(handle);

        return handle.state is not (StackWalkState.SW_ERROR or StackWalkState.SW_COMPLETE);
    }

    private void UpdateState(StackWalkHandle handle)
    {
        // If we are complete or in a bad state, no updating is required.
        if (handle.state is StackWalkState.SW_ERROR or StackWalkState.SW_COMPLETE)
        {
            return;
        }

        bool isManaged = IsManaged(handle.context.InstructionPointer, out _);
        bool validFrame = handle.frameIter.IsValid();

        if (isManaged)
        {
            handle.state = StackWalkState.SW_FRAMELESS;
            if (CheckForSkippedFrames(handle))
            {
                handle.state = StackWalkState.SW_SKIPPED_FRAME;
                return;
            }
        }
        else
        {
            handle.state = validFrame ? StackWalkState.SW_FRAME : StackWalkState.SW_COMPLETE;
        }
    }

    private bool CheckForSkippedFrames(StackWalkHandle handle)
    {
        // ensure we can find the caller context
        Debug.Assert(IsManaged(handle.context.InstructionPointer, out _));

        // if there are no more Frames, vacuously false
        if (!handle.frameIter.IsValid())
        {
            return false;
        }

        // get the caller context
        IPlatformAgnosticContext parentContext = handle.context.Clone();
        parentContext.Unwind(_target);

        return handle.frameIter.CurrentFrameAddress.Value < parentContext.StackPointer.Value;
    }

    IStackDataFrameHandle IStackWalk.GetCurrentFrame(IStackWalkHandle stackWalkHandle)
    {
        StackWalkHandle handle = AssertCorrectHandle(stackWalkHandle);
        return new StackDataFrameHandle
        {
            Context = handle.context.Clone(),
            State = handle.state,
            FrameAddress = handle.frameIter.CurrentFrameAddress,
        };
    }

    byte[] IStackWalk.GetRawContext(IStackDataFrameHandle stackDataFrameHandle)
    {
        StackDataFrameHandle handle = AssertCorrectHandle(stackDataFrameHandle);
        return handle.Context.GetBytes();
    }

    TargetPointer IStackWalk.GetFrameAddress(IStackDataFrameHandle stackDataFrameHandle)
    {
        StackDataFrameHandle handle = AssertCorrectHandle(stackDataFrameHandle);
        if (handle.State is StackWalkState.SW_FRAME or StackWalkState.SW_SKIPPED_FRAME)
        {
            return handle.FrameAddress;
        }
        return TargetPointer.Null;
    }

    private bool IsManaged(TargetPointer ip, [NotNullWhen(true)] out CodeBlockHandle? codeBlockHandle)
    {
        IExecutionManager eman = _target.Contracts.ExecutionManager;
        TargetCodePointer codePointer = CodePointerUtils.CodePointerFromAddress(ip, _target);
        if (eman.GetCodeBlockHandle(codePointer) is CodeBlockHandle cbh && cbh.Address != TargetPointer.Null)
        {
            codeBlockHandle = cbh;
            return true;
        }
        codeBlockHandle = default;
        return false;
    }

    private unsafe void FillContextFromThread(ref IPlatformAgnosticContext refContext, ThreadData threadData)
    {
        byte[] bytes = new byte[refContext.Size];
        Span<byte> buffer = new Span<byte>(bytes);
        int hr = _target.GetThreadContext((uint)threadData.OSId.Value, refContext.DefaultContextFlags, refContext.Size, buffer);
        if (hr != 0)
        {
            throw new InvalidOperationException($"GetThreadContext failed with hr={hr}");
        }

        refContext.FillFromBuffer(buffer);
    }

    private static StackWalkHandle AssertCorrectHandle(IStackWalkHandle stackWalkHandle)
    {
        if (stackWalkHandle is not StackWalkHandle handle)
        {
            throw new ArgumentException("Invalid stack walk handle", nameof(stackWalkHandle));
        }

        return handle;
    }

    private static StackDataFrameHandle AssertCorrectHandle(IStackDataFrameHandle stackDataFrameHandle)
    {
        if (stackDataFrameHandle is not StackDataFrameHandle handle)
        {
            throw new ArgumentException("Invalid stack data frame handle", nameof(stackDataFrameHandle));
        }

        return handle;
    }
};
