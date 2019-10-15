' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System

Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic.CompilerServices

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public NotInheritable Class CharType
        ' Prevent creation.
        Private Sub New()
        End Sub

        Public Shared Function FromString(ByVal Value As String) As Char
            If (Value Is Nothing) OrElse (Value.Length = 0) Then
                Return ControlChars.NullChar
            End If

            Return Value.Chars(0)
        End Function

        Public Shared Function FromObject(ByVal Value As Object) As Char

            If Value Is Nothing Then
                Return ChrW(0)
            End If

            Dim ValueInterface As IConvertible
            Dim ValueTypeCode As TypeCode

            ValueInterface = TryCast(Value, IConvertible)

            If Not ValueInterface Is Nothing Then

                ValueTypeCode = ValueInterface.GetTypeCode()

                Select Case ValueTypeCode
                    Case TypeCode.Char
                        Return ValueInterface.ToChar(Nothing)

                    Case TypeCode.String
                        Return CharType.FromString(ValueInterface.ToString(Nothing))

                    Case TypeCode.Boolean,
                         TypeCode.Byte,
                         TypeCode.Int16,
                         TypeCode.Int32,
                         TypeCode.Int64,
                         TypeCode.Single,
                         TypeCode.Double,
                         TypeCode.Decimal,
                         TypeCode.DateTime
                        ' Fall through to error

                    Case Else
                        ' Fall through to error
                End Select
            End If

            Throw New InvalidCastException(GetResourceString(SR.InvalidCast_FromTo, VBFriendlyName(Value), "Char"))
        End Function

    End Class

End Namespace


