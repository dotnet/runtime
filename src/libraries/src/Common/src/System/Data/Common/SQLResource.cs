// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.




//------------------------------------------------------------------------------




namespace System.Data.SqlTypes
{
    internal static class SQLResource
    {
        internal static string NullString => SR.SqlMisc_NullString;

        internal static string MessageString => SR.SqlMisc_MessageString;

        internal static string ArithOverflowMessage => SR.SqlMisc_ArithOverflowMessage;

        internal static string DivideByZeroMessage => SR.SqlMisc_DivideByZeroMessage;

        internal static string NullValueMessage => SR.SqlMisc_NullValueMessage;

        internal static string TruncationMessage => SR.SqlMisc_TruncationMessage;

        internal static string DateTimeOverflowMessage => SR.SqlMisc_DateTimeOverflowMessage;

        internal static string ConcatDiffCollationMessage => SR.SqlMisc_ConcatDiffCollationMessage;

        internal static string CompareDiffCollationMessage => SR.SqlMisc_CompareDiffCollationMessage;

        internal static string InvalidFlagMessage => SR.SqlMisc_InvalidFlagMessage;

        internal static string NumeToDecOverflowMessage => SR.SqlMisc_NumeToDecOverflowMessage;

        internal static string ConversionOverflowMessage => SR.SqlMisc_ConversionOverflowMessage;

        internal static string InvalidDateTimeMessage => SR.SqlMisc_InvalidDateTimeMessage;

        internal static string TimeZoneSpecifiedMessage => SR.SqlMisc_TimeZoneSpecifiedMessage;

        internal static string InvalidArraySizeMessage => SR.SqlMisc_InvalidArraySizeMessage;

        internal static string InvalidPrecScaleMessage => SR.SqlMisc_InvalidPrecScaleMessage;

        internal static string FormatMessage => SR.SqlMisc_FormatMessage;

        internal static string NotFilledMessage => SR.SqlMisc_NotFilledMessage;

        internal static string AlreadyFilledMessage => SR.SqlMisc_AlreadyFilledMessage;

        internal static string ClosedXmlReaderMessage => SR.SqlMisc_ClosedXmlReaderMessage;

        internal static string InvalidOpStreamClosed(string method)
        {
            return SR.Format(SR.SqlMisc_InvalidOpStreamClosed, method);
        }

        internal static string InvalidOpStreamNonWritable(string method)
        {
            return SR.Format(SR.SqlMisc_InvalidOpStreamNonWritable, method);
        }

        internal static string InvalidOpStreamNonReadable(string method)
        {
            return SR.Format(SR.SqlMisc_InvalidOpStreamNonReadable, method);
        }

        internal static string InvalidOpStreamNonSeekable(string method)
        {
            return SR.Format(SR.SqlMisc_InvalidOpStreamNonSeekable, method);
        }
    } // SqlResource
} // namespace System
