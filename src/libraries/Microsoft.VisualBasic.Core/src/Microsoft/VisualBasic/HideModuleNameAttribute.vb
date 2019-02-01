' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.ComponentModel

Namespace Global.Microsoft.VisualBasic
    ''' <summary>
    ''' When applied to a module, Intellisense will hide the module from
    ''' the statement completion list, but not the contents of the module.
    ''' </summary>
    ''' <remarks>
    ''' WARNING: Do not rename this attribute or move it out of this module.  Otherwise there
    ''' are compiler changes that will need to be made
    ''' </remarks>
    <AttributeUsage(AttributeTargets.Class, AllowMultiple:=False, Inherited:=False)>
    <EditorBrowsable(EditorBrowsableState.Never)>
    Public NotInheritable Class HideModuleNameAttribute
        Inherits Global.System.Attribute
    End Class
End Namespace
