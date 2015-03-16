' Copyright (c) Microsoft. All rights reserved.
' Licensed under the MIT license. See LICENSE file in the project root for full license information.

Namespace Global.Microsoft.VisualBasic
    <Global.System.AttributeUsage(Global.System.AttributeTargets.Class, Inherited:=False)>
    <Global.System.ComponentModel.EditorBrowsable(Global.System.ComponentModel.EditorBrowsableState.Never)>
    Public Class HideModuleNameAttribute
        Inherits Global.System.Attribute
    End Class
End Namespace