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
    bool HandleInlinedCallFrame(Data.InlinedCallFrame frame);
    bool HandleSoftwareExceptionFrame(Data.SoftwareExceptionFrame frame);
    bool HandleTransitionFrame(Data.FramedMethodFrame frame, Data.TransitionBlock transitionBlock, uint transitionBlockSize);
    bool HandleFuncEvalFrame(Data.FuncEvalFrame frame, Data.DebuggerEval debuggerEval);
    bool HandleResumableFrame(Data.ResumableFrame frame);
    bool HandleFaultingExceptionFrame(Data.FaultingExceptionFrame frame);
    bool HandleHijackFrame(Data.HijackFrame frame);
}
