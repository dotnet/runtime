' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Global.Microsoft.VisualBasic.CompilerServices
    <Global.System.Diagnostics.DebuggerNonUserCode()>
    <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)>
    Public NotInheritable Class ProjectData

        Friend m_rndSeed As Integer = &H50000I

        'm_oProject is per-Thread
        <System.ThreadStaticAttribute()> Private Shared m_oProject As ProjectData

        Private Sub New()
        End Sub
        Public Overloads Shared Sub SetProjectError(ex As Global.System.Exception)
        End Sub
        Public Overloads Shared Sub SetProjectError(ex As Global.System.Exception, lErl As Integer)
        End Sub
        Public Shared Sub ClearProjectError()
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
    End Class
End Namespace
