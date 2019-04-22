' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel

Namespace Microsoft.VisualBasic.MyServices

    '''*************************************************************************
    ''' ;RegistryProxy
    ''' <summary>
    ''' An extremely thin wrapper around Microsoft.Win32.Registry to expose the type through My.
    ''' </summary>
    <System.ComponentModel.EditorBrowsable(EditorBrowsableState.Never)>
    Public Class RegistryProxy

        '= FRIEND =============================================================

        '''*************************************************************************
        ''' ;New
        ''' <summary>
        ''' Proxy class can only created by internal classes.
        ''' </summary>
        Friend Sub New()
        End Sub

    End Class
End Namespace

