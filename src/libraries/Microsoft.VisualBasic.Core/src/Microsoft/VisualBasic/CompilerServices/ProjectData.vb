' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports Microsoft.VisualBasic.FileIO

Namespace Global.Microsoft.VisualBasic.CompilerServices
    <Global.System.Diagnostics.DebuggerNonUserCode()>
    <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)>
    Public NotInheritable Class ProjectData

        Friend m_Err As ErrObject
        Friend m_rndSeed As Integer = &H50000I

        'm_oProject is per-Thread
        <System.ThreadStaticAttribute()> Private Shared m_oProject As ProjectData

        Private Sub New()
        End Sub

        Friend Shared Function GetProjectData() As ProjectData
            '*************************
            '*** PERFORMANCE NOTE: ***
            '*************************
            ' m_oProject is <ThreadStatic>
            ' and is pretty expensive to access so we cache to a local
            ' to cut the number of accesses in half
            GetProjectData = m_oProject
            If GetProjectData Is Nothing Then
                GetProjectData = New ProjectData
                m_oProject = GetProjectData
            End If
        End Function

        ''' <summary>
        ''' This function is called by the compiler in response to err code, e.g. err 123
        ''' It is also called when the compiler encounters a resume that isn't preceded by an On Error command
        ''' </summary>
        ''' <param name="hr"></param>
        ''' <returns></returns>
        Public Shared Function CreateProjectError(ByVal hr As Integer) As System.Exception
            '*************************
            '*** PERFORMANCE NOTE: ***
            '*************************
            ' Err Object is <ThreadStatic> and is pretty expensive to access so we cache to a local to cut the number of accesses
            Dim ErrObj As ErrObject = Err()
            ErrObj.Clear()
            Dim ErrNumber As Integer = ErrObj.MapErrorNumber(hr)
            Return ErrObj.CreateException(hr, Utils.GetResourceString(CType(ErrNumber, vbErrors)))
        End Function

        ''' <summary>
        ''' Called by the compiler in response to falling into a catch block.
        ''' Inside the catch statement the compiler generates code to call:
        '''  ProjectData::SetProjectError(exception)  That call
        '''  in turns sets the ErrObject which is accessed via the VB Err statement.
        '''  So a VB6 programmer would typically then do something like:
        '''    if err.Number = * do something where err accesses the ErrObject that
        '''  is set by this method.
        ''' </summary>
        ''' <param name="ex"></param>
        Public Overloads Shared Sub SetProjectError(ByVal ex As Exception)
            Err.CaptureException(ex)
        End Sub

        ''' <summary>
        ''' Called by the compiler in response to falling into a catch block.
        ''' Inside the catch statement the compiler generates code to call:
        '''  ProjectData::SetProjectError(exception, lineNumber)  This call
        ''' differs from SetProjectError(ex as Exception)because it is called
        ''' when the exception is thrown from a specific line number, e.g:
        '''   123:  Throw new Exception
        '''   123:  Error x80004003
        '''  This method in turn sets the ErrObject which is accessed via the
        '''  VB "Err" statement.
        '''  So a VB6 programmer could then do something like:
        '''    if err.Number = * 
        '''    err.Erl will also be set
        '''  is set by this class.
        ''' </summary>
        ''' <param name="ex"></param>
        ''' <param name="lErl"></param>
        Public Overloads Shared Sub SetProjectError(ByVal ex As Exception, ByVal lErl As Integer)
            Err.CaptureException(ex, lErl)
        End Sub

        Public Shared Sub ClearProjectError()
            Err.Clear()
        End Sub

        Public Shared Sub EndApp()
            FileSystem.CloseAllFiles(System.Reflection.Assembly.GetCallingAssembly())
            System.Environment.Exit(0) 'System.Environment.Exit will cause finalizers to be run at shutdown
        End Sub
    End Class
End Namespace
