// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using static System.Runtime.CompilerServices.AsyncProfilerBufferedEventSource;

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

                    if (IsEnabled.CreateAsyncCallstackEvent(eventKeywords))
                    {
                        AsyncCallstack.EmitEvent(context, currentTimestamp, AsyncEventID.CreateAsyncCallstack, id, nextContinuation);
                    }
                }

                AsyncThreadContext.Release(context);
            }
        }

        internal static partial class ResumeAsyncContext
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                        AsyncCallstack.EmitEvent(context, currentTimestamp, id, info.NextContinuation);
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
                    if (IsEnabled.SuspendAsyncContextEvent(activeEventKeywords))
                    {
                        EmitEvent(context, currentTimestamp);
                    }

                    if (IsEnabled.SuspendAsyncCallstackEvent(activeEventKeywords))
                    {
                        AsyncCallstack.EmitEvent(context, currentTimestamp, AsyncEventID.SuspendAsyncCallstack, GetId(ref info), nextContinuation);
                    }
                }

                AsyncThreadContext.Release(context);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static ulong GetId(ref AsyncDispatcherInfo info)
            {
                if (info.CurrentTask != null)
                {
                    return (ulong)info.CurrentTask.Id;
                }
                return 0;
            }
        }

        [StackTraceHidden]
        internal static partial class ContinuationWrapper
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

            internal static long[] GetContinuationWrapperIPs()
            {
                long[] ips = new long[COUNT];
                for (int i = 0; i < COUNT; i++)
                {
                    ips[i] = Unsafe.Add(ref Unsafe.As<ContinuationWrapperTable, nint>(ref s_continuationWrappers), i);
                }
                return ips;
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

        private static partial class SyncPoint
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static unsafe void ResumeAsyncCallstacks(AsyncThreadContext context)
            {
                //Write recursivly all the resume async callstack events.
                AsyncDispatcherInfo* info = AsyncDispatcherInfo.t_current;
                if (info != null)
                {
                    ResumeRuntimeAsyncCallstacks(info, context);
                }

            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static unsafe void ResumeRuntimeAsyncCallstacks(AsyncDispatcherInfo* info, AsyncThreadContext context)
            {
                if (info != null)
                {
                    ResumeRuntimeAsyncCallstacks(info->Next, context);
                    ResumeAsyncContext.Resume(ref *info, context, ResumeAsyncContext.GetId(ref *info), Config.ActiveEventKeywords);
                }
            }
        }

        private static partial class AsyncCallstack
        {
            public struct CaptureRuntimeAsyncCallstackState
            {
                public Continuation Continuation;
                public ulong LastNativeIP;
                public byte Count;
            }

            public static bool CaptureRuntimeAsyncCallstack(Span<byte> buffer, ref int index, ref CaptureRuntimeAsyncCallstackState state)
            {
                if (index > buffer.Length)
                {
                    return false;
                }

                byte maxAsyncCallstackLength = (byte)Math.Min(byte.MaxValue, (buffer.Length - index) / ASYNC_METHOD_INFO_SIZE);
                if (maxAsyncCallstackLength == 0)
                {
                    return false;
                }

                ulong currentNativeIP = 0;
                ulong previousNativeIP = state.LastNativeIP;

                unsafe
                {
                    currentNativeIP = (ulong)state.Continuation.ResumeInfo->DiagnosticIP;
                }

                // First frame (Count == 0) is written as absolute; subsequent frames
                // (including the first frame of a continuation call after overflow)
                // are written as deltas from the previous frame.
                if (state.Count == 0)
                {
                    EventBuffer.Serializer.WriteCompressedUInt64(buffer, ref index, currentNativeIP);
                }
                else
                {
                    EventBuffer.Serializer.WriteCompressedInt64(buffer, ref index, (long)(currentNativeIP - previousNativeIP));
                }

                EventBuffer.Serializer.WriteCompressedInt32(buffer, ref index, state.Continuation.State);
                state.Count++;

                state.Continuation = state.Continuation.Next!;
                while (state.Count < maxAsyncCallstackLength && state.Continuation != null)
                {
                    previousNativeIP = currentNativeIP;

                    unsafe
                    {
                        currentNativeIP = (ulong)state.Continuation.ResumeInfo->DiagnosticIP;
                    }

                    EventBuffer.Serializer.WriteCompressedInt64(buffer, ref index, (long)(currentNativeIP - previousNativeIP));
                    EventBuffer.Serializer.WriteCompressedInt32(buffer, ref index, state.Continuation.State);

                    state.Count++;
                    state.Continuation = state.Continuation.Next!;
                }

                state.LastNativeIP = currentNativeIP;

                return state.Continuation == null || state.Count == byte.MaxValue;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void EmitEvent(AsyncThreadContext context, long currentTimestamp, ulong id, Continuation? asyncCallstack)
            {
                EmitEvent(context, currentTimestamp, AsyncEventID.ResumeAsyncCallstack, id, AsyncCallstackType.Runtime, asyncCallstack);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void EmitEvent(AsyncThreadContext context, long currentTimestamp, AsyncEventID eventID, ulong id, Continuation? asyncCallstack)
            {
                EmitEvent(context, currentTimestamp, eventID, id, AsyncCallstackType.Runtime, asyncCallstack);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void EmitEvent(AsyncThreadContext context, long currentTimestamp, AsyncEventID eventID, ulong id, AsyncCallstackType type, Continuation? asyncCallstack)
            {
                EmitEvent(context, currentTimestamp, currentTimestamp - context.LastEventTimestamp, eventID, id, type, asyncCallstack);
            }

            public static void EmitEvent(AsyncThreadContext context, long currentTimestamp, long delta, AsyncEventID eventID, ulong id, AsyncCallstackType type, Continuation? asyncCallstack)
            {
                if (asyncCallstack != null)
                {
                    ref EventBuffer eventBuffer = ref context.EventBuffer;

                    // Max callstack data that can fit in the buffer after flush.
                    int maxCallstackBytes = Math.Min(
                        byte.MaxValue * ASYNC_METHOD_INFO_SIZE,
                        eventBuffer.Data.Length);

                    CaptureRuntimeAsyncCallstackState state = default;
                    state.Continuation = asyncCallstack;

                    // Callstack envelope: id (max 10 bytes compressed) + type (1) + callstackId (1) + frameCount (1)
                    const int maxEnvelopeSize = EventBuffer.Serializer.MaxCompressedUInt64Size + sizeof(byte) + sizeof(byte) + sizeof(byte);

                    int savedAsyncEventHeaderIndex = EventBuffer.Serializer.AsyncEventHeader(context, ref eventBuffer, currentTimestamp, delta, eventID, maxEnvelopeSize);
                    if (savedAsyncEventHeaderIndex >= 0)
                    {
                        EventBuffer.Serializer.CallstackHeader(ref eventBuffer, id, type, 0);

                        Span<byte> inlineEventBuffer = eventBuffer.Data.AsSpan(eventBuffer.Index);
                        int index = 0;

                        if (!CaptureRuntimeAsyncCallstack(inlineEventBuffer, ref index, ref state))
                        {
                            byte[]? rentedArray = RentArray(maxCallstackBytes);
                            if (rentedArray != null)
                            {
                                inlineEventBuffer.Slice(0, index).CopyTo(rentedArray);
                                CaptureRuntimeAsyncCallstack(rentedArray.AsSpan(0, maxCallstackBytes), ref index, ref state);

                                // Remove async event header from the event buffer before flushing.
                                EventBuffer.Serializer.RemoveAsyncEventHeader(context, savedAsyncEventHeaderIndex);
                                context.Flush();

                                // Write the callstack again.
                                savedAsyncEventHeaderIndex = EventBuffer.Serializer.AsyncEventHeader(context, ref eventBuffer, context.LastEventTimestamp, 0, eventID, maxEnvelopeSize + index);
                                if (savedAsyncEventHeaderIndex >= 0)
                                {
                                    EventBuffer.Serializer.CallstackHeader(ref eventBuffer, id, type, state.Count);
                                    EventBuffer.Serializer.CallstackData(ref eventBuffer, rentedArray, index);
                                }

                                ArrayPool<byte>.Shared.Return(rentedArray);
                            }
                            else
                            {
                                // Remove async event header from the event buffer since we can't write the callstack.
                                EventBuffer.Serializer.RemoveAsyncEventHeader(context, savedAsyncEventHeaderIndex);
                            }
                        }
                        else
                        {
                            // Patch frame count in the event buffer.
                            eventBuffer.Data[eventBuffer.Index - 1] = state.Count;
                            eventBuffer.Index += index;
                        }
                    }
                }
            }

            private static byte[]? RentArray(int minimumLength)
            {
                byte[]? rentedArray = null;
                try
                {
                    rentedArray = ArrayPool<byte>.Shared.Rent(minimumLength);
                }
                catch
                {
                    //AsyncProfiler can't throw, return null if renting fails.
                }

                return rentedArray;
            }
        }
    }
}
