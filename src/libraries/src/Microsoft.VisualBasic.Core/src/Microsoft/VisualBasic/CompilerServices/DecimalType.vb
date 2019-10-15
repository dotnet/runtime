' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Globalization
Imports Microsoft.VisualBasic.CompilerServices.ExceptionUtils
Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic.CompilerServices

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public NotInheritable Class DecimalType
        ' Prevent creation.
        Private Sub New()
        End Sub

        Public Shared Function FromBoolean(ByVal Value As Boolean) As Decimal
            If Value Then
                Return -1D
            Else
                Return 0D
            End If
        End Function

        Public Shared Function FromString(ByVal Value As String) As Decimal
            Return FromString(Value, Nothing)
        End Function

        Public Shared Function FromString(ByVal Value As String, ByVal NumberFormat As NumberFormatInfo) As Decimal
            If Value Is Nothing Then
                Return 0D
            End If

            Try
                Dim i64Value As Int64

                If IsHexOrOctValue(Value, i64Value) Then
                    Return CDec(i64Value)
                End If

                Return Parse(Value, NumberFormat)

            Catch e1 As OverflowException
                Throw VbMakeException(vbErrors.Overflow)
            Catch e2 As FormatException
                Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromStringTo, Left(Value, 32), "Decimal"))
            End Try
        End Function

        Public Shared Function FromObject(ByVal Value As Object) As Decimal
            Return FromObject(Value, Nothing)
        End Function

        Public Shared Function FromObject(ByVal Value As Object, ByVal NumberFormat As NumberFormatInfo) As Decimal

            If Value Is Nothing Then
                Return 0D
            End If

            Dim valueInterface As IConvertible
            Dim valueTypeCode As TypeCode

            valueInterface = TryCast(Value, IConvertible)

            If Not valueInterface Is Nothing Then

                valueTypeCode = valueInterface.GetTypeCode()

                Select Case valueTypeCode

                    Case TypeCode.Boolean
                        Return DecimalType.FromBoolean(valueInterface.ToBoolean(Nothing))

                    Case TypeCode.Byte
                        Return CDec(valueInterface.ToByte(Nothing))

                    Case TypeCode.Int16
                        Return CDec(valueInterface.ToInt16(Nothing))

                    Case TypeCode.Int32
                        Return CDec(valueInterface.ToInt32(Nothing))

                    Case TypeCode.Int64
                        Return CDec(valueInterface.ToInt64(Nothing))

                    Case TypeCode.Single
                        Return CDec(valueInterface.ToSingle(Nothing))

                    Case TypeCode.Double
                        Return CDec(valueInterface.ToDouble(Nothing))

                    Case TypeCode.Decimal
                        Return valueInterface.ToDecimal(Nothing)

                    Case TypeCode.String
                        Return DecimalType.FromString(valueInterface.ToString(Nothing), NumberFormat)

                    Case TypeCode.Char,
                         TypeCode.DateTime
                        ' Fall through to error                        
                    Case Else
                        ' Fall through to error
                End Select
            End If

            Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromTo, VBFriendlyName(Value), "Decimal"))
        End Function

        Public Shared Function Parse(ByVal Value As String, ByVal NumberFormat As NumberFormatInfo) As Decimal
            Dim normalizedNumberFormat As NumberFormatInfo
            Dim culture As CultureInfo = GetCultureInfo()

            If NumberFormat Is Nothing Then
                NumberFormat = culture.NumberFormat
            End If

            ' Normalize number format settings to enable us to first use the numeric settings for both currency and number parsing
            ' compatible with VB6
            normalizedNumberFormat = GetNormalizedNumberFormat(NumberFormat)

            Const flags As NumberStyles =
                    NumberStyles.AllowDecimalPoint Or
                    NumberStyles.AllowExponent Or
                    NumberStyles.AllowLeadingSign Or
                    NumberStyles.AllowLeadingWhite Or
                    NumberStyles.AllowThousands Or
                    NumberStyles.AllowTrailingSign Or
                    NumberStyles.AllowParentheses Or
                    NumberStyles.AllowTrailingWhite Or
                    NumberStyles.AllowCurrencySymbol

            Value = ToHalfwidthNumbers(Value, culture)

            Try
                ' Use numeric settings to parse
                Return System.Decimal.Parse(Value, flags, normalizedNumberFormat)
            Catch FormatEx As FormatException When Not (NumberFormat Is normalizedNumberFormat)
                ' Use currency settings to parse
                Return System.Decimal.Parse(Value, flags, NumberFormat)
            Catch Ex As Exception
                Throw Ex
            End Try

        End Function

        '  This method returns a NumberFormat with the relevant Currency Settings to be the same as the Number Settings
        '  In - NumberFormat to be normalized - this is not changed by this Method
        '  Return - Normalized NumberFormat
        Friend Shared Function GetNormalizedNumberFormat(ByVal InNumberFormat As NumberFormatInfo) As NumberFormatInfo

            Dim outNumberFormat As NumberFormatInfo

            With InNumberFormat
                If (Not .CurrencyDecimalSeparator Is Nothing) AndAlso
                   (Not .NumberDecimalSeparator Is Nothing) AndAlso
                   (Not .CurrencyGroupSeparator Is Nothing) AndAlso
                   (Not .NumberGroupSeparator Is Nothing) AndAlso
                   (.CurrencyDecimalSeparator.Length = 1) AndAlso
                   (.NumberDecimalSeparator.Length = 1) AndAlso
                   (.CurrencyGroupSeparator.Length = 1) AndAlso
                   (.NumberGroupSeparator.Length = 1) AndAlso
                   (.CurrencyDecimalSeparator.Chars(0) = .NumberDecimalSeparator.Chars(0)) AndAlso
                   (.CurrencyGroupSeparator.Chars(0) = .NumberGroupSeparator.Chars(0)) AndAlso
                   (.CurrencyDecimalDigits = .NumberDecimalDigits) Then
                    Return InNumberFormat
                End If
            End With


            With InNumberFormat
                If (Not .CurrencyDecimalSeparator Is Nothing) AndAlso
                   (Not .NumberDecimalSeparator Is Nothing) AndAlso
                   (.CurrencyDecimalSeparator.Length = .NumberDecimalSeparator.Length) AndAlso
                   (Not .CurrencyGroupSeparator Is Nothing) AndAlso
                   (Not .NumberGroupSeparator Is Nothing) AndAlso
                   (.CurrencyGroupSeparator.Length = .NumberGroupSeparator.Length) Then

                    Dim i As Integer
                    For i = 0 To .CurrencyDecimalSeparator.Length - 1
                        If (.CurrencyDecimalSeparator.Chars(i) <> .NumberDecimalSeparator.Chars(i)) Then GoTo MisMatch
                    Next

                    For i = 0 To .CurrencyGroupSeparator.Length - 1
                        If (.CurrencyGroupSeparator.Chars(i) <> .NumberGroupSeparator.Chars(i)) Then GoTo MisMatch
                    Next

                    Return InNumberFormat
                End If
            End With

MisMatch:

            outNumberFormat = DirectCast(InNumberFormat.Clone, NumberFormatInfo)

            ' Set the Currency Settings to be the Same as the Numeric Settings
            With outNumberFormat
                .CurrencyDecimalSeparator = .NumberDecimalSeparator
                .CurrencyGroupSeparator = .NumberGroupSeparator
                .CurrencyDecimalDigits = .NumberDecimalDigits
            End With

            Return outNumberFormat
        End Function

    End Class

End Namespace
