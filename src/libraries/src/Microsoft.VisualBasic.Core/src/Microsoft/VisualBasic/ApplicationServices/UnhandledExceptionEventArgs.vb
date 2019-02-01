' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel
Imports System.Runtime.InteropServices
Imports System.Threading

Namespace Microsoft.VisualBasic.ApplicationServices
    ''' <summary>
    ''' Provides the exception encountered along with a flag on whether to abort the program
    ''' </summary>
    <EditorBrowsable(EditorBrowsableState.Advanced), ComVisible(False)> _
    Public Class UnhandledExceptionEventArgs : Inherits ThreadExceptionEventArgs
        Sub New(ByVal exitApplication As Boolean, ByVal exception As System.Exception)
            MyBase.new(exception)
            Me.ExitApplication = exitApplication
        End Sub

        ''' <summary>
        ''' Indicates whether the application should exit upon exiting the exception handler
        ''' </summary>
        Public Property ExitApplication() As Boolean
    End Class
End Namespace
