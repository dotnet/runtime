// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

            public Status MajorStatus
            {
                get { return (Status)HResult; }
            }

            public Status MinorStatus
            {
                get { return _minorStatus; }
            }

            public GssApiException(string message) : base(message)
            {
            }

            public GssApiException(Status majorStatus, Status minorStatus)
                : base(GetGssApiDisplayStatus(majorStatus, minorStatus, null))
            {
                HResult = (int)majorStatus;
                _minorStatus = minorStatus;
            }

            public GssApiException(Status majorStatus, Status minorStatus, string helpText)
                : base(GetGssApiDisplayStatus(majorStatus, minorStatus, helpText))
            {
                HResult = (int)majorStatus;
                _minorStatus = minorStatus;
            }

            private static string GetGssApiDisplayStatus(Status majorStatus, Status minorStatus, string? helpText)
            {
                string? majorError = GetGssApiDisplayStatus(majorStatus, isMinor: false);
                string errorMessage;

                if (minorStatus != Status.GSS_S_COMPLETE)
                {
                    string? minorError = GetGssApiDisplayStatus(minorStatus, isMinor: true);
                    errorMessage = (majorError != null && minorError != null) ?
                        SR.Format(SR.net_gssapi_operation_failed_detailed, majorError, minorError) :
                        SR.Format(SR.net_gssapi_operation_failed, majorStatus.ToString("x"), minorStatus.ToString("x"));
                }
                else
                {
                    errorMessage = (majorError != null) ?
                        SR.Format(SR.net_gssapi_operation_failed_detailed_majoronly, majorError) :
                        SR.Format(SR.net_gssapi_operation_failed_majoronly, majorStatus.ToString("x"));
                }

                if (!string.IsNullOrEmpty(helpText))
                {
                    return errorMessage + " " + helpText;
                }

                return errorMessage;
            }

            private static string? GetGssApiDisplayStatus(Status status, bool isMinor)
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
