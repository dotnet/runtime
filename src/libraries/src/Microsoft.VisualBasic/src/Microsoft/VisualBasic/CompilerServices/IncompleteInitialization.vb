' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Global.Microsoft.VisualBasic.CompilerServices
    <Global.System.Diagnostics.DebuggerNonUserCode()>
    <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)>
    <Global.System.Serializable>
    Public Class IncompleteInitialization
        Inherits Global.System.Exception
        Public Sub New()
            MyBase.New()
        End Sub

#Disable Warning CA2229 ' Rule wants ctor to be protected, but private to match desktop
        <Global.System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)>
        Private Sub New(ByVal info As System.Runtime.Serialization.SerializationInfo, ByVal context As System.Runtime.Serialization.StreamingContext)
            MyBase.New(info, context)
        End Sub
#Enable Warning CA2229 ' Implement Serialization constructor

    End Class
End Namespace
