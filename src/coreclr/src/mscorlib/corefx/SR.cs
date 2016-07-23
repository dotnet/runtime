using System;

namespace System.Globalization
{
    internal static class SR
    {
        public static string Arg_HexStyleNotSupported
        {
            get { return Environment.GetResourceString("Arg_HexStyleNotSupported"); }
        }

        public static string Arg_InvalidHexStyle
        {
            get { return Environment.GetResourceString("Arg_InvalidHexStyle"); }
        }

        public static string ArgumentNull_Array
        {
            get { return Environment.GetResourceString("ArgumentNull_Array"); }
        }

        public static string ArgumentNull_ArrayValue
        {
            get { return Environment.GetResourceString("ArgumentNull_ArrayValue"); }
        }

        public static string ArgumentNull_Obj
        {
            get { return Environment.GetResourceString("ArgumentNull_Obj"); }
        }

        public static string ArgumentNull_String
        {
            get { return Environment.GetResourceString("ArgumentNull_String"); }
        }

        public static string ArgumentOutOfRange_AddValue
        {
            get { return Environment.GetResourceString("ArgumentOutOfRange_AddValue"); }
        }

        public static string ArgumentOutOfRange_BadHourMinuteSecond
        {
            get { return Environment.GetResourceString("ArgumentOutOfRange_BadHourMinuteSecond"); }
        }

        public static string ArgumentOutOfRange_BadYearMonthDay
        {
            get { return Environment.GetResourceString("ArgumentOutOfRange_BadYearMonthDay"); }
        }

        public static string ArgumentOutOfRange_Bounds_Lower_Upper
        {
            get { return Environment.GetResourceString("ArgumentOutOfRange_Bounds_Lower_Upper"); }
        }

        public static string ArgumentOutOfRange_CalendarRange
        {
            get { return Environment.GetResourceString("ArgumentOutOfRange_CalendarRange"); }
        }

        public static string ArgumentOutOfRange_Count
        {
            get { return Environment.GetResourceString("ArgumentOutOfRange_Count"); }
        }

        public static string ArgumentOutOfRange_Day
        {
            get { return Environment.GetResourceString("ArgumentOutOfRange_Day"); }
        }

        public static string ArgumentOutOfRange_Enum
        {
            get { return Environment.GetResourceString("ArgumentOutOfRange_Enum"); }
        }

        public static string ArgumentOutOfRange_Era
        {
            get { return Environment.GetResourceString("ArgumentOutOfRange_Era"); }
        }

        public static string ArgumentOutOfRange_Index
        {
            get { return Environment.GetResourceString("ArgumentOutOfRange_Index"); }
        }

        public static string ArgumentOutOfRange_InvalidEraValue
        {
            get { return Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"); }
        }

        public static string ArgumentOutOfRange_Month
        {
            get { return Environment.GetResourceString("ArgumentOutOfRange_Month"); }
        }

        public static string ArgumentOutOfRange_NeedNonNegNum
        {
            get { return Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"); }
        }

        public static string ArgumentOutOfRange_NeedPosNum
        {
            get { return Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"); }
        }

        public static string ArgumentOutOfRange_OffsetLength
        {
            get { return Environment.GetResourceString("ArgumentOutOfRange_OffsetLength"); }
        }

        public static string ArgumentOutOfRange_Range
        {
            get { return Environment.GetResourceString("ArgumentOutOfRange_Range"); }
        }

        public static string Argument_CompareOptionOrdinal
        {
            get { return Environment.GetResourceString("Argument_CompareOptionOrdinal"); }
        }

        public static string Argument_ConflictingDateTimeRoundtripStyles
        {
            get { return Environment.GetResourceString("Argument_ConflictingDateTimeRoundtripStyles"); }
        }

        public static string Argument_ConflictingDateTimeStyles
        {
            get { return Environment.GetResourceString("Argument_ConflictingDateTimeStyles"); }
        }

        public static string Argument_CultureInvalidIdentifier
        {
            get { return Environment.GetResourceString("Argument_CultureInvalidIdentifier"); }
        }

        public static string Argument_CultureNotSupported
        {
            get { return Environment.GetResourceString("Argument_CultureNotSupported"); }
        }
       
        public static string Argument_EmptyDecString
        {
            get { return Environment.GetResourceString("Argument_EmptyDecString"); }
        }

        public static string Argument_InvalidArrayLength
        {
            get { return Environment.GetResourceString("Argument_InvalidArrayLength"); }
        }

        public static string Argument_InvalidCalendar
        {
            get { return Environment.GetResourceString("Argument_InvalidCalendar"); }
        }

        public static string Argument_InvalidCultureName
        {
            get { return Environment.GetResourceString("Argument_InvalidCultureName"); }
        }

        public static string Argument_InvalidDateTimeStyles
        {
            get { return Environment.GetResourceString("Argument_InvalidDateTimeStyles"); }
        }

        public static string Argument_InvalidFlag
        {
            get { return Environment.GetResourceString("Argument_InvalidFlag"); }
        }

        public static string Argument_InvalidGroupSize
        {
            get { return Environment.GetResourceString("Argument_InvalidGroupSize"); }
        }

        public static string Argument_InvalidNeutralRegionName
        {
            get { return Environment.GetResourceString("Argument_InvalidNeutralRegionName"); }
        }

        public static string Argument_InvalidNumberStyles
        {
            get { return Environment.GetResourceString("Argument_InvalidNumberStyles"); }
        }

        public static string Argument_InvalidResourceCultureName
        {
            get { return Environment.GetResourceString("Argument_InvalidResourceCultureName"); }
        }

        public static string Argument_NoEra
        {
            get { return Environment.GetResourceString("Argument_NoEra"); }
        }

        public static string Argument_NoRegionInvariantCulture
        {
            get { return Environment.GetResourceString("Argument_NoRegionInvariantCulture"); }
        }

        public static string Argument_ResultCalendarRange
        {
            get { return Environment.GetResourceString("Argument_ResultCalendarRange"); }
        }

        public static string Format_BadFormatSpecifier
        {
            get { return Environment.GetResourceString("Format_BadFormatSpecifier"); }
        }

        public static string InvalidOperation_DateTimeParsing
        {
            get { return Environment.GetResourceString("InvalidOperation_DateTimeParsing"); }
        }

        public static string InvalidOperation_EnumEnded
        {
            get { return Environment.GetResourceString("InvalidOperation_EnumEnded"); }
        }

        public static string InvalidOperation_EnumNotStarted
        {
            get { return Environment.GetResourceString("InvalidOperation_EnumNotStarted"); }
        }

        public static string InvalidOperation_ReadOnly
        {
            get { return Environment.GetResourceString("InvalidOperation_ReadOnly"); }
        }

        public static string Overflow_TimeSpanTooLong
        {
            get { return Environment.GetResourceString("Overflow_TimeSpanTooLong"); }
        }

        public static string Serialization_MemberOutOfRange
        {
            get { return Environment.GetResourceString("Serialization_MemberOutOfRange"); }
        }

        public static string Format(string formatString, params object[] args)
        {
            return string.Format(CultureInfo.CurrentCulture, formatString, args);
        }
    }
}
