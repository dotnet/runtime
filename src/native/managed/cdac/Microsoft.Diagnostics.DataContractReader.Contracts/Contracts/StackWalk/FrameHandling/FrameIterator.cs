// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

/// <summary>
/// Walks the linked list of capital-F <see cref="Data.Frame"/> structures pushed on a
/// managed thread (Thread::m_pFrame chain), maintaining a single current-frame cursor.
/// This class only owns iteration state; per-frame inspection and operations live in
/// <see cref="FrameHelpers"/>. Convenience methods on this class forward to
/// <see cref="FrameHelpers"/> for the current frame.
/// </summary>
internal sealed class FrameIterator
{
    private readonly Target target;
    private readonly TargetPointer terminator;
    private readonly FrameHelpers frameHelpers;
    private TargetPointer currentFramePointer;

    internal Data.Frame CurrentFrame => target.ProcessedData.GetOrAdd<Data.Frame>(currentFramePointer);

    public TargetPointer CurrentFrameAddress => currentFramePointer;

    public FrameIterator(Target target, ThreadData threadData)
    {
        this.target = target;
        terminator = new TargetPointer(target.PointerSize == 8 ? ulong.MaxValue : uint.MaxValue);
        frameHelpers = new FrameHelpers(target);
        currentFramePointer = threadData.Frame;
    }

    public bool IsValid()
    {
        return currentFramePointer != terminator;
    }

    public bool Next()
    {
        if (currentFramePointer == terminator)
            return false;

        currentFramePointer = CurrentFrame.Next;
        return currentFramePointer != terminator;
    }

    /// <summary>
    /// Returns the <see cref="FrameType"/> of the current frame.
    /// </summary>
    public FrameType GetCurrentFrameType()
        => frameHelpers.GetFrameType(CurrentFrame.Identifier);

    /// <summary>
    /// Returns the return address of the current frame, matching native Frame::GetReturnAddress().
    /// </summary>
    public TargetPointer GetCurrentReturnAddress()
        => frameHelpers.GetReturnAddress(CurrentFrame);

    /// <summary>
    /// Updates <paramref name="context"/> based on the current frame's type.
    /// </summary>
    public void UpdateContextFromCurrentFrame(IPlatformAgnosticContext context)
        => frameHelpers.UpdateContextFromFrame(CurrentFrame, context);

    /// <summary>
    /// Returns the InternalFrameType (CorDebugInternalFrameType) of the current Frame.
    /// Mirrors the native DacDbiInterfaceImpl::GetInternalFrameType logic.
    /// </summary>
    public InternalFrameType GetCurrentInternalFrameType() => frameHelpers.GetInternalFrameType(currentFramePointer);

}
