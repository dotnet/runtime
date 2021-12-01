// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

namespace System.Net
{
    internal sealed partial class NetEventSource : EventSource
    {
        /// <summary>Logs the contents of a buffer </summary>
        /// <param name="thisOrContextObject">`this`, or another object that serves to provide context for the operation.</param>
        /// <param name="buffer">The buffer to be logged.</param>
        /// <param name="memberName">The calling member.</param>
        [NonEvent]
        public static void DumpBuffer(object thisOrContextObject, ReadOnlySpan<byte> buffer, [CallerMemberName] string? memberName = null)
        {
            if (Log.IsEnabled())
            {
                int count = Math.Min(buffer.Length, MaxDumpSize);

                byte[] slice = buffer.Slice(0, count).ToArray();
                Log.DumpBuffer(IdOf(thisOrContextObject), memberName, slice);
            }
        }
    }
}
