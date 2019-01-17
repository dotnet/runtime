' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Globalization
Imports Microsoft.VisualBasic.CompilerServices.DecimalType
Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic.CompilerServices

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public NotInheritable Class DoubleType
        ' Prevent creation.
        Private Sub New()
        End Sub

        Public Shared Function FromString(ByVal Value As String) As Double
            Return FromString(Value, Nothing)
        End Function

        Public Shared Function FromString(ByVal Value As String, ByVal NumberFormat As NumberFormatInfo) As Double

            If Value Is Nothing Then
                Return 0
            End If

            Try
                Dim i64Value As Int64

                If IsHexOrOctValue(Value, i64Value) Then
                    Return CDbl(i64Value)
                End If
                Return DoubleType.Parse(Value, NumberFormat)

            Catch e As FormatException
                Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromStringTo, Left(Value, 32), "Double"), e)
            End Try

        End Function

        Public Shared Function FromObject(ByVal Value As Object) As Double
            Return FromObject(Value, Nothing)
        End Function

        Public Shared Function FromObject(ByVal Value As Object, ByVal NumberFormat As NumberFormatInfo) As Double

            If Value Is Nothing Then
                Return 0
            End If

            Dim valueInterface As IConvertible
            Dim valueTypeCode As TypeCode

            valueInterface = TryCast(Value, IConvertible)

            If valueInterface Is Nothing Then
                GoTo ThrowInvalidCast
            End If

            valueTypeCode = valueInterface.GetTypeCode()

            Select Case valueTypeCode

                Case TypeCode.Boolean
                    Return CDbl(valueInterface.ToBoolean(Nothing))

                Case TypeCode.Byte
                    If TypeOf Value Is System.Byte Then
                        Return CDbl(DirectCast(Value, Byte))
                    Else
                        Return CDbl(valueInterface.ToByte(Nothing))
                    End If

                Case TypeCode.Int16
                    If TypeOf Value Is System.Int16 Then
                        Return CDbl(DirectCast(Value, Int16))
                    Else
                        Return CDbl(valueInterface.ToInt16(Nothing))
                    End If

                Case TypeCode.Int32
                    If TypeOf Value Is System.Int32 Then
                        Return CDbl(DirectCast(Value, Int32))
                    Else
                        Return CDbl(valueInterface.ToInt32(Nothing))
                    End If

                Case TypeCode.Int64
                    If TypeOf Value Is System.Int64 Then
                        Return CDbl(DirectCast(Value, Int64))
                    Else
                        Return CDbl(valueInterface.ToInt64(Nothing))
                    End If

                Case TypeCode.Single
                    If TypeOf Value Is System.Single Then
                        Return DirectCast(Value, Single)
                    Else
                        Return CDbl(valueInterface.ToSingle(Nothing))
                    End If

                Case TypeCode.Double
                    If TypeOf Value Is System.Double Then
                        Return CDbl(DirectCast(Value, Double))
                    Else
                        Return CDbl(valueInterface.ToDouble(Nothing))
                    End If

                Case TypeCode.Decimal
                    'Do not use .ToDecimal because of jit temp issue effects all perf
                    Return DecimalToDouble(valueInterface)

                Case TypeCode.String
                    Return DoubleType.FromString(valueInterface.ToString(Nothing), NumberFormat)

                Case TypeCode.Char,
                     TypeCode.DateTime
                    ' Fall through to error

                Case Else
                    ' Fall through to error
            End Select
ThrowInvalidCast:
            Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromTo, VBFriendlyName(Value), "Double"))

        End Function

        Private Shared Function DecimalToDouble(ByVal ValueInterface As IConvertible) As Double
            Return CDbl(ValueInterface.ToDecimal(Nothing))
        End Function

        Public Shared Function Parse(ByVal Value As String) As Double
            Return Parse(Value, Nothing)
        End Function

        Friend Shared Function TryParse(ByVal Value As String, ByRef Result As Double) As Boolean
            Dim numberFormat As NumberFormatInfo
            Dim normalizedNumberFormat As NumberFormatInfo
            Dim culture As CultureInfo = GetCultureInfo()

            numberFormat = culture.NumberFormat
            normalizedNumberFormat = GetNormalizedNumberFormat(numberFormat)

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

            ' The below code handles the 80% case efficiently and is inefficient only when the numeric and currency settings
            ' are different

            If numberFormat Is normalizedNumberFormat Then
                Return System.Double.TryParse(Value, flags, normalizedNumberFormat, Result)
            Else
                Try
                    ' Use numeric settings to parse
                    ' Note that we use Parse instead of TryParse in order to distinguish whether the conversion failed
                    ' due to FormatException or other exception like OverFlowException, etc.
                    Result = System.Double.Parse(Value, flags, normalizedNumberFormat)
                    Return True
                Catch FormatEx As FormatException
                    ' Use currency settings to parse
                    Try
                        Return System.Double.TryParse(Value, flags, numberFormat, Result)
                    Catch ex As ArgumentException
                        Return False
                    End Try
                Catch ex As StackOverflowException
                    Throw ex
                Catch ex As OutOfMemoryException
                    Throw ex
                Catch ex As System.Threading.ThreadAbortException
                    Throw ex
                Catch Ex As Exception
                    Return False
                End Try
            End If

        End Function

        Public Shared Function Parse(ByVal Value As String, ByVal NumberFormat As NumberFormatInfo) As Double
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
                Return System.Double.Parse(Value, flags, normalizedNumberFormat)
            Catch FormatEx As FormatException When Not (NumberFormat Is normalizedNumberFormat)
                ' Use currency settings to parse
                Return System.Double.Parse(Value, flags, NumberFormat)
            Catch Ex As Exception
                Throw Ex
            End Try

        End Function

    End Class

End Namespace
