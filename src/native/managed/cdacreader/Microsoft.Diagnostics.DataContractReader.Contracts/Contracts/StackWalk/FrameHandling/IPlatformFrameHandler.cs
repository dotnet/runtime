// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

/// <summary>
/// Interface for handling platform-specific frames.
/// Methods return true if the context was updated from the Frame. False otherwise.
/// </summary>
internal interface IPlatformFrameHandler
{
    public abstract bool HandleInlinedCallFrame(Data.InlinedCallFrame frame);
    public abstract bool HandleSoftwareExceptionFrame(Data.SoftwareExceptionFrame frame);
    public abstract bool HandleTransitionFrame(Data.FramedMethodFrame frame, Data.TransitionBlock transitionBlock, uint transitionBlockSize);
    public abstract bool HandleFuncEvalFrame(Data.FuncEvalFrame frame, Data.DebuggerEval debuggerEval);
    public abstract bool HandleResumableFrame(Data.ResumableFrame frame);
    public abstract bool HandleFaultingExceptionFrame(Data.FaultingExceptionFrame frame);
    public abstract bool HandleHijackFrame(Data.HijackFrame frame);
}
