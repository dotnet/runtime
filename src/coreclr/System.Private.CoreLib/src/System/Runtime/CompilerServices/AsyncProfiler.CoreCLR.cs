// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using Serializer = System.Runtime.CompilerServices.AsyncProfiler.EventBuffer.Serializer;

namespace System.Runtime.CompilerServices
{
    internal static partial class AsyncProfiler
    {
        internal static partial class CreateAsyncContext
        {
            public static void Create(ulong id, Continuation nextContinuation)
            {
                Info info = default;
                AsyncThreadContext context = AsyncThreadContext.Acquire(ref info);

                SyncPoint.Check(context);

                EventKeywords eventKeywords = context.ActiveEventKeywords;
                if (IsEnabled.AnyAsyncEvents(eventKeywords))
                {
                    long currentTimestamp = Stopwatch.GetTimestamp();
                    if (IsEnabled.CreateAsyncContextEvent(eventKeywords))
                    {
                        EmitEvent(context, currentTimestamp, id);
                    }

                    if (IsEnabled.CreateAsyncCallstackEvent(eventKeywords) && nextContinuation != null)
                    {
                        AsyncCallstack.EmitEvent(context, currentTimestamp, AsyncEventID.CreateAsyncCallstack, id, nextContinuation);
                    }
                }

                AsyncThreadContext.Release(context);
            }
        }

        internal static partial class ResumeAsyncContext
        {
            public static ulong GetId(ref AsyncDispatcherInfo info)
            {
                if (info.CurrentTask != null)
                {
                    return (ulong)info.CurrentTask.Id;
                }
                return 0;
            }

            public static void Resume(ref AsyncDispatcherInfo info)
            {
                AsyncThreadContext context = AsyncThreadContext.Acquire(ref info.AsyncProfilerInfo);

                Resume(ref info, context, GetId(ref info), context.ActiveEventKeywords);

                AsyncThreadContext.Release(context);
            }

            public static void Resume(ref AsyncDispatcherInfo info, AsyncThreadContext context, ulong id, EventKeywords activeEventKeywords)
            {
                if (SyncPoint.Check(context))
                {
                    return;
                }

                if (IsEnabled.AnyAsyncEvents(activeEventKeywords))
                {
                    long currentTimestamp = Stopwatch.GetTimestamp();
                    if (IsEnabled.ResumeAsyncContextEvent(activeEventKeywords))
                    {
                        EmitEvent(context, currentTimestamp, id);
                    }

                    if (IsEnabled.ResumeAsyncCallstackEvent(activeEventKeywords))
                    {
                        byte wrapperIndex = (byte)(info.AsyncProfilerInfo.ContinuationIndex & ContinuationWrapper.COUNT_MASK);
                        AsyncCallstack.EmitEvent(context, currentTimestamp, AsyncEventID.ResumeAsyncCallstack, id, wrapperIndex, info.NextContinuation);
                    }
                }
            }
        }

        internal static partial class SuspendAsyncContext
        {
            public static void Suspend(ref AsyncDispatcherInfo info, Continuation nextContinuation)
            {
                AsyncThreadContext context = AsyncThreadContext.Acquire(ref info.AsyncProfilerInfo);

                SyncPoint.Check(context);

                EventKeywords activeEventKeywords = context.ActiveEventKeywords;
                if (IsEnabled.AnyAsyncEvents(activeEventKeywords))
                {
                    long currentTimestamp = Stopwatch.GetTimestamp();

                    if (IsEnabled.SuspendAsyncCallstackEvent(activeEventKeywords) && nextContinuation != null)
                    {
                        AsyncCallstack.EmitEvent(context, currentTimestamp, AsyncEventID.SuspendAsyncCallstack, GetId(ref info), nextContinuation);
                    }

                    if (IsEnabled.SuspendAsyncContextEvent(activeEventKeywords))
                    {
                        EmitEvent(context, currentTimestamp);
                    }
                }

                AsyncThreadContext.Release(context);
            }

            private static ulong GetId(ref AsyncDispatcherInfo info)
            {
                if (info.CurrentTask != null)
                {
                    return (ulong)info.CurrentTask.Id;
                }
                return 0;
            }
        }

        /// <summary>
        /// Provides a table of 32 functionally identical continuation wrapper methods, each with
        /// a unique native IP address. When resuming an async continuation, the profiler dispatches
        /// through the wrapper at index (ContinuationIndex &amp; COUNT_MASK), then increments the index.
        ///
        /// This creates a rotating pattern of unique return addresses on the native callstack. An OS
        /// CPU profiler (e.g., ETW, perf) captures these native IPs in its stack samples. A post-processing
        /// tool uses the wrapper name template and count (defined by the async profiler contract) to
        /// format method names, resolve them via symbol data (rundown events), and correlate
        /// native stack IPs with the async resume callstack events emitted at the same logical point.
        /// This bridges the gap between synchronous native stack samples and the asynchronous
        /// continuation chain.
        ///
        /// Every COUNT (32) continuations, a ResetAsyncContinuationWrapperIndex event is emitted
        /// so the tool knows the index has wrapped around and can correctly map subsequent samples.
        ///
        /// Each wrapper is marked [NoInlining] to guarantee a distinct native IP, and
        /// [AggressiveOptimization] to ensure stable JIT output (skip tiered compilation).
        /// </summary>
        [StackTraceHidden]
        internal static partial class ContinuationWrapper
        {
            /// <summary>
            /// Name template for the continuation wrapper methods, defined by contract.
            /// External tools format this template with the wrapper index (0..COUNT-1) to produce
            /// method names for identifying wrapper frames in stacks.
            /// Must match the actual method names below (e.g., Continuation_Wrapper_0, Continuation_Wrapper_1, ...).
            /// </summary>
            public const string NameTemplate = "Continuation_Wrapper_{0}";

