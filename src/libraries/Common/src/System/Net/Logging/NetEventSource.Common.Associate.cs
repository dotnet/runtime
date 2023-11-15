// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

namespace System.Net
{
    internal sealed partial class NetEventSource
    {
        private const int AssociateEventId = 3;

        /// <summary>Logs a relationship between two objects.</summary>
        /// <param name="first">The first object.</param>
        /// <param name="second">The second object.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Associate(object first, object second, [CallerMemberName] string? memberName = null) =>
            Associate(first, first, second, memberName);

        /// <summary>Logs a relationship between two objects.</summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="first">The first object.</param>
        /// <param name="second">The second object.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void Associate(object? thisOrContextObject, object first, object second, [CallerMemberName] string? memberName = null) =>
            Log.Associate(IdOf(thisOrContextObject), memberName, IdOf(first), IdOf(second));

        [Event(AssociateEventId, Level = EventLevel.Informational, Keywords = Keywords.Default, Message = "[{2}]<-->[{3}]")]
        private void Associate(string thisOrContextObject, string? memberName, string first, string second)
        {
            Debug.Assert(IsEnabled());
            WriteEvent(AssociateEventId, thisOrContextObject, memberName ?? MissingMember, first, second);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        [NonEvent]
        private unsafe void WriteEvent(int eventId, string? arg1, string? arg2, string? arg3, string? arg4)
        {
            arg1 ??= "";
            arg2 ??= "";
            arg3 ??= "";
            arg4 ??= "";

            fixed (char* string1Bytes = arg1)
            fixed (char* string2Bytes = arg2)
            fixed (char* string3Bytes = arg3)
            fixed (char* string4Bytes = arg4)
            {
                const int NumEventDatas = 4;
                EventData* descrs = stackalloc EventData[NumEventDatas];

                descrs[0] = new EventData
                {
                    DataPointer = (IntPtr)string1Bytes,
                    Size = ((arg1.Length + 1) * 2)
                };
                descrs[1] = new EventData
                {
                    DataPointer = (IntPtr)string2Bytes,
                    Size = ((arg2.Length + 1) * 2)
                };
                descrs[2] = new EventData
                {
                    DataPointer = (IntPtr)string3Bytes,
                    Size = ((arg3.Length + 1) * 2)
                };
                descrs[3] = new EventData
                {
                    DataPointer = (IntPtr)string4Bytes,
                    Size = ((arg4.Length + 1) * 2)
                };

                WriteEventCore(eventId, NumEventDatas, descrs);
            }
        }
    }
}
