// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.




//------------------------------------------------------------------------------




namespace System.Data.SqlTypes
{
    using Res = System.SR;

    internal sealed class SQLResource
    {
        private SQLResource() { /* prevent utility class from being insantiated*/ }

        internal static readonly String NullString = Res.GetString(Res.SqlMisc_NullString);

        internal static readonly String MessageString = Res.GetString(Res.SqlMisc_MessageString);

        internal static readonly String ArithOverflowMessage = Res.GetString(Res.SqlMisc_ArithOverflowMessage);

        internal static readonly String DivideByZeroMessage = Res.GetString(Res.SqlMisc_DivideByZeroMessage);

        internal static readonly String NullValueMessage = Res.GetString(Res.SqlMisc_NullValueMessage);

        internal static readonly String TruncationMessage = Res.GetString(Res.SqlMisc_TruncationMessage);

        internal static readonly String DateTimeOverflowMessage = Res.GetString(Res.SqlMisc_DateTimeOverflowMessage);

        internal static readonly String ConcatDiffCollationMessage = Res.GetString(Res.SqlMisc_ConcatDiffCollationMessage);

        internal static readonly String CompareDiffCollationMessage = Res.GetString(Res.SqlMisc_CompareDiffCollationMessage);

        internal static readonly String InvalidFlagMessage = Res.GetString(Res.SqlMisc_InvalidFlagMessage);

        internal static readonly String NumeToDecOverflowMessage = Res.GetString(Res.SqlMisc_NumeToDecOverflowMessage);

        internal static readonly String ConversionOverflowMessage = Res.GetString(Res.SqlMisc_ConversionOverflowMessage);

        internal static readonly String InvalidDateTimeMessage = Res.GetString(Res.SqlMisc_InvalidDateTimeMessage);

        internal static readonly String TimeZoneSpecifiedMessage = Res.GetString(Res.SqlMisc_TimeZoneSpecifiedMessage);

        internal static readonly String InvalidArraySizeMessage = Res.GetString(Res.SqlMisc_InvalidArraySizeMessage);

        internal static readonly String InvalidPrecScaleMessage = Res.GetString(Res.SqlMisc_InvalidPrecScaleMessage);

        internal static readonly String FormatMessage = Res.GetString(Res.SqlMisc_FormatMessage);

        internal static readonly String NotFilledMessage = Res.GetString(Res.SqlMisc_NotFilledMessage);

        internal static readonly String AlreadyFilledMessage = Res.GetString(Res.SqlMisc_AlreadyFilledMessage);

        internal static readonly String ClosedXmlReaderMessage = Res.GetString(Res.SqlMisc_ClosedXmlReaderMessage);

        internal static String InvalidOpStreamClosed(String method)
        {
            return Res.GetString(Res.SqlMisc_InvalidOpStreamClosed, method);
        }

        internal static String InvalidOpStreamNonWritable(String method)
        {
            return Res.GetString(Res.SqlMisc_InvalidOpStreamNonWritable, method);
        }

        internal static String InvalidOpStreamNonReadable(String method)
        {
            return Res.GetString(Res.SqlMisc_InvalidOpStreamNonReadable, method);
        }

        internal static String InvalidOpStreamNonSeekable(String method)
        {
            return Res.GetString(Res.SqlMisc_InvalidOpStreamNonSeekable, method);
        }
    } // SqlResource
} // namespace System
