' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System

Namespace Global.Microsoft.VisualBasic
    ' -------------------------------------------------------------------
    ' VBFixedString is used by the runtime to determine 
    ' if the field should be written/read without the string length descriptor.
    ' -------------------------------------------------------------------
    <AttributeUsage(AttributeTargets.Field, Inherited:=False, AllowMultiple:=False)> _
    Public NotInheritable Class VBFixedStringAttribute
        Inherits Attribute

        Public ReadOnly Property Length() As Integer

        Public Sub New(ByVal Length As Integer)
            If (Length < 1 OrElse Length > Short.MaxValue) Then
                Throw New ArgumentException(SR.Invalid_VBFixedString)
            End If

            Me.Length = Length
        End Sub
    End Class
End Namespace
