' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Option Explicit On
Option Strict On

Imports System.ComponentModel

Namespace Microsoft.VisualBasic.MyServices

    '''*****************************************************************************
    ''';ClipboardProxy
    ''' <summary>
    ''' A class that wraps System.Windows.Forms.Clipboard so that
    ''' a clipboard can be instanced.
    ''' </summary>
    <EditorBrowsable(EditorBrowsableState.Never)>
    Public Class ClipboardProxy

        '==PUBLIC*******************************************************************

        '''*************************************************************************
        ''';New
        ''' <summary>
        ''' Only Allows instantiation of the class
        ''' </summary>
        Friend Sub New()
        End Sub

    End Class
End Namespace
