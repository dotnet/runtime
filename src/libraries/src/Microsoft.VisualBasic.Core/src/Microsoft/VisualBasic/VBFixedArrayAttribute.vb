' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System

Namespace Global.Microsoft.VisualBasic
    ' -------------------------------------------------------------------
    ' VBFixedArray is used by the runtime to determine 
    ' if the array should be written/read without the array descriptor.
    ' -------------------------------------------------------------------
    <AttributeUsage(AttributeTargets.Field, Inherited:=False, AllowMultiple:=False)>
    Public NotInheritable Class VBFixedArrayAttribute
        Inherits Attribute

        Friend FirstBound As Integer
        Friend SecondBound As Integer

        Public ReadOnly Property Bounds() As Integer()
            Get
                If Me.SecondBound = -1 Then
                    Return New Integer() {Me.FirstBound}
                Else
                    Return New Integer() {Me.FirstBound, Me.SecondBound}
                End If
            End Get
        End Property

        Public ReadOnly Property Length() As Integer
            Get
                If Me.SecondBound = -1 Then
                    Return (Me.FirstBound + 1)
                Else
                    Return (Me.FirstBound + 1) * (Me.SecondBound + 1)
                End If
            End Get
        End Property

        Public Sub New(ByVal UpperBound1 As Integer)
            If UpperBound1 < 0 Then
                Throw New ArgumentException(SR.Invalid_VBFixedArray)
            End If

            Me.FirstBound = UpperBound1
            Me.SecondBound = -1

        End Sub

        Public Sub New(ByVal UpperBound1 As Integer, ByVal UpperBound2 As Integer)
            If UpperBound1 < 0 OrElse UpperBound2 < 0 Then
                Throw New ArgumentException(SR.Invalid_VBFixedArray)
            End If

            Me.FirstBound = UpperBound1
            Me.SecondBound = UpperBound2

        End Sub
    End Class
End Namespace
