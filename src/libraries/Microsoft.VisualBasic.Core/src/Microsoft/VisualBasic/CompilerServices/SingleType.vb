' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Globalization

Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic.CompilerServices

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public NotInheritable Class SingleType
        ' Prevent creation.
        Private Sub New()
        End Sub

        Public Shared Function FromString(ByVal Value As String) As Single
            Return FromString(Value, Nothing)
        End Function

        Public Shared Function FromString(ByVal Value As String, ByVal NumberFormat As NumberFormatInfo) As Single

            If Value Is Nothing Then
                Return 0
            End If

            Try
                Dim i64Value As Int64

                If IsHexOrOctValue(Value, i64Value) Then
                    Return CSng(i64Value)
                End If

                Dim Result As Double = DoubleType.Parse(Value, NumberFormat)
                If (Result < System.Single.MinValue OrElse Result > System.Single.MaxValue) AndAlso
                   Not System.Double.IsInfinity(Result) Then
                    Throw New OverflowException
                End If
                Return CSng(Result)

            Catch e As FormatException
                Throw New InvalidCastException(GetResourceString(SR.InvalidCast_FromStringTo, Left(Value, 32), "Single"), e)
            End Try

        End Function

        Public Shared Function FromObject(ByVal Value As Object) As Single
            Return FromObject(Value, Nothing)
        End Function

        Public Shared Function FromObject(ByVal Value As Object, ByVal NumberFormat As NumberFormatInfo) As Single

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
                    Return CSng(ValueInterface.ToBoolean(Nothing))

                Case TypeCode.Byte
                    If TypeOf Value Is System.Byte Then
                        Return CSng(DirectCast(Value, Byte))
                    Else
                        Return CSng(ValueInterface.ToByte(Nothing))
                    End If

                Case TypeCode.Int16
                    If TypeOf Value Is System.Int16 Then
                        Return CSng(DirectCast(Value, Int16))
                    Else
                        Return CSng(ValueInterface.ToInt16(Nothing))
                    End If

                Case TypeCode.Int32
                    If TypeOf Value Is System.Int32 Then
                        Return CSng(DirectCast(Value, Int32))
                    Else
                        Return CSng(ValueInterface.ToInt32(Nothing))
                    End If

                Case TypeCode.Int64
                    If TypeOf Value Is System.Int64 Then
                        Return CSng(DirectCast(Value, Int64))
                    Else
                        Return CSng(ValueInterface.ToInt64(Nothing))
                    End If

                Case TypeCode.Single
                    If TypeOf Value Is System.Single Then
                        Return DirectCast(Value, Single)
                    Else
                        Return ValueInterface.ToSingle(Nothing)
                    End If

                Case TypeCode.Double
                    If TypeOf Value Is System.Double Then
                        Return CSng(DirectCast(Value, Double))
                    Else
                        Return CSng(ValueInterface.ToDouble(Nothing))
                    End If

                Case TypeCode.Decimal
                    'Do not use .ToDecimal because of jit temp issue effects all perf
                    Return DecimalToSingle(ValueInterface)

                Case TypeCode.String
                    Return SingleType.FromString(ValueInterface.ToString(Nothing), NumberFormat)

                Case TypeCode.Char,
                     TypeCode.DateTime
                    ' Fall through to error

                Case Else
                    ' Fall through to error
            End Select

ThrowInvalidCast:
            Throw New InvalidCastException(GetResourceString(SR.InvalidCast_FromTo, VBFriendlyName(Value), "Single"))
        End Function

        Private Shared Function DecimalToSingle(ByVal ValueInterface As IConvertible) As Single
            Return CSng(ValueInterface.ToDecimal(Nothing))
        End Function

    End Class

End Namespace


