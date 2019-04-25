// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

internal partial class Interop
{
    internal partial class Kernel32
    {
        internal struct FILE_TIME
        {
            internal uint dwLowDateTime;
            internal uint dwHighDateTime;

            internal FILE_TIME(long fileTime)
            {
                dwLowDateTime = (uint)fileTime;
                dwHighDateTime = (uint)(fileTime >> 32);
            }

            internal long ToTicks() => ((long)dwHighDateTime << 32) + dwLowDateTime;
            internal DateTime ToDateTimeUtc() => DateTime.FromFileTimeUtc(ToTicks());
            internal DateTimeOffset ToDateTimeOffset() => DateTimeOffset.FromFileTime(ToTicks());
        }
    }
}
