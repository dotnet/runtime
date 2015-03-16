' Copyright (c) Microsoft. All rights reserved.
' Licensed under the MIT license. See LICENSE file in the project root for full license information.

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