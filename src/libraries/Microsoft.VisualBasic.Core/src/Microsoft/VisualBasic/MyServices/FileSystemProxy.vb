' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Option Strict On
Option Explicit On

Imports System
Imports System.ComponentModel

Namespace Microsoft.VisualBasic.MyServices

    '''******************************************************************************
    ''' ;FileSystemProxy
    ''' <summary>
    ''' An extremely thin wrapper around Microsoft.VisualBasic.FileIO.FileSystem to expose the type through My.
    ''' </summary>
    <System.ComponentModel.EditorBrowsable(EditorBrowsableState.Never)>
    Public Class FileSystemProxy

        '= PUBLIC =============================================================

        Public ReadOnly Property SpecialDirectories() As MyServices.SpecialDirectoriesProxy
            Get
                If m_SpecialDirectoriesProxy Is Nothing Then
                    m_SpecialDirectoriesProxy = New SpecialDirectoriesProxy
                End If
                Return m_SpecialDirectoriesProxy
            End Get
        End Property

        '= FRIEND =============================================================

        '''******************************************************************************
        ''' ;New
        ''' <summary>
        ''' Proxy class can only created by internal classes.
        ''' </summary>
        Friend Sub New()
        End Sub

        Private m_SpecialDirectoriesProxy As SpecialDirectoriesProxy = Nothing
    End Class
End Namespace
