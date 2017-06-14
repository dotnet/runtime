' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Global.Microsoft.VisualBasic.CompilerServices
    <Global.System.Diagnostics.DebuggerNonUserCode()>
    <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)>
    Public Class IncompleteInitialization
        Inherits Global.System.Exception
        Public Sub New()
            MyBase.New()
        End Sub

    End Class
End Namespace
