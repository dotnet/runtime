' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.ComponentModel

Namespace Global.Microsoft.VisualBasic.CompilerServices
    ''' <summary>
    ''' OptionTextAttribute is used by the compiler to mark all Classes/Modules
    ''' as to whether we Option Compare Text is defined or not
    ''' </summary>
    ''' <remarks>
    ''' WARNING: Do not rename this attribute or move it out of this
    ''' module.  Otherwise there are compiler changes that will
    ''' need to be made!
    ''' </remarks>
    <AttributeUsage(AttributeTargets.Class, Inherited:=False, AllowMultiple:=False)>
    <EditorBrowsable(ComponentModel.EditorBrowsableState.Never)>
    Public NotInheritable Class OptionTextAttribute
        Inherits Attribute
    End Class
End Namespace
