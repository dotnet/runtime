' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Globalization
Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic.CompilerServices

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public NotInheritable Class BooleanType
        ' Prevent creation.
        Private Sub New()
        End Sub

        Public Shared Function FromString(ByVal Value As String) As Boolean

            If Value Is Nothing Then
                'For VB6 compatibility, treat Nothing as empty string.
                Value = ""
            End If

            Try
                Dim loc As CultureInfo = GetCultureInfo()

                'Use untrimmed Value to test for 'True'/'False'
                If System.String.Compare(Value, Boolean.FalseString, True, loc) = 0 Then
                    Return False
                ElseIf System.String.Compare(Value, Boolean.TrueString, True, loc) = 0 Then
                    Return True
                End If

                Dim i64Value As Int64

                If IsHexOrOctValue(Value, i64Value) Then
                    Return CBool(i64Value)
                End If

                Return CBool(DoubleType.Parse(Value))

            Catch e As FormatException
                Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromStringTo, Left(Value, 32), "Boolean"), e)
            End Try

        End Function

        Public Shared Function FromObject(ByVal Value As Object) As Boolean

            If Value Is Nothing Then
                Return False
            End If

            Dim ValueInterface As IConvertible
            Dim ValueTypeCode As TypeCode

            ValueInterface = TryCast(Value, IConvertible)

            If Not ValueInterface Is Nothing Then

                ValueTypeCode = ValueInterface.GetTypeCode()

                Select Case ValueTypeCode

                    Case TypeCode.Boolean
                        If TypeOf Value Is Boolean Then
                            Return CBool(DirectCast(Value, Boolean))
                        Else
                            Return CBool(ValueInterface.ToBoolean(Nothing))
                        End If

                    Case TypeCode.Byte
                        'Using ToByte also handles enums
                        If TypeOf Value Is Byte Then
                            Return CBool(DirectCast(Value, Byte))
                        Else
                            Return CBool(ValueInterface.ToByte(Nothing))
                        End If

                    Case TypeCode.Int16
                        If TypeOf Value Is Int16 Then
                            Return CBool(DirectCast(Value, Int16))
                        Else
                            'Using ToInt16 also handles enums
                            Return CBool(ValueInterface.ToInt16(Nothing))
                        End If

                    Case TypeCode.Int32
                        If TypeOf Value Is Int32 Then
                            Return CBool(DirectCast(Value, Int32))
                        Else
                            'Using ToInt32 also handles enums
                            Return CBool(ValueInterface.ToInt32(Nothing))
                        End If

                    Case TypeCode.Int64
                        If TypeOf Value Is Int64 Then
                            Return CBool(DirectCast(Value, Int64))
                        Else
                            'Using ToInt64 also handles enums
                            Return CBool(ValueInterface.ToInt64(Nothing))
                        End If

                    Case TypeCode.Single
                        If TypeOf Value Is Single Then
                            Return CBool(DirectCast(Value, Single))
                        Else
                            Return CBool(ValueInterface.ToSingle(Nothing))
                        End If

                    Case TypeCode.Double
                        If TypeOf Value Is Double Then
                            Return CBool(DirectCast(Value, Double))
                        Else
                            Return CBool(ValueInterface.ToDouble(Nothing))
                        End If

                    Case TypeCode.Decimal
                        Return DecimalToBoolean(ValueInterface)

                    Case TypeCode.String
                        Dim ValueString As String = TryCast(Value, String)

                        If ValueString IsNot Nothing Then
                            Return CBool(BooleanType.FromString(ValueString))
                        Else
                            Return CBool(BooleanType.FromString(ValueInterface.ToString(Nothing)))
                        End If
                    Case TypeCode.Char,
                         TypeCode.DateTime
                        ' Fall through to error

                    Case Else
                        ' Fall through to error
                End Select
            End If

            Throw New InvalidCastException(SR.Format(SR.InvalidCast_FromTo, VBFriendlyName(Value), "Boolean"))
        End Function

        Private Shared Function DecimalToBoolean(ByVal ValueInterface As IConvertible) As Boolean
            Return CBool(ValueInterface.ToDecimal(Nothing))
        End Function

    End Class

End Namespace
