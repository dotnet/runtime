' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Globalization

Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic.CompilerServices

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public NotInheritable Class IntegerType
        ' Prevent creation.
        Private Sub New()
        End Sub

        Public Shared Function FromString(ByVal Value As String) As Integer

            If Value Is Nothing Then
                Return 0
            End If

            Try
                Dim i64Value As Int64

                If IsHexOrOctValue(Value, i64Value) Then
                    Return CInt(i64Value)
                End If

                Return CInt(DoubleType.Parse(Value))

            Catch e As FormatException
                Throw New InvalidCastException(GetResourceString(SR.InvalidCast_FromStringTo, Left(Value, 32), "Integer"), e)
            End Try

        End Function

        Public Shared Function FromObject(ByVal Value As Object) As Integer

            If Value Is Nothing Then
                Return 0
            End If

            Dim ValueInterface As IConvertible
            Dim ValueTypeCode As TypeCode

            ValueInterface = TryCast(Value, IConvertible)

            If ValueInterface Is Nothing Then
                GoTo ThrowInvalidCast
            End If

            ValueTypeCode = ValueInterface.GetTypeCode()

            Select Case ValueTypeCode

                Case TypeCode.Boolean
                    Return CInt(ValueInterface.ToBoolean(Nothing))

                Case TypeCode.Byte
                    If TypeOf Value Is System.Byte Then
                        Return CInt(DirectCast(Value, Byte))
                    Else
                        Return CInt(ValueInterface.ToByte(Nothing))
                    End If

                Case TypeCode.Int16
                    If TypeOf Value Is System.Int16 Then
                        Return CInt(DirectCast(Value, Int16))
                    Else
                        Return CInt(ValueInterface.ToInt16(Nothing))
                    End If

                Case TypeCode.Int32
                    If TypeOf Value Is System.Int32 Then
                        Return CInt(DirectCast(Value, Int32))
                    Else
                        Return CInt(ValueInterface.ToInt32(Nothing))
                    End If

                Case TypeCode.Int64
                    If TypeOf Value Is System.Int64 Then
                        Return CInt(DirectCast(Value, Int64))
                    Else
                        Return CInt(ValueInterface.ToInt64(Nothing))
                    End If

                Case TypeCode.Single
                    If TypeOf Value Is System.Single Then
                        Return CInt(DirectCast(Value, Single))
                    Else
                        Return CInt(ValueInterface.ToSingle(Nothing))
                    End If

                Case TypeCode.Double
                    If TypeOf Value Is System.Double Then
                        Return CInt(DirectCast(Value, Double))
                    Else
                        Return CInt(ValueInterface.ToDouble(Nothing))
                    End If

                Case TypeCode.Decimal
                    'Do not use .ToDecimal because of jit temp issue effects all perf
                    Return DecimalToInteger(ValueInterface)

                Case TypeCode.String
                    Return IntegerType.FromString(ValueInterface.ToString(Nothing))
                Case TypeCode.Char,
                     TypeCode.DateTime
                    ' Fall through to error

                Case Else
                    ' Fall through to error
            End Select
ThrowInvalidCast:
            Throw New InvalidCastException(GetResourceString(SR.InvalidCast_FromTo, VBFriendlyName(Value), "Integer"))
        End Function

        Private Shared Function DecimalToInteger(ByVal ValueInterface As IConvertible) As Integer
            Return CInt(ValueInterface.ToDecimal(Nothing))
        End Function

    End Class

End Namespace


