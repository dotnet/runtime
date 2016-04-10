// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.




//------------------------------------------------------------------------------




namespace System.Data.SqlTypes
{
    using Res = System.SR;

    internal static class SQLResource
    {
        internal static string NullString => Res.SqlMisc_NullString;

        internal static string MessageString => Res.SqlMisc_MessageString;

        internal static string ArithOverflowMessage => Res.SqlMisc_ArithOverflowMessage;

        internal static string DivideByZeroMessage => Res.SqlMisc_DivideByZeroMessage;

        internal static string NullValueMessage => Res.SqlMisc_NullValueMessage;

        internal static string TruncationMessage => Res.SqlMisc_TruncationMessage;

        internal static string DateTimeOverflowMessage => Res.SqlMisc_DateTimeOverflowMessage;

        internal static string ConcatDiffCollationMessage => Res.SqlMisc_ConcatDiffCollationMessage;

        internal static string CompareDiffCollationMessage => Res.SqlMisc_CompareDiffCollationMessage;

        internal static string InvalidFlagMessage => Res.SqlMisc_InvalidFlagMessage;

        internal static string NumeToDecOverflowMessage => Res.SqlMisc_NumeToDecOverflowMessage;

        internal static string ConversionOverflowMessage => Res.SqlMisc_ConversionOverflowMessage;

        internal static string InvalidDateTimeMessage => Res.SqlMisc_InvalidDateTimeMessage;

        internal static string TimeZoneSpecifiedMessage => Res.SqlMisc_TimeZoneSpecifiedMessage;

        internal static string InvalidArraySizeMessage => Res.SqlMisc_InvalidArraySizeMessage;

        internal static string InvalidPrecScaleMessage => Res.SqlMisc_InvalidPrecScaleMessage;

        internal static string FormatMessage => Res.SqlMisc_FormatMessage;

        internal static string NotFilledMessage => Res.SqlMisc_NotFilledMessage;

        internal static string AlreadyFilledMessage => Res.SqlMisc_AlreadyFilledMessage;

        internal static string ClosedXmlReaderMessage => Res.SqlMisc_ClosedXmlReaderMessage;

        internal static string InvalidOpStreamClosed(string method)
        {
            return Res.GetString(Res.SqlMisc_InvalidOpStreamClosed, method);
        }

        internal static string InvalidOpStreamNonWritable(string method)
        {
            return Res.GetString(Res.SqlMisc_InvalidOpStreamNonWritable, method);
        }

        internal static string InvalidOpStreamNonReadable(string method)
        {
            return Res.GetString(Res.SqlMisc_InvalidOpStreamNonReadable, method);
        }

        internal static string InvalidOpStreamNonSeekable(string method)
        {
            return Res.GetString(Res.SqlMisc_InvalidOpStreamNonSeekable, method);
        }
    } // SqlResource
} // namespace System
