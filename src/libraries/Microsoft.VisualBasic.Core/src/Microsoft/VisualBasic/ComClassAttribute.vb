' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System

Namespace Microsoft.VisualBasic
    ''' <summary>
    ''' ComClass is used by the VB compiler to mark a public class 
    ''' that will be exposed via COM interop.
    ''' </summary>
    <AttributeUsage(AttributeTargets.Class, Inherited:=False, AllowMultiple:=False)>
    Public NotInheritable Class ComClassAttribute
        Inherits Attribute

        Public Sub New()
        End Sub

        Public Sub New(ByVal _ClassID As String)
            ClassID = _ClassID
        End Sub

        Public Sub New(ByVal _ClassID As String, ByVal _InterfaceID As String)
            ClassID = _ClassID
            InterfaceID = _InterfaceID
        End Sub

        Public Sub New(ByVal _ClassID As String, ByVal _InterfaceID As String, ByVal _EventId As String)
            ClassID = _ClassID
            InterfaceID = _InterfaceID
            EventID = _EventId
        End Sub

        Public ReadOnly Property ClassID() As String

        Public ReadOnly Property InterfaceID() As String

        Public ReadOnly Property EventID() As String

        Public Property InterfaceShadows() As Boolean
    End Class
End Namespace
