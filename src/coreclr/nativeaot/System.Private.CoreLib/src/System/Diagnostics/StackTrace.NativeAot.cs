// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;

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

            // Read the config to determine async behavior before collecting continuations.
            int hideMode = GetHideAsyncDispatchMode();

            // Mode 2 (physical only): skip continuation collection entirely.
            IntPtr[]? continuationIPs = hideMode == 2 ? null : CollectAsyncContinuationIPs();
            InitializeForIpAddressArray(stackTrace, adjustedSkip, trueFrameCount, needFileInfo, continuationIPs, hideMode);
        }

        private static int s_hideAsyncDispatchMode = -1;

        private static int GetHideAsyncDispatchMode()
        {
            int cached = s_hideAsyncDispatchMode;
            if (cached >= 0)
                return cached;

            string? envValue = Environment.GetEnvironmentVariable("DOTNET_StackTraceAsyncBehavior");
            cached = envValue switch
            {
                "0" => 0,
                "2" => 2,
                "3" => 3,
                _ => 1,
            };
            s_hideAsyncDispatchMode = cached;
            return cached;
        }

        /// <summary>
        /// When executing inside a runtime async (v2) continuation dispatch, collect
        /// the DiagnosticIP values from the async continuation chain.
        /// Returns null if not inside a dispatch or no valid continuation IPs exist.
        /// </summary>
        private static unsafe IntPtr[]? CollectAsyncContinuationIPs()
        {
            AsyncDispatcherInfo* pInfo = AsyncDispatcherInfo.t_current;
            if (pInfo is null)
                return null;

            // Only collect continuations from the innermost (first) dispatcher in the chain.
            // Outer dispatchers represent already-completed async scopes and are not displayed.
            Continuation? cont = pInfo->NextContinuation;
            if (cont is null)
                return null;

            IntPtr[] buffer = new IntPtr[16];
            int count = 0;
            while (cont is not null)
            {
                if (cont.ResumeInfo is not null && cont.ResumeInfo->DiagnosticIP is not null)
                {
                    if (count == buffer.Length)
                        Array.Resize(ref buffer, buffer.Length * 2);

                    buffer[count++] = (IntPtr)cont.ResumeInfo->DiagnosticIP;
                }
                cont = cont.Next;
            }

            if (count == 0)
                return null;

            if (count < buffer.Length)
                Array.Resize(ref buffer, count);

            return buffer;
        }
#endif

        /// <summary>
        /// Initialize the stack trace based on a given exception and initial frame index.
        /// </summary>
        private void InitializeForException(Exception exception, int skipFrames, bool needFileInfo)
        {
            IntPtr[] stackIPs = exception.GetStackIPs();
            InitializeForIpAddressArray(stackIPs, skipFrames, stackIPs.Length, needFileInfo, null, 0);
        }

        /// <summary>
        /// Initialize the stack trace based on a given array of IP addresses.
        /// When continuationIPs is provided, detects the async dispatch boundary
        /// during frame construction and splices in continuation frames.
        /// </summary>
        private void InitializeForIpAddressArray(IntPtr[] ipAddresses, int skipFrames, int endFrameIndex, bool needFileInfo, IntPtr[]? continuationIPs, int hideAsyncDispatchMode)
        {
            int frameCount = (skipFrames < endFrameIndex ? endFrameIndex - skipFrames : 0);
            int continuationCount = continuationIPs?.Length ?? 0;

            // Count physical frames upfront - EdiSeparators are collapsed onto the
            // preceding frame's boolean flag and don't produce output frames.
            int physicalFrameCount = 0;
            for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
            {
                if (ipAddresses[frameIndex + skipFrames] != Exception.EdiSeparator)
                    physicalFrameCount++;
            }

            int totalCapacity = physicalFrameCount + continuationCount;
            if (totalCapacity > 0)
            {
                _stackFrames = new StackFrame[totalCapacity];
                int outputFrameIndex = 0;
                bool asyncFrameSeen = false;
                bool boundaryFound = false;

                for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
                {
                    IntPtr ipAddress = ipAddresses[frameIndex + skipFrames];
                    if (ipAddress == Exception.EdiSeparator)
                    {
                        if (outputFrameIndex > 0)
                            _stackFrames[outputFrameIndex - 1].SetIsLastFrameFromForeignExceptionStackTrace();
                        continue;
                    }

                    var frame = new StackFrame(ipAddress, needFileInfo);

                    if (frame.IsAsyncMethod)
                    {
                        asyncFrameSeen = true;
                    }

                    // When inside a v2 async dispatch, the DispatchContinuations frame marks
                    // the boundary between user frames and internal dispatch machinery.
                    // Truncate there and append the continuation chain instead.
                    if (continuationIPs is not null && frame.IsAsyncDispatchBoundary())
                    {
                        for (int i = 0; i < continuationCount; i++)
                            _stackFrames[outputFrameIndex++] = new StackFrame(continuationIPs[i], needFileInfo);
                        boundaryFound = true;
                        break;
                    }

                    // Mode 1: hide all non-async frames once we've seen the first async frame.
                    if (hideAsyncDispatchMode == 1 && asyncFrameSeen && !frame.IsAsyncMethod)
                        continue;

                    _stackFrames[outputFrameIndex++] = frame;
                }

                // Fallback: if we have continuation IPs but didn't find the dispatch boundary
                // (e.g. the boundary method was inlined), append continuations after the
                // physical frames so async stitching is still performed.
                if (continuationIPs is not null && !boundaryFound)
                {
                    int needed = outputFrameIndex + continuationCount;
                    if (needed > _stackFrames.Length)
                        Array.Resize(ref _stackFrames, needed);

                    for (int i = 0; i < continuationCount; i++)
                        _stackFrames[outputFrameIndex++] = new StackFrame(continuationIPs[i], needFileInfo);
                }

                if (outputFrameIndex < totalCapacity)
                    Array.Resize(ref _stackFrames, outputFrameIndex);

                _numOfFrames = outputFrameIndex;
            }
            else
            {
                _numOfFrames = 0;
            }

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
