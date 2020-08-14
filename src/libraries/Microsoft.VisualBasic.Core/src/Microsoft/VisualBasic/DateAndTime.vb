' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.

Imports System
Imports System.Globalization
Imports System.Runtime.Versioning
Imports Microsoft.VisualBasic.CompilerServices
Imports Microsoft.VisualBasic.CompilerServices.ExceptionUtils
Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic
    Public Module DateAndTime

        Private AcceptedDateFormatsDBCS() As String = {"yyyy-M-d", "y-M-d", "yyyy/M/d", "y/M/d"}
        Private AcceptedDateFormatsSBCS() As String = {"M-d-yyyy", "M-d-y", "M/d/yyyy", "M/d/y"}

        '============================================================================
        ' Date/Time Properties
        '============================================================================
        Public Property Today() As DateTime
            Get
                Return DateTime.Today
            End Get
            <SupportedOSPlatform("windows")>
            Set(ByVal Value As DateTime)
                SetDate(Value)
            End Set
        End Property

        Public ReadOnly Property Now As DateTime
            Get
                Return DateTime.Now
            End Get
        End Property

        Public Property TimeOfDay() As DateTime
            Get
                Dim Ticks As Int64 = DateTime.Now.TimeOfDay.Ticks

                'Truncate to the nearest second
                Return New DateTime(Ticks - Ticks Mod TimeSpan.TicksPerSecond)
            End Get
            <SupportedOSPlatform("windows")>
            Set(ByVal Value As DateTime)
                SetTime(Value)
            End Set
        End Property

        ' TimeString (replaces Time$)
        Public Property TimeString() As String
            'Locale agnostic, Always returns 24hr clock
            Get
                Return (New DateTime(DateTime.Now.TimeOfDay.Ticks)).ToString("HH:mm:ss", GetInvariantCultureInfo())
            End Get
            <SupportedOSPlatform("windows")>
            Set(ByVal Value As String)
                Dim dt As Date

                Try
                    dt = CompilerServices.DateType.FromString(Value, GetInvariantCultureInfo())
                Catch ex As StackOverflowException
                    Throw ex
                Catch ex As OutOfMemoryException
                    Throw ex
                Catch
                    Throw VbMakeException(New InvalidCastException(SR.Format(SR.InvalidCast_FromStringTo, Left(Value, 32), "Date")), vbErrors.IllegalFuncCall)
                End Try

                SetTime(dt)
            End Set
        End Property

        Private Function IsDBCSCulture() As Boolean
#If TARGET_WINDOWS Then
            'This function is apparently trying to determine a different default for East Asian systems.
            If System.Runtime.InteropServices.Marshal.SystemMaxDBCSCharSize = 1 Then
                Return False
            End If
            Return True
#Else
            ' Emulate IsDBCSCulture of .NET 3.5 using CultureInfo
            Dim langName As String = System.Threading.Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName
            Return String.Equals(langName, "zh", StringComparison.OrdinalIgnoreCase) OrElse _
                String.Equals(langName, "ko", StringComparison.OrdinalIgnoreCase) OrElse _
                String.Equals(langName, "ja", StringComparison.OrdinalIgnoreCase)
