' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel
Imports Microsoft.VisualBasic.FileIO

Namespace Microsoft.VisualBasic.MyServices

    '''*************************************************************************
    ''' ;SpecialDirectoriesProxy
    ''' <summary>
    ''' An extremely thin wrapper around Microsoft.VisualBasic.FileIO.SpecialDirectories to expose the type through My.
    ''' </summary>
    <System.ComponentModel.EditorBrowsable(EditorBrowsableState.Never)>
    Public Class SpecialDirectoriesProxy

        '= PUBLIC =============================================================

        Public ReadOnly Property MyDocuments() As String
            Get
                Return SpecialDirectories.MyDocuments
            End Get
        End Property

        Public ReadOnly Property MyMusic() As String
            Get
                Return SpecialDirectories.MyMusic
            End Get
        End Property

        Public ReadOnly Property MyPictures() As String
            Get
                Return SpecialDirectories.MyPictures
            End Get
        End Property

        Public ReadOnly Property Desktop() As String
            Get
                Return SpecialDirectories.Desktop
            End Get
        End Property

        Public ReadOnly Property Programs() As String
            Get
                Return SpecialDirectories.Programs
            End Get
        End Property

        Public ReadOnly Property ProgramFiles() As String
            Get
                Return SpecialDirectories.ProgramFiles
            End Get
        End Property

        Public ReadOnly Property Temp() As String
            Get
                Return SpecialDirectories.Temp
            End Get
        End Property

        Public ReadOnly Property CurrentUserApplicationData() As String
            Get
                Return SpecialDirectories.CurrentUserApplicationData
            End Get
        End Property

        Public ReadOnly Property AllUsersApplicationData() As String
            Get
                Return SpecialDirectories.AllUsersApplicationData
            End Get
        End Property

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
