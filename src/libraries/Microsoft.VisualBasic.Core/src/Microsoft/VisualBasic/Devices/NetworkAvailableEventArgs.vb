' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System

Namespace Microsoft.VisualBasic.Devices
    ''' <summary>
    '''  Used to pass network connectivity status.
    ''' </summary>
    Public Class NetworkAvailableEventArgs
        Inherits EventArgs
        Public Sub New(ByVal networkAvailable As Boolean)
            IsNetworkAvailable = networkAvailable
        End Sub

        Public ReadOnly Property IsNetworkAvailable() As Boolean
    End Class
End Namespace
