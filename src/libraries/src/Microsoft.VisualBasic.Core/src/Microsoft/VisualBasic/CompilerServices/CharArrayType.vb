' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic.CompilerServices

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public NotInheritable Class CharArrayType
        ' Prevent creation.
        Private Sub New()
        End Sub

        Public Shared Function FromString(ByVal Value As String) As Char()

            If Value Is Nothing Then
                Value = ""
            End If

            Return Value.ToCharArray()

        End Function

        Public Shared Function FromObject(ByVal Value As Object) As Char()

            If Value Is Nothing Then
                Return "".ToCharArray()
            End If

            Dim CharArray As Char() = TryCast(Value, Char())

            If CharArray IsNot Nothing AndAlso CharArray.Rank = 1 Then
                Return CharArray

            Else
                Dim ValueInterface As IConvertible
                ValueInterface = TryCast(Value, IConvertible)

                If Not ValueInterface Is Nothing Then
                    If (ValueInterface.GetTypeCode() = TypeCode.String) Then
                        Return ValueInterface.ToString(Nothing).ToCharArray()
                    End If
                End If

            End If

            Throw New InvalidCastException(GetResourceString(SR.InvalidCast_FromTo, VBFriendlyName(Value), "Char()"))

        End Function

    End Class

End Namespace


