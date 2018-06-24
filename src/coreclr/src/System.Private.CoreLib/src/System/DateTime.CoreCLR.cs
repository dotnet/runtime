// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System
{
    public readonly partial struct DateTime
    {
        public static DateTime UtcNow
        {
            get
            {
                // following code is tuned for speed. Don't change it without running benchmark.
                long ticks = 0;
                ticks = GetSystemTimeAsFileTime();

                return new DateTime(((ulong)(ticks + FileTimeOffset)) | KindUtc);
            }
        }


        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern long GetSystemTimeAsFileTime();
    }
}
