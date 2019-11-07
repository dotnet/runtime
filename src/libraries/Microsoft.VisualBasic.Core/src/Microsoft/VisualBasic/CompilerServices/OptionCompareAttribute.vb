' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.ComponentModel

Namespace Global.Microsoft.VisualBasic.CompilerServices
    ''' <summary>
    ''' OptionCompareAttribute is used by the compiler to determine 
    ''' when the Option Compare setting should be passed as the default 
    ''' value for the attributed argument.
    ''' </summary>
    ''' <remarks>
    ''' WARNING: Do not rename this attribute or move it out of this 
    ''' module.  Otherwise there are compiler changes that will
    ''' need to be made!
    ''' </remarks>
    <AttributeUsage(AttributeTargets.Parameter, Inherited:=False, AllowMultiple:=False)>
    <EditorBrowsable(EditorBrowsableState.Never)>
    Public NotInheritable Class OptionCompareAttribute
        Inherits Attribute
    End Class
End Namespace
