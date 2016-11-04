// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data.SqlTypes
{
    internal sealed class SQLResource
    {
        private SQLResource() { /* prevent utility class from being insantiated*/ }

        internal static readonly string s_nullString = SR.SqlMisc_NullString;

        internal static readonly string s_messageString = SR.SqlMisc_MessageString;

        internal static readonly string s_arithOverflowMessage = SR.SqlMisc_ArithOverflowMessage;

        internal static readonly string s_divideByZeroMessage = SR.SqlMisc_DivideByZeroMessage;

        internal static readonly string s_nullValueMessage = SR.SqlMisc_NullValueMessage;

        internal static readonly string s_truncationMessage = SR.SqlMisc_TruncationMessage;

        internal static readonly string s_dateTimeOverflowMessage = SR.SqlMisc_DateTimeOverflowMessage;

        internal static readonly string s_concatDiffCollationMessage = SR.SqlMisc_ConcatDiffCollationMessage;

        internal static readonly string s_compareDiffCollationMessage = SR.SqlMisc_CompareDiffCollationMessage;

        internal static readonly string s_invalidFlagMessage = SR.SqlMisc_InvalidFlagMessage;

        internal static readonly string s_numeToDecOverflowMessage = SR.SqlMisc_NumeToDecOverflowMessage;

        internal static readonly string s_conversionOverflowMessage = SR.SqlMisc_ConversionOverflowMessage;

        internal static readonly string s_invalidDateTimeMessage = SR.SqlMisc_InvalidDateTimeMessage;

        internal static readonly string s_timeZoneSpecifiedMessage = SR.SqlMisc_TimeZoneSpecifiedMessage;

        internal static readonly string s_invalidArraySizeMessage = SR.SqlMisc_InvalidArraySizeMessage;

        internal static readonly string s_invalidPrecScaleMessage = SR.SqlMisc_InvalidPrecScaleMessage;

        internal static readonly string s_formatMessage = SR.SqlMisc_FormatMessage;

        internal static readonly string s_notFilledMessage = SR.SqlMisc_NotFilledMessage;

        internal static readonly string s_alreadyFilledMessage = SR.SqlMisc_AlreadyFilledMessage;

        internal static readonly string s_closedXmlReaderMessage = SR.SqlMisc_ClosedXmlReaderMessage;

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
