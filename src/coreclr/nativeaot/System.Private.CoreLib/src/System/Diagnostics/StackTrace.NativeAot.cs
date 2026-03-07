// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;

using Internal.Runtime.Augments;

namespace System.Diagnostics
{
    public partial class StackTrace
    {
#if !TARGET_WASM
        /// <summary>
        /// Initialize the stack trace based on current thread and given initial frame index.
        /// </summary>
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private void InitializeForCurrentThread(int skipFrames, bool needFileInfo)
        {
            const int SystemDiagnosticsStackDepth = 2;

            int frameCount = -RuntimeImports.RhGetCurrentThreadStackTrace(Array.Empty<IntPtr>());
            Debug.Assert(frameCount >= 0);
            IntPtr[] stackTrace = new IntPtr[frameCount];
            int trueFrameCount = RuntimeImports.RhGetCurrentThreadStackTrace(stackTrace);
            Debug.Assert(trueFrameCount == frameCount);

            int adjustedSkip = skipFrames + SystemDiagnosticsStackDepth;
            stackTrace = TryAugmentWithAsyncContinuations(stackTrace, adjustedSkip, ref trueFrameCount);

            InitializeForIpAddressArray(stackTrace, adjustedSkip, trueFrameCount, needFileInfo);
        }

        /// <summary>
        /// When executing inside a runtime async (v2) continuation dispatch, augment the
        /// collected IP array by truncating internal dispatch frames and appending
        /// continuation DiagnosticIP values from the async continuation chain.
        /// </summary>
        private static unsafe IntPtr[] TryAugmentWithAsyncContinuations(IntPtr[] stackTrace, int skipFrames, ref int frameCount)
        {
            AsyncDispatcherInfo* pInfo = AsyncDispatcherInfo.t_current;
            if (pInfo is null)
                return stackTrace;

            // Walk the continuation chain and collect DiagnosticIPs.
            int continuationCount = 0;
            Span<IntPtr> continuationIPs = stackalloc IntPtr[64];

            for (AsyncDispatcherInfo* pCurrent = pInfo; pCurrent is not null; pCurrent = pCurrent->Next)
            {
                Continuation? cont = pCurrent->NextContinuation;
                while (cont is not null)
                {
                    if (cont.ResumeInfo is not null && cont.ResumeInfo->DiagnosticIP is not null)
                    {
                        if (continuationCount < continuationIPs.Length)
                        {
                            continuationIPs[continuationCount] = (IntPtr)cont.ResumeInfo->DiagnosticIP;
                        }
                        continuationCount++;
                    }
                    cont = cont.Next;
                }
            }

            if (continuationCount == 0)
                return stackTrace;

            // If we overflowed the stackalloc buffer, heap-allocate and re-walk.
            if (continuationCount > continuationIPs.Length)
            {
                continuationIPs = new IntPtr[continuationCount];
                continuationCount = 0;
                for (AsyncDispatcherInfo* pCurrent = pInfo; pCurrent is not null; pCurrent = pCurrent->Next)
                {
                    Continuation? cont = pCurrent->NextContinuation;
                    while (cont is not null)
                    {
                        if (cont.ResumeInfo is not null && cont.ResumeInfo->DiagnosticIP is not null)
                        {
                            continuationIPs[continuationCount++] = (IntPtr)cont.ResumeInfo->DiagnosticIP;
                        }
                        cont = cont.Next;
                    }
                }
            }

            // Find the truncation point: the first StackTraceHidden frame from
            // RuntimeAsyncTaskCore (i.e. DispatchContinuations) marks the boundary
            // between user async frames and internal dispatch machinery.
            int truncateAt = FindAsyncDispatchBoundary(stackTrace, skipFrames, frameCount);
            if (truncateAt < 0)
                return stackTrace;

            // Build augmented IP array: user frames up to the boundary, then continuation IPs.
            int newCount = truncateAt + continuationCount;
            IntPtr[] result = new IntPtr[newCount];
            Array.Copy(stackTrace, 0, result, 0, truncateAt);
            continuationIPs.Slice(0, continuationCount).CopyTo(result.AsSpan(truncateAt));

            frameCount = newCount;
            return result;
        }

