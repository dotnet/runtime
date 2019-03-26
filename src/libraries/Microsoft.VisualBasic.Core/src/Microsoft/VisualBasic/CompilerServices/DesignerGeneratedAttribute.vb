' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.ComponentModel

Namespace Global.Microsoft.VisualBasic.CompilerServices
    ''' <summary>
    ''' When applied to a class, the compiler will generate an implicit call to
    ''' to a private InitializeComponent method from the default synthetic
    ''' constructor. The compiler will also verify that this method is called
    ''' from user defined constructors and report a warning or error if it is not.
    ''' The IDE will honor this attribute when generating code on behalf of the
    ''' user. 
    ''' </summary>
    ''' <remarks>
    ''' WARNING: Do not rename this attribute or move it out of this module.  Otherwise there
    ''' are compiler changes that will need to be made
    ''' </remarks>
    <AttributeUsage(AttributeTargets.Class, AllowMultiple:=False, Inherited:=False)>
    <EditorBrowsable(EditorBrowsableState.Never)>
    Public NotInheritable Class DesignerGeneratedAttribute
        Inherits Attribute
    End Class
End Namespace
