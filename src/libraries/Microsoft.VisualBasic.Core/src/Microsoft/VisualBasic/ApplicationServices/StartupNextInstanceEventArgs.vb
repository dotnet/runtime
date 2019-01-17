' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.ComponentModel
Imports System.Collections.ObjectModel

Namespace Microsoft.VisualBasic.ApplicationServices
    ''' <summary>
    ''' Provides context for the StartupNextInstance event.
    ''' </summary>
    <EditorBrowsable(EditorBrowsableState.Advanced)>
    Public Class StartupNextInstanceEventArgs : Inherits EventArgs

        ''' <summary>
        ''' Create a new instance of the StartupNextInstanceEventArgs.
        ''' </summary>
        ''' <param name="Args"></param>
        Public Sub New(ByVal args As ReadOnlyCollection(Of String), ByVal bringToForegroundFlag As Boolean)
            If args Is Nothing Then
                args = New ReadOnlyCollection(Of String)(Nothing)
            End If

            CommandLine = args
            BringToForeground = bringToForegroundFlag
        End Sub

        ''' <summary>
        ''' Indicates whether we will bring the application to the foreground when processing the
        ''' StartupNextInstance event.
        ''' </summary>
        Public Property BringToForeground() As Boolean

        ''' <summary>
        ''' Returns the command line sent to this application
        ''' </summary>
        ''' <remarks>I'm using Me.CommandLine so that it is consistent with my.net and to assure they 
        ''' always return the same values</remarks>
        Public ReadOnly Property CommandLine() As ReadOnlyCollection(Of String)
    End Class
End Namespace