        /// <summary>
        /// Scan the IP array looking for the DispatchContinuations frame from
        /// RuntimeAsyncTaskCore. Returns the index at which to truncate, or -1
        /// if no boundary was found.
        /// </summary>
        private static int FindAsyncDispatchBoundary(IntPtr[] stackTrace, int skipFrames, int frameCount)
        {
            StackTraceMetadataCallbacks? callbacks = RuntimeAugments.StackTraceCallbacksIfAvailable;
            if (callbacks is null)
                return -1;

            for (int i = skipFrames; i < frameCount; i++)
            {
                IntPtr ip = stackTrace[i];
                if (ip == IntPtr.Zero || ip == Exception.EdiSeparator)
                    continue;

                IntPtr methodStart = RuntimeImports.RhFindMethodStartAddress(ip);
                if (methodStart == IntPtr.Zero)
                    continue;

                int nativeOffset = (int)((nint)ip - (nint)methodStart);
                callbacks.TryGetMethodStackFrameInfo(
                    methodStart, nativeOffset, false,
                    out string? owningType, out _, out _, out bool isHidden, out _, out _);

                if (isHidden && owningType is not null &&
                    owningType.Contains("RuntimeAsyncTask", StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }
#endif

        /// <summary>
        /// Initialize the stack trace based on a given exception and initial frame index.
        /// </summary>
        private void InitializeForException(Exception exception, int skipFrames, bool needFileInfo)
        {
            IntPtr[] stackIPs = exception.GetStackIPs();
            InitializeForIpAddressArray(stackIPs, skipFrames, stackIPs.Length, needFileInfo);
        }

        /// <summary>
        /// Initialize the stack trace based on a given array of IP addresses.
        /// </summary>
        private void InitializeForIpAddressArray(IntPtr[] ipAddresses, int skipFrames, int endFrameIndex, bool needFileInfo)
        {
            int frameCount = (skipFrames < endFrameIndex ? endFrameIndex - skipFrames : 0);

            // Calculate true frame count upfront - we need to skip EdiSeparators which get
            // collapsed onto boolean flags on the preceding stack frame
            int outputFrameCount = 0;
            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                if (ipAddresses[frameIndex + skipFrames] != Exception.EdiSeparator)
                {
                    outputFrameCount++;
                }
            }

            if (outputFrameCount > 0)
            {
                _stackFrames = new StackFrame[outputFrameCount];
                int outputFrameIndex = 0;
                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                {
                    IntPtr ipAddress = ipAddresses[frameIndex + skipFrames];
                    if (ipAddress != Exception.EdiSeparator)
                    {
                        _stackFrames[outputFrameIndex++] = new StackFrame(ipAddress, needFileInfo);
                    }
                    else if (outputFrameIndex > 0)
                    {
                        _stackFrames[outputFrameIndex - 1].SetIsLastFrameFromForeignExceptionStackTrace();
                    }
                }
                Debug.Assert(outputFrameIndex == outputFrameCount);
            }

            _numOfFrames = outputFrameCount;
            _methodsToSkip = 0;
        }

#if !TARGET_WASM
        internal void ToString(TraceFormat traceFormat, StringBuilder builder)
        {
            if (_stackFrames != null)
            {
                foreach (StackFrame frame in _stackFrames)
                {
                    frame?.AppendToStackTrace(builder);
                }
            }

            if (traceFormat == TraceFormat.Normal && builder.Length >= Environment.NewLine.Length)
                builder.Length -= Environment.NewLine.Length;

            if (traceFormat == TraceFormat.TrailingNewLine && builder.Length == 0)
                builder.AppendLine();
        }
#endif
    }
}
