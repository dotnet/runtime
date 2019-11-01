' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Globalization

Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic.CompilerServices

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public NotInheritable Class LongType
        ' Prevent creation.
        Private Sub New()
        End Sub

        Public Shared Function FromString(ByVal Value As String) As Long

            If (Value Is Nothing) Then
                Return 0
            End If

            Try
                Dim i64Value As Int64

                If IsHexOrOctValue(Value, i64Value) Then
                    Return CLng(i64Value)
                End If

                'Using Decimal parse so that we full range of Int64
                ' and still get currency and thousands parsing
                Return CLng(DecimalType.Parse(Value, Nothing))


            Catch e As FormatException
                Throw New InvalidCastException(GetResourceString(SR.InvalidCast_FromStringTo, Left(Value, 32), "Long"), e)
            End Try

        End Function

        Public Shared Function FromObject(ByVal Value As Object) As Long

            If Value Is Nothing Then
                Return 0L
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
                    Return CLng(ValueInterface.ToBoolean(Nothing))

                Case TypeCode.Byte
                    If TypeOf Value Is System.Byte Then
                        Return CLng(DirectCast(Value, Byte))
                    Else
                        Return CLng(ValueInterface.ToByte(Nothing))
                    End If

                Case TypeCode.Int16
                    If TypeOf Value Is System.Int16 Then
                        Return CLng(DirectCast(Value, Int16))
                    Else
                        Return CLng(ValueInterface.ToInt16(Nothing))
                    End If

                Case TypeCode.Int32
                    If TypeOf Value Is System.Int32 Then
                        Return CLng(DirectCast(Value, Int32))
                    Else
                        Return CLng(ValueInterface.ToInt32(Nothing))
                    End If

                Case TypeCode.Int64
                    If TypeOf Value Is System.Int64 Then
                        Return CLng(DirectCast(Value, Int64))
                    Else
                        Return CLng(ValueInterface.ToInt64(Nothing))
                    End If

                Case TypeCode.Single
                    If TypeOf Value Is System.Single Then
                        Return CLng(DirectCast(Value, Single))
                    Else
                        Return CLng(ValueInterface.ToSingle(Nothing))
                    End If

                Case TypeCode.Double
                    If TypeOf Value Is System.Double Then
                        Return CLng(DirectCast(Value, Double))
                    Else
                        Return CLng(ValueInterface.ToDouble(Nothing))
                    End If

                Case TypeCode.Decimal
                    'Do not use .ToDecimal because of jit temp issue effects all perf
                    Return DecimalToLong(ValueInterface)

                Case TypeCode.String
                    Return LongType.FromString(ValueInterface.ToString(Nothing))
                Case TypeCode.Char,
                     TypeCode.DateTime
                    ' Fall through to error

                Case Else
                    ' Fall through to error
            End Select
ThrowInvalidCast:
            Throw New InvalidCastException(GetResourceString(SR.InvalidCast_FromTo, VBFriendlyName(Value), "Long"))
        End Function

        Private Shared Function DecimalToLong(ByVal ValueInterface As IConvertible) As Long
            Return CLng(ValueInterface.ToDecimal(Nothing))
        End Function

    End Class

End Namespace