#End If
        End Function

        Public Property DateString() As String
            ' DateString (replaces Date$)
            'Returns yyyy-MM-dd for DBCS locale
            'Returns MM-dd-yyyy for non-DBCS locale
            Get
                If IsDBCSCulture() Then
                    Return DateTime.Today.ToString("yyyy\-MM\-dd", GetInvariantCultureInfo())
                Else
                    Return DateTime.Today.ToString("MM\-dd\-yyyy", GetInvariantCultureInfo())
                End If
            End Get
            <SupportedOSPlatform("windows")>
            Set(ByVal Value As String)
                Dim NewDate As Date

                Try
                    Dim TmpValue As String = ToHalfwidthNumbers(Value, GetCultureInfo())
                    If IsDBCSCulture() Then
                        NewDate = DateTime.ParseExact(TmpValue, AcceptedDateFormatsDBCS, GetInvariantCultureInfo(), DateTimeStyles.AllowWhiteSpaces)
                    Else
                        NewDate = DateTime.ParseExact(TmpValue, AcceptedDateFormatsSBCS, GetInvariantCultureInfo(), DateTimeStyles.AllowWhiteSpaces)
                    End If
                Catch ex As StackOverflowException
                    Throw ex
                Catch ex As OutOfMemoryException
                    Throw ex
                Catch
                    Throw VbMakeException(New InvalidCastException(SR.Format(SR.InvalidCast_FromStringTo, Left(Value, 32), "Date")), vbErrors.IllegalFuncCall)
                End Try

                SetDate(NewDate)

            End Set
        End Property

        Public ReadOnly Property Timer() As Double
            Get
                'Returns number of seconds past Midnight
                Return (System.DateTime.Now.Ticks Mod System.TimeSpan.TicksPerDay) /
                        (TimeSpan.TicksPerMillisecond * 1000)
            End Get
        End Property

        Private ReadOnly Property CurrentCalendar() As Calendar
            Get
                Return Threading.Thread.CurrentThread.CurrentCulture.Calendar
            End Get
        End Property

        '============================================================================
        ' Date manipulation functions.
        '============================================================================
        Public Function DateAdd(ByVal Interval As DateInterval,
            ByVal Number As Double,
            ByVal DateValue As DateTime) As DateTime
            Dim lNumber As Integer

            lNumber = CInt(Fix(Number))

            Select Case Interval
                Case DateInterval.Year
                    Return CurrentCalendar.AddYears(DateValue, lNumber)
                Case DateInterval.Month
                    Return CurrentCalendar.AddMonths(DateValue, lNumber)
                Case DateInterval.Day,
                     DateInterval.DayOfYear,
                     DateInterval.Weekday
                    Return DateValue.AddDays(lNumber)
                Case DateInterval.WeekOfYear
                    Return DateValue.AddDays(lNumber * 7.0#)
                Case DateInterval.Hour
                    Return DateValue.AddHours(lNumber)
                Case DateInterval.Minute
                    Return DateValue.AddMinutes(lNumber)
                Case DateInterval.Second
                    Return DateValue.AddSeconds(lNumber)
                Case DateInterval.Quarter
                    Return DateValue.AddMonths(lNumber * 3)
            End Select

            Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Interval"))
        End Function

        Public Function DateDiff(ByVal Interval As DateInterval,
            ByVal Date1 As DateTime,
            ByVal Date2 As DateTime,
            Optional ByVal DayOfWeek As FirstDayOfWeek = FirstDayOfWeek.Sunday,
            Optional ByVal WeekOfYear As FirstWeekOfYear = FirstWeekOfYear.Jan1) As Long

            Dim tm As TimeSpan
            Dim cal As Calendar

            tm = Date2.Subtract(Date1)

            Select Case Interval
                Case DateInterval.Year
                    cal = CurrentCalendar
                    Return cal.GetYear(Date2) - cal.GetYear(Date1)
                Case DateInterval.Month
                    cal = CurrentCalendar
                    Return (cal.GetYear(Date2) - cal.GetYear(Date1)) * 12 + cal.GetMonth(Date2) - cal.GetMonth(Date1)
                Case DateInterval.Day,
                     DateInterval.DayOfYear
                    Return CLng(Fix(tm.TotalDays()))
                Case DateInterval.Hour
                    Return CLng(Fix(tm.TotalHours()))
                Case DateInterval.Minute
                    Return CLng(Fix(tm.TotalMinutes()))
                Case DateInterval.Second
                    Return CLng(Fix(tm.TotalSeconds()))
                Case DateInterval.WeekOfYear
                    Date1 = Date1.AddDays(-GetDayOfWeek(Date1, DayOfWeek))
                    Date2 = Date2.AddDays(-GetDayOfWeek(Date2, DayOfWeek))
                    tm = Date2.Subtract(Date1)
                    Return CLng(Fix(tm.TotalDays())) \ 7
                Case DateInterval.Weekday
                    Return CLng(Fix(tm.TotalDays())) \ 7
                Case DateInterval.Quarter
                    cal = CurrentCalendar
                    Return (cal.GetYear(Date2) - cal.GetYear(Date1)) * 4 + (cal.GetMonth(Date2) - 1) \ 3 - (cal.GetMonth(Date1) - 1) \ 3
            End Select

            Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Interval"))
        End Function

        Private Function GetDayOfWeek(ByVal dt As Date, ByVal weekdayFirst As FirstDayOfWeek) As Integer
            If (weekdayFirst < FirstDayOfWeek.System OrElse weekdayFirst > FirstDayOfWeek.Saturday) Then
                Throw VbMakeException(vbErrors.IllegalFuncCall)
            End If

            ' If FirstWeekDay is 0, get offset from NLS.
            If (weekdayFirst = FirstDayOfWeek.System) Then
                weekdayFirst = CType(GetDateTimeFormatInfo().FirstDayOfWeek + 1, FirstDayOfWeek)
            End If

            Return (dt.DayOfWeek - weekdayFirst + 8) Mod 7 + 1
        End Function

        Public Function DatePart(ByVal Interval As DateInterval, ByVal DateValue As DateTime,
            Optional ByVal FirstDayOfWeekValue As FirstDayOfWeek = vbSunday,
            Optional ByVal FirstWeekOfYearValue As FirstWeekOfYear = vbFirstJan1) As Integer

            'Get the part asked for
            Select Case Interval
                Case DateInterval.Year
                    Return CurrentCalendar.GetYear(DateValue)
                Case DateInterval.Month
                    Return CurrentCalendar.GetMonth(DateValue)
                Case DateInterval.Day
                    Return CurrentCalendar.GetDayOfMonth(DateValue)
                Case DateInterval.Hour
                    Return CurrentCalendar.GetHour(DateValue)
                Case DateInterval.Minute
                    Return CurrentCalendar.GetMinute(DateValue)
                Case DateInterval.Second
                    Return CurrentCalendar.GetSecond(DateValue)
                Case DateInterval.Weekday
                    Return Weekday(DateValue, FirstDayOfWeekValue)
                Case DateInterval.WeekOfYear
                    Dim WeekRule As CalendarWeekRule
                    Dim Day As DayOfWeek

                    If FirstDayOfWeekValue = vbUseSystemDayOfWeek Then
                        Day = GetCultureInfo().DateTimeFormat.FirstDayOfWeek
                    Else
                        Day = CType(FirstDayOfWeekValue - 1, DayOfWeek)
                    End If

                    Select Case FirstWeekOfYearValue
                        Case vbUseSystem
                            WeekRule = GetCultureInfo().DateTimeFormat.CalendarWeekRule
                        Case vbFirstJan1
                            WeekRule = CalendarWeekRule.FirstDay
                        Case vbFirstFourDays
                            WeekRule = CalendarWeekRule.FirstFourDayWeek
                        Case vbFirstFullWeek
                            WeekRule = CalendarWeekRule.FirstFullWeek
                    End Select

                    Return CurrentCalendar.GetWeekOfYear(DateValue, WeekRule, Day)
                Case DateInterval.Quarter
                    Return ((DateValue.Month - 1) \ 3) + 1
                Case DateInterval.DayOfYear
                    Return CurrentCalendar.GetDayOfYear(DateValue)
            End Select

            Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Interval"))
        End Function

        Public Function DateAdd(ByVal Interval As String,
            ByVal Number As Double,
            ByVal DateValue As Object) As DateTime

            Dim dt1 As Date

            Try
                dt1 = CDate(DateValue)
            Catch ex As StackOverflowException
                Throw ex
            Catch ex As OutOfMemoryException
                Throw ex
            Catch
                Throw New InvalidCastException(SR.Format(SR.Argument_InvalidDateValue1, "DateValue"))
            End Try

            Return DateAdd(DateIntervalFromString(Interval), Number, dt1)
        End Function

        Public Function DateDiff(ByVal Interval As String,
            ByVal Date1 As Object,
            ByVal Date2 As Object,
            Optional ByVal DayOfWeek As FirstDayOfWeek = FirstDayOfWeek.Sunday,
            Optional ByVal WeekOfYear As FirstWeekOfYear = FirstWeekOfYear.Jan1) As Long

            Dim dt1, dt2 As Date

            Try
                dt1 = CDate(Date1)
            Catch ex As StackOverflowException
                Throw ex
            Catch ex As OutOfMemoryException
                Throw ex
            Catch
                Throw New InvalidCastException(SR.Format(SR.Argument_InvalidDateValue1, "Date1"))
            End Try
            Try
                dt2 = CDate(Date2)
            Catch ex As StackOverflowException
                Throw ex
            Catch ex As OutOfMemoryException
                Throw ex
            Catch
                Throw New InvalidCastException(SR.Format(SR.Argument_InvalidDateValue1, "Date2"))
            End Try

            Return DateDiff(DateIntervalFromString(Interval), dt1, dt2, DayOfWeek, WeekOfYear)
        End Function

        Public Function DatePart(ByVal Interval As String, ByVal DateValue As Object,
            Optional ByVal DayOfWeek As FirstDayOfWeek = FirstDayOfWeek.Sunday,
            Optional ByVal WeekOfYear As FirstWeekOfYear = FirstWeekOfYear.Jan1) As Integer

            Dim dt1 As Date

            Try
                dt1 = CDate(DateValue)
            Catch ex As StackOverflowException
                Throw ex
            Catch ex As OutOfMemoryException
                Throw ex
            Catch
                Throw New InvalidCastException(SR.Format(SR.Argument_InvalidDateValue1, "DateValue"))
            End Try

            Return DatePart(DateIntervalFromString(Interval), dt1, DayOfWeek, WeekOfYear)
        End Function

        Private Function DateIntervalFromString(ByVal Interval As String) As DateInterval
            If Interval IsNot Nothing Then
                Interval = Interval.ToUpperInvariant()
            End If

            Select Case Interval
                Case "YYYY"
                    Return DateInterval.Year
                Case "Y"
                    Return DateInterval.DayOfYear
                Case "M"
                    Return DateInterval.Month
                Case "D"
                    Return DateInterval.Day
                Case "H"
                    Return DateInterval.Hour
                Case "N"
                    Return DateInterval.Minute
                Case "S"
                    Return DateInterval.Second
                Case "WW"
                    Return DateInterval.WeekOfYear
                Case "W"
                    Return DateInterval.Weekday
                Case "Q"
                    Return DateInterval.Quarter
                Case Else
                    Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Interval"))
            End Select
        End Function

        '============================================================================
        ' Date value functions.
        '============================================================================
        Public Function DateSerial(ByVal [Year] As Integer, ByVal [Month] As Integer, ByVal [Day] As Integer) As DateTime
            'We have to handle negative months and days
            ' so we start with the year and add months and days
            Dim cal As Calendar = CurrentCalendar
            Dim Result As DateTime

            If Year < 0 Then
                Year = cal.GetYear(System.DateTime.Today) + Year
            ElseIf Year < 100 Then
                Year = cal.ToFourDigitYear(Year)
            End If

            '*** BEGIN PERFOPT ***
            '*** Gregorian Calendar perf optimization
            '*** The AddMonths/AddDays require excessive conversion to/from ticks
            '*** so we special case 
            If TypeOf cal Is GregorianCalendar Then
                If (Month >= 1 AndAlso Month <= 12) AndAlso (Day >= 1 AndAlso Day <= 28) Then
                    'Uses 28 so we don't have to use the calendar to obtain
                    ' the number of days in the month, which is the cause of the
                    ' extra overhead we are trying to avoid
                    Return New DateTime(Year, Month, Day)
                End If
            End If
            '*** END PERFOPT ***

            Try
                Result = cal.ToDateTime(Year, 1, 1, 0, 0, 0, 0)
            Catch ex As StackOverflowException
                Throw ex
            Catch ex As OutOfMemoryException
                Throw ex
            Catch
                Throw VbMakeException(New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Year")), vbErrors.IllegalFuncCall)
            End Try

            Try
                Result = cal.AddMonths(Result, Month - 1)
            Catch ex As StackOverflowException
                Throw ex
            Catch ex As OutOfMemoryException
                Throw ex
            Catch
                Throw VbMakeException(New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Month")), vbErrors.IllegalFuncCall)
            End Try

            Try
                Result = cal.AddDays(Result, Day - 1)
            Catch ex As StackOverflowException
                Throw ex
            Catch ex As OutOfMemoryException
                Throw ex
            Catch
                Throw VbMakeException(New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Day")), vbErrors.IllegalFuncCall)
            End Try

            Return Result
        End Function

        Public Function TimeSerial(ByVal Hour As Integer, ByVal Minute As Integer, ByVal Second As Integer) As DateTime
            Const SecondsInDay As Integer = (24 * 60 * 60)

            Dim TotalSeconds As Integer = (Hour * 60 * 60) + (Minute * 60) + Second

            If TotalSeconds < 0 Then
                'Wrap clock
                TotalSeconds += SecondsInDay
            End If

            Return (New DateTime(TotalSeconds * TimeSpan.TicksPerSecond))
        End Function

        Public Function DateValue(ByVal [StringDate] As String) As DateTime

            Return CDate([StringDate]).Date

        End Function

        Public Function TimeValue(ByVal [StringTime] As String) As DateTime

            Return New DateTime(CDate([StringTime]).Ticks Mod TimeSpan.TicksPerDay)

        End Function

        '============================================================================
        ' Date/time part functions.
        '============================================================================
        Public Function Year(ByVal DateValue As DateTime) As Integer
            Return CurrentCalendar.GetYear(DateValue)
        End Function

        Public Function Month(ByVal DateValue As DateTime) As Integer
            Return CurrentCalendar.GetMonth(DateValue)
        End Function

        Public Function Day(ByVal DateValue As DateTime) As Integer
            Return CurrentCalendar.GetDayOfMonth(DateValue)
        End Function

        Public Function Hour(ByVal [TimeValue] As DateTime) As Integer
            Return CurrentCalendar.GetHour([TimeValue])
        End Function

        Public Function Minute(ByVal [TimeValue] As DateTime) As Integer
            Return CurrentCalendar.GetMinute([TimeValue])
        End Function

        Public Function Second(ByVal [TimeValue] As DateTime) As Integer
            Return CurrentCalendar.GetSecond([TimeValue])
        End Function

        Public Function Weekday(ByVal DateValue As DateTime, Optional ByVal DayOfWeek As FirstDayOfWeek = FirstDayOfWeek.Sunday) As Integer
            If DayOfWeek = FirstDayOfWeek.System Then
                '
                DayOfWeek = CType(DateTimeFormatInfo.CurrentInfo.FirstDayOfWeek + 1, FirstDayOfWeek)

            ElseIf (DayOfWeek < FirstDayOfWeek.Sunday) OrElse (DayOfWeek > FirstDayOfWeek.Saturday) Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "DayOfWeek"))
            End If

            'Get the day from the date
            Dim iDayOfWeek As Integer

            iDayOfWeek = CurrentCalendar.GetDayOfWeek(DateValue) + 1 ' System.DateTime uses Sunday = 0 thru Satuday = 6
            Return ((iDayOfWeek - DayOfWeek + 7) Mod 7) + 1
        End Function

        '============================================================================
        ' Date name functions.
        '============================================================================

        Public Function MonthName(ByVal Month As Integer, Optional ByVal Abbreviate As Boolean = False) As String
            Dim Result As String

            If Month < 1 OrElse Month > 13 Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Month"))
            End If

            If Abbreviate Then
                Result = GetDateTimeFormatInfo().GetAbbreviatedMonthName(Month)
            Else
                Result = GetDateTimeFormatInfo().GetMonthName(Month)
            End If

            If Result.Length = 0 Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Month"))
            End If

            Return Result
        End Function

        Public Function WeekdayName(ByVal Weekday As Integer, Optional ByVal Abbreviate As Boolean = False, Optional ByVal FirstDayOfWeekValue As FirstDayOfWeek = FirstDayOfWeek.System) As String
            Dim dtfi As DateTimeFormatInfo
            Dim Result As String

            If (Weekday < 1) OrElse (Weekday > 7) Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Weekday"))
            End If

            If (FirstDayOfWeekValue < 0) OrElse (FirstDayOfWeekValue > 7) Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "FirstDayOfWeekValue"))
            End If

            dtfi = CType(GetCultureInfo().GetFormat(GetType(System.Globalization.DateTimeFormatInfo)), DateTimeFormatInfo)  'Returns a read-only object

            If FirstDayOfWeekValue = 0 Then
                FirstDayOfWeekValue = CType(CInt(dtfi.FirstDayOfWeek) + 1, FirstDayOfWeek)
            End If

            Try
                If Abbreviate Then
                    Result = dtfi.GetAbbreviatedDayName(CType((Weekday + FirstDayOfWeekValue - 2) Mod 7, System.DayOfWeek))
                Else
                    Result = dtfi.GetDayName(CType((Weekday + FirstDayOfWeekValue - 2) Mod 7, System.DayOfWeek))
                End If
            Catch ex As StackOverflowException
                Throw ex
            Catch ex As OutOfMemoryException
                Throw ex
            Catch
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Weekday"))
            End Try

            If Result.Length = 0 Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Weekday"))
            End If

            Return Result
        End Function

    End Module
End Namespace
