// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

/// <summary>
/// Interface for handling platform-specific frames.
/// </summary>
internal interface IPlatformFrameHandler
{
    void HandleInlinedCallFrame(Data.InlinedCallFrame frame);
    void HandleSoftwareExceptionFrame(Data.SoftwareExceptionFrame frame);
    void HandleTransitionFrame(Data.FramedMethodFrame frame);
    void HandleFuncEvalFrame(Data.FuncEvalFrame frame);
    void HandleResumableFrame(Data.ResumableFrame frame);
    void HandleFaultingExceptionFrame(Data.FaultingExceptionFrame frame);
    void HandleHijackFrame(Data.HijackFrame frame);
}
