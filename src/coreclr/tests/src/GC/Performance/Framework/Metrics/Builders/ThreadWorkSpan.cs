// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if WINDOWS

using Microsoft.Diagnostics.Tracing.Parsers.Kernel;

namespace GCPerfTestFramework.Metrics.Builders
{
    /// <summary>
    /// Span of thread work recorded by CSwitch or CPU Sample Profile events
    /// </summary>
    internal class ThreadWorkSpan
    {
        public int ThreadId;
        public int ProcessId;
        public string ProcessName;
        public int ProcessorNumber;
        public double AbsoluteTimestampMsc;
        public double DurationMsc;
        public int Priority = -1;
        public int WaitReason = -1;

        public ThreadWorkSpan(CSwitchTraceData switchData)
        {
            ProcessName = switchData.NewProcessName;
            ThreadId = switchData.NewThreadID;
            ProcessId = switchData.NewProcessID;
            ProcessorNumber = switchData.ProcessorNumber;
            AbsoluteTimestampMsc = switchData.TimeStampRelativeMSec;
            Priority = switchData.NewThreadPriority;
            WaitReason = (int)switchData.OldThreadWaitReason;
        }

        public ThreadWorkSpan(ThreadWorkSpan span)
        {
            ProcessName = span.ProcessName;
            ThreadId = span.ThreadId;
            ProcessId = span.ProcessId;
            ProcessorNumber = span.ProcessorNumber;
            AbsoluteTimestampMsc = span.AbsoluteTimestampMsc;
            DurationMsc = span.DurationMsc;
            Priority = span.Priority;
            WaitReason = span.WaitReason;
        }

        public ThreadWorkSpan(SampledProfileTraceData sample)
        {
            ProcessName = sample.ProcessName;
            ProcessId = sample.ProcessID;
            ThreadId = sample.ThreadID;
            ProcessorNumber = sample.ProcessorNumber;
            AbsoluteTimestampMsc = sample.TimeStampRelativeMSec;
            DurationMsc = 1;
            Priority = 0;
        }
    }
}

#endif
