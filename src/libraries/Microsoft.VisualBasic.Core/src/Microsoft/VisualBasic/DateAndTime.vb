' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System

Namespace Microsoft.VisualBasic
    Public Module DateAndTime
        Public ReadOnly Property Now As DateTime
            Get
                Return DateTime.Now
            End Get
        End Property

        Public ReadOnly Property Today As DateTime
            Get
                Return DateTime.Today
            End Get
        End Property
    End Module
End Namespace
