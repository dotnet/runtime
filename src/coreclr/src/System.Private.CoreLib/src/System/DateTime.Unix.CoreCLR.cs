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
                return new DateTime(((ulong)(GetSystemTimeAsFileTime() + FileTimeOffset)) | KindUtc);
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern long GetSystemTimeAsFileTime();
    }
}
