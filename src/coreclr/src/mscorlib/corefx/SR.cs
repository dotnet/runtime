using System;
using System.Globalization;

// CoreFX creates SR in the System namespace. While putting the CoreCLR SR adapter in the root
// may be unconventional, it allows us to keep the shared code identical.

internal static class SR
{
    public static string Arg_ArrayZeroError
    {
        get { return Environment.GetResourceString("Arg_ArrayZeroError"); }
    }

    public static string Arg_ExternalException
    {
        get { return Environment.GetResourceString("Arg_ExternalException"); }
    }

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

    public static string ArgumentOutOfRange_IndexCountBuffer
    {
        get { return Environment.GetResourceString("ArgumentOutOfRange_IndexCountBuffer"); }
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

    public static string Arg_ArgumentOutOfRangeException
    {
        get { return Environment.GetResourceString("Arg_ArgumentOutOfRangeException"); }
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

    public static string Argument_CultureIetfNotSupported
    {
        get { return Environment.GetResourceString("Argument_CultureIetfNotSupported"); }
    }

    public static string Argument_CultureInvalidIdentifier
    {
        get { return Environment.GetResourceString("Argument_CultureInvalidIdentifier"); }
    }

    public static string Argument_CultureNotSupported
    {
        get { return Environment.GetResourceString("Argument_CultureNotSupported"); }
    }

    public static string Argument_CultureIsNeutral
    {
        get { return Environment.GetResourceString("Argument_CultureIsNeutral"); }
    }

    public static string Argument_CustomCultureCannotBePassedByNumber
    {
        get { return Environment.GetResourceString("Argument_CustomCultureCannotBePassedByNumber"); }
    }
       
    public static string Argument_EmptyDecString
    {
        get { return Environment.GetResourceString("Argument_EmptyDecString"); }
    }

    public static string Argument_IdnBadLabelSize
    {
        get { return Environment.GetResourceString("Argument_IdnBadLabelSize"); }
    }

    public static string Argument_IdnBadPunycode
    {
        get { return Environment.GetResourceString("Argument_IdnBadPunycode"); }
    }

    public static string Argument_IdnIllegalName
    {
        get { return Environment.GetResourceString("Argument_IdnIllegalName"); }
    }

    public static string Argument_InvalidArrayLength
    {
        get { return Environment.GetResourceString("Argument_InvalidArrayLength"); }
    }

    public static string Argument_InvalidCalendar
    {
        get { return Environment.GetResourceString("Argument_InvalidCalendar"); }
    }

    public static string Argument_InvalidCharSequence
    {
        get { return Environment.GetResourceString("Argument_InvalidCharSequence"); }
    }

    public static string Argument_InvalidCultureName
    {
        get { return Environment.GetResourceString("Argument_InvalidCultureName"); }
    }

    public static string Argument_InvalidDateTimeStyles
    {
        get { return Environment.GetResourceString("Argument_InvalidDateTimeStyles"); }
    }

    public static string Argument_InvalidDigitSubstitution
    {
        get { return Environment.GetResourceString("Argument_InvalidDigitSubstitution"); }
    }

    public static string Argument_InvalidFlag
    {
        get { return Environment.GetResourceString("Argument_InvalidFlag"); }
    }

    public static string Argument_InvalidGroupSize
    {
        get { return Environment.GetResourceString("Argument_InvalidGroupSize"); }
    }

    public static string Argument_InvalidNativeDigitCount
    {
        get { return Environment.GetResourceString("Argument_InvalidNativeDigitCount"); }
    }

    public static string Argument_InvalidNativeDigitValue
    {
        get { return Environment.GetResourceString("Argument_InvalidNativeDigitValue"); }
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

    public static string Argument_OneOfCulturesNotSupported
    {
        get { return Environment.GetResourceString("Argument_OneOfCulturesNotSupported"); }
    }

    public static string Argument_OnlyMscorlib
    {
        get { return Environment.GetResourceString("Argument_OnlyMscorlib"); }
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

    public static string Arg_InvalidHandle
    {
        get { return Environment.GetResourceString("Arg_InvalidHandle"); }
    }

    public static string ObjectDisposed_FileClosed
    {
        get { return Environment.GetResourceString("ObjectDisposed_FileClosed"); }
    }

    public static string Arg_HandleNotAsync
    {
        get { return Environment.GetResourceString("Arg_HandleNotAsync"); }
    }

    public static string ArgumentNull_Path
    {
        get { return Environment.GetResourceString("ArgumentNull_Path"); }
    }

    public static string Argument_EmptyPath
    {
        get { return Environment.GetResourceString("Argument_EmptyPath"); }
    }

    public static string Argument_InvalidFileModeAndAccessCombo
    {
        get { return Environment.GetResourceString("Argument_InvalidFileMode&AccessCombo"); }
    }

    public static string Argument_InvalidAppendMode
    {
        get { return Environment.GetResourceString("Argument_InvalidAppendMode"); }
    }

    public static string ArgumentNull_Buffer
    {
        get { return Environment.GetResourceString("ArgumentNull_Buffer"); }
    }

    public static string Argument_InvalidOffLen
    {
        get { return Environment.GetResourceString("Argument_InvalidOffLen"); }
    }

    public static string IO_UnknownFileName
    {
        get { return Environment.GetResourceString("IO_UnknownFileName"); }
    }

    public static string IO_FileStreamHandlePosition
    {
        get { return Environment.GetResourceString("IO.IO_FileStreamHandlePosition"); }
    }

    public static string NotSupported_FileStreamOnNonFiles
    {
        get { return Environment.GetResourceString("NotSupported_FileStreamOnNonFiles"); }
    }

    public static string IO_BindHandleFailed
    {
        get { return Environment.GetResourceString("IO.IO_BindHandleFailed"); }
    }

    public static string Arg_HandleNotSync
    {
        get { return Environment.GetResourceString("Arg_HandleNotSync"); }
    }

    public static string IO_SetLengthAppendTruncate
    {
        get { return Environment.GetResourceString("IO.IO_SetLengthAppendTruncate"); }
    }

    public static string ArgumentOutOfRange_FileLengthTooBig
    {
        get { return Environment.GetResourceString("ArgumentOutOfRange_FileLengthTooBig"); }
    }

    public static string Argument_InvalidSeekOrigin
    {
        get { return Environment.GetResourceString("Argument_InvalidSeekOrigin"); }
    }

    public static string IO_SeekAppendOverwrite
    {
        get { return Environment.GetResourceString("IO.IO_SeekAppendOverwrite"); }
    }

    public static string IO_FileTooLongOrHandleNotSync
    {
        get { return Environment.GetResourceString("IO_FileTooLongOrHandleNotSync"); }
    }

    public static string IndexOutOfRange_IORaceCondition
    {
        get { return Environment.GetResourceString("IndexOutOfRange_IORaceCondition"); }
    }

    public static string IO_FileNotFound
    {
        get { return Environment.GetResourceString("IO.FileNotFound"); }
    }

    public static string IO_FileNotFound_FileName
    {
        get { return Environment.GetResourceString("IO.FileNotFound_FileName"); }
    }

    public static string IO_PathNotFound_NoPathName
    {
        get { return Environment.GetResourceString("IO.PathNotFound_NoPathName"); }
    }

    public static string IO_PathNotFound_Path
    {
        get { return Environment.GetResourceString("IO.PathNotFound_Path"); }
    }

    public static string UnauthorizedAccess_IODenied_NoPathName
    {
        get { return Environment.GetResourceString("UnauthorizedAccess_IODenied_NoPathName"); }
    }

    public static string UnauthorizedAccess_IODenied_Path
    {
        get { return Environment.GetResourceString("UnauthorizedAccess_IODenied_Path"); }
    }

    public static string IO_AlreadyExists_Name
    {
        get { return Environment.GetResourceString("IO.IO_AlreadyExists_Name"); }
    }

    public static string IO_PathTooLong
    {
        get { return Environment.GetResourceString("IO.PathTooLong"); }
    }

    public static string IO_SharingViolation_NoFileName
    {
        get { return Environment.GetResourceString("IO.IO_SharingViolation_NoFileName"); }
    }

    public static string IO_SharingViolation_File
    {
        get { return Environment.GetResourceString("IO.IO_SharingViolation_File"); }
    }

    public static string IO_FileExists_Name
    {
        get { return Environment.GetResourceString("IO.IO_FileExists_Name"); }
    }

    public static string NotSupported_UnwritableStream
    {
        get { return Environment.GetResourceString("NotSupported_UnwritableStream"); }
    }

    public static string NotSupported_UnreadableStream
    {
        get { return Environment.GetResourceString("NotSupported_UnreadableStream"); }
    }

    public static string NotSupported_UnseekableStream
    {
        get { return Environment.GetResourceString("NotSupported_UnseekableStream"); }
    }

    public static string IO_EOF_ReadBeyondEOF
    {
        get { return Environment.GetResourceString("IO.EOF_ReadBeyondEOF"); }
    }

    public static string Argument_InvalidHandle
    {
        get { return Environment.GetResourceString("Argument_InvalidHandle"); }
    }

    public static string Argument_AlreadyBoundOrSyncHandle
    {
        get { return Environment.GetResourceString("Argument_AlreadyBoundOrSyncHandle"); }
    }

    public static string Argument_PreAllocatedAlreadyAllocated
    {
        get { return Environment.GetResourceString("Argument_PreAllocatedAlreadyAllocated"); }
    }

    public static string Argument_NativeOverlappedAlreadyFree
    {
        get { return Environment.GetResourceString("Argument_NativeOverlappedAlreadyFree"); }
    }

    public static string Argument_NativeOverlappedWrongBoundHandle
    {
        get { return Environment.GetResourceString("Argument_NativeOverlappedWrongBoundHandle"); }
    }

    public static string InvalidOperation_NativeOverlappedReused
    {
        get { return Environment.GetResourceString("InvalidOperation_NativeOverlappedReused"); }
    }
        
    public static string ArgumentOutOfRange_Length
    {
        get { return Environment.GetResourceString("ArgumentOutOfRange_Length"); }
    }

    public static string ArgumentOutOfRange_IndexString 
    {
        get { return Environment.GetResourceString("ArgumentOutOfRange_IndexString"); }
    }

    public static string ArgumentOutOfRange_Capacity 
    {
        get { return Environment.GetResourceString("ArgumentOutOfRange_Capacity"); }
    }

    public static string Arg_CryptographyException 
    {
        get { return Environment.GetResourceString("Arg_CryptographyException"); }
    }

    public static string ArgumentException_BufferNotFromPool
    {
        get { return Environment.GetResourceString("ArgumentException_BufferNotFromPool"); }
    }

    public static string Argument_InvalidPathChars
    {
        get { return Environment.GetResourceString("Argument_InvalidPathChars"); }
    }

    public static string Argument_PathFormatNotSupported
    {
        get { return Environment.GetResourceString("Argument_PathFormatNotSupported"); }
    }

    public static string Arg_PathIllegal
    {
        get { return Environment.GetResourceString("Arg_PathIllegal"); }
    }

    public static string Arg_PathIllegalUNC
    {
        get { return Environment.GetResourceString("Arg_PathIllegalUNC"); }
    }

    public static string Arg_InvalidSearchPattern
    {
        get { return Environment.GetResourceString("Arg_InvalidSearchPattern"); }
    }

    public static string InvalidOperation_Cryptography
    {
        get { return Environment.GetResourceString("InvalidOperation_Cryptography"); }
    }

    public static string Format(string formatString, params object[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, formatString, args);
    }

    internal static string ArgumentException_ValueTupleIncorrectType
    {
        get { return Environment.GetResourceString("ArgumentException_ValueTupleIncorrectType"); }
    }

    internal static string ArgumentException_ValueTupleLastArgumentNotATuple
    {
        get { return Environment.GetResourceString("ArgumentException_ValueTupleLastArgumentNotATuple"); }
    }
}
