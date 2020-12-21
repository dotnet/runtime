// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System
{
    public readonly partial struct DateTime
    {
        public static DateTime UtcNow
        {
            get
            {
                return new DateTime(((ulong)(GetSystemTimeAsFileTime() + FileTimeOffset)) | KindUtc);
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern long GetSystemTimeAsFileTime();
    }
}
