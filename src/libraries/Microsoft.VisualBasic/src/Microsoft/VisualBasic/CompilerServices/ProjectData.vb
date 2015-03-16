' Copyright (c) Microsoft. All rights reserved.
' Licensed under the MIT license. See LICENSE file in the project root for full license information.

Namespace Global.Microsoft.VisualBasic.CompilerServices
    <Global.System.Diagnostics.DebuggerNonUserCode()>
    <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)>
    Public Class ProjectData
        Private Sub New()
        End Sub
        Public Overloads Shared Sub SetProjectError(ex As Global.System.Exception)
        End Sub
        Public Overloads Shared Sub SetProjectError(ex As Global.System.Exception, lErl As Integer)
        End Sub
        Public Shared Sub ClearProjectError()
        End Sub
    End Class
End Namespace