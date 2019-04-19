' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Globalization

Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic.CompilerServices

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public NotInheritable Class ShortType
        ' Prevent creation.
        Private Sub New()
        End Sub

        Public Shared Function FromString(ByVal Value As String) As Short

            If Value Is Nothing Then
                Return 0
            End If

            Try
                Dim i64Value As Int64

                If IsHexOrOctValue(Value, i64Value) Then
                    Return CShort(i64Value)
                End If

                Return CShort(DoubleType.Parse(Value))

            Catch e As FormatException
                Throw New InvalidCastException(GetResourceString(SR.InvalidCast_FromStringTo, Left(Value, 32), "Short"), e)
            End Try

        End Function

        Public Shared Function FromObject(ByVal Value As Object) As Short

            If Value Is Nothing Then
                Return 0S
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
                    Return CShort(ValueInterface.ToBoolean(Nothing))

                Case TypeCode.Byte
                    If TypeOf Value Is System.Byte Then
                        Return CShort(DirectCast(Value, Byte))
                    Else
                        Return CShort(ValueInterface.ToByte(Nothing))
                    End If

                Case TypeCode.Int16
                    If TypeOf Value Is System.Int16 Then
                        Return CShort(DirectCast(Value, Int16))
                    Else
                        Return CShort(ValueInterface.ToInt16(Nothing))
                    End If

                Case TypeCode.Int32
                    If TypeOf Value Is System.Int32 Then
                        Return CShort(DirectCast(Value, Int32))
                    Else
                        Return CShort(ValueInterface.ToInt32(Nothing))
                    End If

                Case TypeCode.Int64
                    If TypeOf Value Is System.Int64 Then
                        Return CShort(DirectCast(Value, Int64))
                    Else
                        Return CShort(ValueInterface.ToInt64(Nothing))
                    End If

                Case TypeCode.Single
                    If TypeOf Value Is System.Single Then
                        Return CShort(DirectCast(Value, Single))
                    Else
                        Return CShort(ValueInterface.ToSingle(Nothing))
                    End If

                Case TypeCode.Double
                    If TypeOf Value Is System.Double Then
                        Return CShort(DirectCast(Value, Double))
                    Else
                        Return CShort(ValueInterface.ToDouble(Nothing))
                    End If

                Case TypeCode.Decimal
                    'Do not use .ToDecimal because of jit temp issue effects all perf
                    Return DecimalToShort(ValueInterface)

                Case TypeCode.String
                    Return ShortType.FromString(ValueInterface.ToString(Nothing))
                Case TypeCode.Char,
                     TypeCode.DateTime
                    ' Fall through to error

                Case Else
                    ' Fall through to error
            End Select
ThrowInvalidCast:
            Throw New InvalidCastException(GetResourceString(SR.InvalidCast_FromTo, VBFriendlyName(Value), "Short"))

        End Function

        Private Shared Function DecimalToShort(ByVal ValueInterface As IConvertible) As Short
            Return CShort(ValueInterface.ToDecimal(Nothing))
        End Function

    End Class

End Namespace


