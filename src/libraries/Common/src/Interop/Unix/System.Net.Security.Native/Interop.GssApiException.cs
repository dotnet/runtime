// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class NetSecurityNative
    {
        internal sealed class GssApiException : Exception
        {
            private readonly Status _minorStatus;

            public Status MinorStatus
            {
                get { return _minorStatus;}
            }

            public GssApiException(string message) : base(message)
            {
            }

            public GssApiException(Status majorStatus, Status minorStatus)
                : base(GetGssApiDisplayStatus(majorStatus, minorStatus))
            {
                HResult = (int)majorStatus;
                _minorStatus = minorStatus;
            }

            private static string GetGssApiDisplayStatus(Status majorStatus, Status minorStatus)
            {
                string majorError = GetGssApiDisplayStatus(majorStatus, isMinor: false);
                string minorError = GetGssApiDisplayStatus(minorStatus, isMinor: true);

                return (majorError != null && minorError != null) ?
                    SR.Format(SR.net_gssapi_operation_failed_detailed, majorError, minorError) :
                    SR.Format(SR.net_gssapi_operation_failed, majorStatus.ToString("x"), minorStatus.ToString("x"));
            }

            private static string GetGssApiDisplayStatus(Status status, bool isMinor)
            {
                GssBuffer displayBuffer = default(GssBuffer);

                try
                {
                    Interop.NetSecurityNative.Status minStat;
                    Interop.NetSecurityNative.Status displayCallStatus = isMinor ?
                        DisplayMinorStatus(out minStat, status, ref displayBuffer):
                        DisplayMajorStatus(out minStat, status, ref displayBuffer);
                    return (Status.GSS_S_COMPLETE != displayCallStatus) ? null : Marshal.PtrToStringAnsi(displayBuffer._data);
                }
                finally
                {
                    displayBuffer.Dispose();
                }
            }
        }
    }
}