            public static void InitInfo(ref Info info)
            {
                info.ContinuationTable = ref Unsafe.As<ContinuationWrapperTable, nint>(ref s_continuationWrappers);
                info.ContinuationIndex = 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Continuation? Dispatch(ref AsyncDispatcherInfo info, Continuation curContinuation, ref byte resultLoc)
            {
                nint dispatcher = Unsafe.Add(ref info.AsyncProfilerInfo.ContinuationTable, info.AsyncProfilerInfo.ContinuationIndex & COUNT_MASK);
                unsafe
                {
                    return ((delegate*<Continuation, ref byte, Continuation?>)(dispatcher))(curContinuation, ref resultLoc);
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_0(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_1(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_2(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_3(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_4(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_5(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_6(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_7(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_8(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_9(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_10(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_11(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_12(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_13(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_14(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_15(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_16(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_17(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_18(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_19(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_20(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_21(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_22(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_23(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_24(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_25(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_26(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_27(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_28(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_29(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_30(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
            private static unsafe Continuation? Continuation_Wrapper_31(Continuation continuation, ref byte resultLoc)
            {
                return continuation.ResumeInfo->Resume(continuation, ref resultLoc);
            }

            private static unsafe ContinuationWrapperTable InitContinuationWrappers()
            {
                ContinuationWrapperTable wrappers = default;
                wrappers[0] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_0;
                wrappers[1] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_1;
                wrappers[2] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_2;
                wrappers[3] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_3;
                wrappers[4] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_4;
                wrappers[5] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_5;
                wrappers[6] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_6;
                wrappers[7] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_7;
                wrappers[8] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_8;
                wrappers[9] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_9;
                wrappers[10] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_10;
                wrappers[11] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_11;
                wrappers[12] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_12;
                wrappers[13] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_13;
                wrappers[14] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_14;
                wrappers[15] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_15;
                wrappers[16] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_16;
                wrappers[17] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_17;
                wrappers[18] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_18;
                wrappers[19] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_19;
                wrappers[20] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_20;
                wrappers[21] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_21;
                wrappers[22] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_22;
                wrappers[23] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_23;
                wrappers[24] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_24;
                wrappers[25] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_25;
                wrappers[26] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_26;
                wrappers[27] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_27;
                wrappers[28] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_28;
                wrappers[29] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_29;
                wrappers[30] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_30;
                wrappers[31] = (nint)(delegate*<Continuation, ref byte, Continuation?>)&Continuation_Wrapper_31;
                return wrappers;
            }

            [InlineArray(COUNT)]
            private struct ContinuationWrapperTable
            {
                private nint _element;
            }

            private static ContinuationWrapperTable s_continuationWrappers = InitContinuationWrappers();
        }

        internal static partial class SyncPoint
        {
            private static unsafe void ResumeAsyncCallstacks(AsyncThreadContext context)
            {
                //Write recursively all the resume async callstack events for both AsyncDispatcherInfo (V2) and AsyncTaskDispatcherInfo (V1).
                //Ordered by their stack location, largest address first (assumes stack grow downwards).
                AsyncDispatcherInfo* runtimeAsyncInfo = AsyncDispatcherInfo.t_current;
                AsyncTaskDispatcherInfo* taskAsyncInfo = AsyncTaskDispatcherInfo.t_current;

                ResumeAsyncCallstacks(runtimeAsyncInfo, taskAsyncInfo, context);
            }

            private static unsafe void ResumeAsyncCallstacks(AsyncDispatcherInfo* runtimeAsyncInfo, AsyncTaskDispatcherInfo* taskAsyncInfo, AsyncThreadContext context)
            {
                if (runtimeAsyncInfo == null && taskAsyncInfo == null)
                {
                    return;
                }

                if (taskAsyncInfo == null || (runtimeAsyncInfo != null && (void*)runtimeAsyncInfo < (void*)taskAsyncInfo))
                {
                    ResumeAsyncCallstacks(runtimeAsyncInfo->Next, taskAsyncInfo, context);
                    ResumeAsyncContext.Resume(ref *runtimeAsyncInfo, context, ResumeAsyncContext.GetId(ref *runtimeAsyncInfo), Config.ActiveEventKeywords);
                }
                else
                {
                    ResumeAsyncCallstacks(runtimeAsyncInfo, taskAsyncInfo->Next, context);
                    ResumeAsyncContext.Resume(ref *taskAsyncInfo, context, ResumeAsyncContext.GetId(ref *taskAsyncInfo), Config.ActiveEventKeywords);
                }
            }
        }

        private static partial class AsyncCallstack
        {
            private const int MaxRuntimeAsyncMethodFrameSize = Serializer.MaxCompressedUInt64Size + Serializer.MaxCompressedUInt32Size;

            private ref struct CaptureRuntimeAsyncCallstackState : ICaptureAsyncCallstack
            {
                public Continuation? Continuation;
                public ulong LastNativeIP;
                public byte Count;

                public bool Capture(byte[] buffer, ref int index, out byte count)
                {
                    bool result = CaptureRuntimeAsyncCallstack(buffer, ref index, ref this);
                    count = Count;
                    return result;
                }

                public static AsyncCallstackType CallstackType => AsyncCallstackType.Runtime;
                public static int MaxAsyncMethodFrameSize => MaxRuntimeAsyncMethodFrameSize;
            }

            public static void EmitEvent(AsyncThreadContext context, long currentTimestamp, AsyncEventID eventID, ulong id, Continuation? asyncCallstack)
            {
                CaptureRuntimeAsyncCallstackState state = default;
                state.Continuation = asyncCallstack;
                EmitAsyncCallstack(context, currentTimestamp, currentTimestamp - context.LastEventTimestamp, eventID, id, 0, ref state);
            }

            public static void EmitEvent(AsyncThreadContext context, long currentTimestamp, AsyncEventID eventID, ulong id, byte continuationIndex, Continuation? asyncCallstack)
            {
                CaptureRuntimeAsyncCallstackState state = default;
                state.Continuation = asyncCallstack;
                EmitAsyncCallstack(context, currentTimestamp, currentTimestamp - context.LastEventTimestamp, eventID, id, continuationIndex, ref state);
            }

            private static bool CaptureRuntimeAsyncCallstack(byte[] buffer, ref int index, ref CaptureRuntimeAsyncCallstackState state)
            {
                if (index > buffer.Length)
                {
                    return false;
                }

                if (state.Continuation == null)
                {
                    return true;
                }

                byte maxAsyncCallstackFrames = (byte)Math.Min(byte.MaxValue, (buffer.Length - index) / MaxRuntimeAsyncMethodFrameSize);
                if (maxAsyncCallstackFrames == 0)
                {
                    return false;
                }

                Span<byte> callstackSpan = buffer.AsSpan(index);

                ulong currentNativeIP = 0;
                ulong previousNativeIP = state.LastNativeIP;

                unsafe
                {
                    currentNativeIP = (ulong)state.Continuation.ResumeInfo->DiagnosticIP;
                }

                int callstackSpanIndex = 0;

                // First frame (Count == 0) is written as absolute; subsequent frames
                // (including the first frame of a continuation call after overflow)
                // are written as deltas from the previous frame.
                if (state.Count == 0)
                {
                    callstackSpanIndex += Serializer.WriteCompressedUInt64(callstackSpan.Slice(callstackSpanIndex, Serializer.MaxCompressedUInt64Size), currentNativeIP);
                }
                else
                {
                    callstackSpanIndex += Serializer.WriteCompressedInt64(callstackSpan.Slice(callstackSpanIndex, Serializer.MaxCompressedInt64Size), (long)(currentNativeIP - previousNativeIP));
                }

                // Handrolled continuations (like RuntimeAsyncTaskContinuation) are indicated with a null DiagnosticIP. For these we always write 0 as the state since the state number is not meaningful.
                callstackSpanIndex += Serializer.WriteCompressedInt32(callstackSpan.Slice(callstackSpanIndex, Serializer.MaxCompressedInt32Size), currentNativeIP == 0 ? 0 : state.Continuation.State);
                state.Count++;

                state.Continuation = state.Continuation.Next;
                while (state.Count < maxAsyncCallstackFrames && state.Continuation != null)
                {
                    previousNativeIP = currentNativeIP;

                    unsafe
                    {
                        currentNativeIP = (ulong)state.Continuation.ResumeInfo->DiagnosticIP;
                    }

                    callstackSpanIndex += Serializer.WriteCompressedInt64(callstackSpan.Slice(callstackSpanIndex, Serializer.MaxCompressedInt64Size), (long)(currentNativeIP - previousNativeIP));
                    callstackSpanIndex += Serializer.WriteCompressedInt32(callstackSpan.Slice(callstackSpanIndex, Serializer.MaxCompressedInt32Size), currentNativeIP == 0 ? 0 : state.Continuation.State);

                    state.Count++;
                    state.Continuation = state.Continuation.Next;
                }

                state.LastNativeIP = currentNativeIP;
                index += callstackSpanIndex;

                return state.Continuation == null || state.Count == byte.MaxValue;
            }
        }
    }
}
