' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel
Imports System.Collections.ObjectModel

Namespace Microsoft.VisualBasic.ApplicationServices
    ''' <summary>
    ''' Provides context for the Startup event.
    ''' </summary>
    Public Class StartupEventArgs : Inherits CancelEventArgs
        ''' <summary>
        ''' Create a new instance of the StartupEventArgs.
        ''' </summary>
        ''' <param name="Args"></param>
        Public Sub New(ByVal args As ReadOnlyCollection(Of String))
            If args Is Nothing Then
                args = New ReadOnlyCollection(Of String)(Nothing)
            End If

            CommandLine = args
        End Sub

        ''' <summary>
        ''' Returns the command line sent to this application
        ''' </summary>
        Public ReadOnly Property CommandLine() As ReadOnlyCollection(Of String)
    End Class
End Namespace
