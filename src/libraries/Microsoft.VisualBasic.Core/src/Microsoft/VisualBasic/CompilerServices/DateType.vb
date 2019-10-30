' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Globalization
Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic.CompilerServices

    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Public NotInheritable Class DateType
        ' Prevent creation.
        Private Sub New()
        End Sub

        Public Shared Function FromString(ByVal Value As String) As Date
            Return DateType.FromString(Value, GetCultureInfo())
        End Function

        Public Shared Function FromString(ByVal Value As String, ByVal culture As Globalization.CultureInfo) As Date
            Dim ParsedDate As System.DateTime

            If TryParse(Value, ParsedDate) Then
                Return ParsedDate
            Else
                'Truncate the string to 32 characters for the message
                Throw New InvalidCastException(GetResourceString(SR.InvalidCast_FromStringTo, Left(Value, 32), "Date"))
            End If
        End Function

        Public Shared Function FromObject(ByVal Value As Object) As Date

            If Value Is Nothing Then
                Return Nothing
            End If

            Dim ValueInterface As IConvertible
            Dim ValueTypeCode As TypeCode

            ValueInterface = TryCast(Value, IConvertible)

            If Not ValueInterface Is Nothing Then

                ValueTypeCode = ValueInterface.GetTypeCode()

                Select Case ValueTypeCode
                    Case TypeCode.DateTime
                        Return ValueInterface.ToDateTime(Nothing)

                    Case TypeCode.String
                        Return DateType.FromString(ValueInterface.ToString(Nothing), GetCultureInfo())

                    Case TypeCode.Boolean,
                         TypeCode.Byte,
                         TypeCode.Int16,
                         TypeCode.Int32,
                         TypeCode.Int64,
                         TypeCode.Single,
                         TypeCode.Double,
                         TypeCode.Decimal,
                         TypeCode.Char
                        ' Fall through to error

                    Case Else
                        ' Fall through to error
                End Select

            End If

            Throw New InvalidCastException(GetResourceString(SR.InvalidCast_FromTo, VBFriendlyName(Value), "Date"))
        End Function

        Friend Shared Function TryParse(ByVal Value As String, ByRef Result As System.DateTime) As Boolean
            Const ParseStyle As DateTimeStyles =
                        DateTimeStyles.AllowWhiteSpaces Or
                        DateTimeStyles.NoCurrentDateDefault
            Dim Culture As CultureInfo = GetCultureInfo()
            Return System.DateTime.TryParse(ToHalfwidthNumbers(Value, Culture), Culture, ParseStyle, Result)
        End Function

    End Class

End Namespace
