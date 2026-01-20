' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports Xunit

Public Class GitHub_123254

    <Fact>
    Public Shared Sub Test_Method1()
        Dim w As New TestClass1(Of Integer)
        Dim en As IEnumerator(Of Integer) = DirectCast(w, ILayer1_1(Of Integer)).GetEnumerator()
    End Sub

    Public Class TestClass1(Of T)
        Implements ILayer3_1(Of T)

        Public ReadOnly Property Count As Integer Implements ILayer2_1(Of T).Count

        Public Function GetEnumerator() As IEnumerator(Of T) Implements ILayer1_1(Of T).GetEnumerator
            Return DirectCast(Array.Empty(Of T)(), IEnumerable(Of T)).GetEnumerator()
        End Function
    End Class

    Public Interface ILayer3_1(Of T)
        Inherits ILayer2_1(Of T)
    End Interface

    Public Interface ILayer2_1(Of T)
        Inherits ILayer1_1(Of T)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_1(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface
End Class