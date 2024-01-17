// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

namespace System.Net
{
    internal sealed partial class NetEventSource
    {
        private const int DumpArrayEventId = 4;
        private const int MaxDumpSize = 1024;

        [NonEvent]
        public static void DumpBuffer(object? thisOrContextObject, byte[] buffer, [CallerMemberName] string? memberName = null) =>
            DumpBuffer(thisOrContextObject, buffer.AsSpan(), memberName);

        /// <summary>Logs the contents of a buffer.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="buffer">The buffer to be logged.</param>
        /// <param name="offset">The starting offset from which to log.</param>
        /// <param name="count">The number of bytes to log.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void DumpBuffer(object? thisOrContextObject, byte[] buffer, int offset, int count, [CallerMemberName] string? memberName = null) =>
            DumpBuffer(thisOrContextObject, buffer.AsSpan(offset, count), memberName);

        /// <summary>Logs the contents of a buffer.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="buffer">The buffer to be logged.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void DumpBuffer(object? thisOrContextObject, ReadOnlySpan<byte> buffer, [CallerMemberName] string? memberName = null) =>
            Log.DumpBuffer(IdOf(thisOrContextObject), memberName, buffer.Slice(0, Math.Min(buffer.Length, MaxDumpSize)).ToArray());

        [Event(DumpArrayEventId, Level = EventLevel.Verbose, Keywords = Keywords.Debug)]
        private unsafe void DumpBuffer(string thisOrContextObject, string? memberName, byte[] buffer) =>
            WriteEvent(DumpArrayEventId, thisOrContextObject, memberName ?? MissingMember, buffer);

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        [NonEvent]
        private unsafe void WriteEvent(int eventId, string? arg1, string? arg2, byte[]? arg3)
        {
            arg1 ??= "";
            arg2 ??= "";
            arg3 ??= Array.Empty<byte>();

            fixed (char* arg1Ptr = arg1)
            fixed (char* arg2Ptr = arg2)
            fixed (byte* arg3Ptr = arg3)
            {
                int bufferLength = arg3.Length;
                const int NumEventDatas = 4;
                EventData* descrs = stackalloc EventData[NumEventDatas];

                descrs[0] = new EventData
                {
                    DataPointer = (IntPtr)arg1Ptr,
                    Size = (arg1.Length + 1) * sizeof(char)
                };
                descrs[1] = new EventData
                {
                    DataPointer = (IntPtr)arg2Ptr,
                    Size = (arg2.Length + 1) * sizeof(char)
                };
                descrs[2] = new EventData
                {
                    DataPointer = (IntPtr)(&bufferLength),
                    Size = 4
                };
                descrs[3] = new EventData
                {
                    DataPointer = (IntPtr)arg3Ptr,
                    Size = bufferLength
                };

                WriteEventCore(eventId, NumEventDatas, descrs);
            }
        }
    }
}
