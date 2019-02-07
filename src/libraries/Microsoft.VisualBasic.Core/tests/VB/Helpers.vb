' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.
Option Explicit On
Option Strict On

Imports System
Imports System.Runtime.CompilerServices

Namespace Microsoft.VisualBasic.Tests.VB
    Public Module Helpers

        <Extension>
        Public Function TrimSeparators(s As String) As String
            Return s.TrimEnd({IO.Path.DirectorySeparatorChar, IO.Path.AltDirectorySeparatorChar})
        End Function

    End Module
End Namespace

