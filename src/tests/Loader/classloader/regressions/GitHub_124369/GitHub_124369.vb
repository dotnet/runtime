' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports Xunit
Imports System.Runtime.CompilerServices

' This test needs to be written in VB, since the C# compiler has a different rule around
' filling in the InterfaceImpl table. Notably, the C# compiler will emit into the table, all of the
' interfaces that are transitively implemented by a class that the C# compiler is aware of, and
' the VB compiler will only emit the interfaces as specified in the source. In practice, this isn't
' supposed to have any meaningful impact unless the set of assemblies being used at runtime does not
' match the set of assemblies used at compile time, but it does impact some of the runtimes internal
' algorithms around interface resolution, notably the behavior of LoadExactInterfaceMap has an optimization
' that avoids doing quite a lot of work.
'
' This is a variant of GitHub_123254 test which tests the situations where various of the interfaces
' types have had their open generic type loaded, and the interfaces examined
Public Class GitHub_124369

    ' Test the scenario where the implied interface is in the interface map of a containing interface as a special marker type, and the containing interface itself is ALSO a special marker type.
    ' The critical path is that when establishing the interface map for ILayer3_1(Of T) we find read ILayer1_1(Of T) as a special marker type member of ILayer2_1(Of T), and the ILayer2_1(Of T) itself is a special marker type.
    <Fact>
    Public Shared Sub Test_Method1_000()
        Test_Method1_000_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method1_000_Inner()
        Dim w As New TestClass1_000(Of Integer)
        Dim en As IEnumerator(Of Integer) = DirectCast(w, ILayer1_1_000(Of Integer)).GetEnumerator()
    End Sub

    Public Class TestClass1_000(Of T)
        Implements ILayer3_1_000(Of T)

        Public ReadOnly Property Count As Integer Implements ILayer2_1_000(Of T).Count

        Public Function GetEnumerator() As IEnumerator(Of T) Implements ILayer1_1_000(Of T).GetEnumerator
            Return DirectCast(Array.Empty(Of T)(), IEnumerable(Of T)).GetEnumerator()
        End Function
    End Class

    Public Interface ILayer3_1_000(Of T)
        Inherits ILayer2_1_000(Of T)
    End Interface

    Public Interface ILayer2_1_000(Of T)
        Inherits ILayer1_1_000(Of T)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_1_000(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method1_001()
        GetType(ILayer3_1_001(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method1_001_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method1_001_Inner()
        Dim w As New TestClass1_001(Of Integer)
        Dim en As IEnumerator(Of Integer) = DirectCast(w, ILayer1_1_001(Of Integer)).GetEnumerator()
    End Sub

    Public Class TestClass1_001(Of T)
        Implements ILayer3_1_001(Of T)

        Public ReadOnly Property Count As Integer Implements ILayer2_1_001(Of T).Count

        Public Function GetEnumerator() As IEnumerator(Of T) Implements ILayer1_1_001(Of T).GetEnumerator
            Return DirectCast(Array.Empty(Of T)(), IEnumerable(Of T)).GetEnumerator()
        End Function
    End Class

    Public Interface ILayer3_1_001(Of T)
        Inherits ILayer2_1_001(Of T)
    End Interface

    Public Interface ILayer2_1_001(Of T)
        Inherits ILayer1_1_001(Of T)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_1_001(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method1_010()
        GetType(ILayer2_1_010(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method1_010_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method1_010_Inner()
        Dim w As New TestClass1_010(Of Integer)
        Dim en As IEnumerator(Of Integer) = DirectCast(w, ILayer1_1_010(Of Integer)).GetEnumerator()
    End Sub

    Public Class TestClass1_010(Of T)
        Implements ILayer3_1_010(Of T)

        Public ReadOnly Property Count As Integer Implements ILayer2_1_010(Of T).Count

        Public Function GetEnumerator() As IEnumerator(Of T) Implements ILayer1_1_010(Of T).GetEnumerator
            Return DirectCast(Array.Empty(Of T)(), IEnumerable(Of T)).GetEnumerator()
        End Function
    End Class

    Public Interface ILayer3_1_010(Of T)
        Inherits ILayer2_1_010(Of T)
    End Interface

    Public Interface ILayer2_1_010(Of T)
        Inherits ILayer1_1_010(Of T)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_1_010(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method1_011()
        GetType(ILayer2_1_011(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        GetType(ILayer3_1_011(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method1_011_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method1_011_Inner()
        Dim w As New TestClass1_011(Of Integer)
        Dim en As IEnumerator(Of Integer) = DirectCast(w, ILayer1_1_011(Of Integer)).GetEnumerator()
    End Sub

    Public Class TestClass1_011(Of T)
        Implements ILayer3_1_011(Of T)

        Public ReadOnly Property Count As Integer Implements ILayer2_1_011(Of T).Count

        Public Function GetEnumerator() As IEnumerator(Of T) Implements ILayer1_1_011(Of T).GetEnumerator
            Return DirectCast(Array.Empty(Of T)(), IEnumerable(Of T)).GetEnumerator()
        End Function
    End Class

    Public Interface ILayer3_1_011(Of T)
        Inherits ILayer2_1_011(Of T)
    End Interface

    Public Interface ILayer2_1_011(Of T)
        Inherits ILayer1_1_011(Of T)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_1_011(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method1_100()
        GetType(ILayer1_1_100(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method1_100_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method1_100_Inner()
        Dim w As New TestClass1_100(Of Integer)
        Dim en As IEnumerator(Of Integer) = DirectCast(w, ILayer1_1_100(Of Integer)).GetEnumerator()
    End Sub

    Public Class TestClass1_100(Of T)
        Implements ILayer3_1_100(Of T)

        Public ReadOnly Property Count As Integer Implements ILayer2_1_100(Of T).Count

        Public Function GetEnumerator() As IEnumerator(Of T) Implements ILayer1_1_100(Of T).GetEnumerator
            Return DirectCast(Array.Empty(Of T)(), IEnumerable(Of T)).GetEnumerator()
        End Function
    End Class

    Public Interface ILayer3_1_100(Of T)
        Inherits ILayer2_1_100(Of T)
    End Interface

    Public Interface ILayer2_1_100(Of T)
        Inherits ILayer1_1_100(Of T)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_1_100(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method1_101()
        GetType(ILayer1_1_101(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        GetType(ILayer3_1_101(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method1_101_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method1_101_Inner()
        Dim w As New TestClass1_101(Of Integer)
        Dim en As IEnumerator(Of Integer) = DirectCast(w, ILayer1_1_101(Of Integer)).GetEnumerator()
    End Sub

    Public Class TestClass1_101(Of T)
        Implements ILayer3_1_101(Of T)

        Public ReadOnly Property Count As Integer Implements ILayer2_1_101(Of T).Count

        Public Function GetEnumerator() As IEnumerator(Of T) Implements ILayer1_1_101(Of T).GetEnumerator
            Return DirectCast(Array.Empty(Of T)(), IEnumerable(Of T)).GetEnumerator()
        End Function
    End Class

    Public Interface ILayer3_1_101(Of T)
        Inherits ILayer2_1_101(Of T)
    End Interface

    Public Interface ILayer2_1_101(Of T)
        Inherits ILayer1_1_101(Of T)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_1_101(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method1_110()
        GetType(ILayer1_1_110(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        GetType(ILayer2_1_110(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method1_110_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method1_110_Inner()
        Dim w As New TestClass1_110(Of Integer)
        Dim en As IEnumerator(Of Integer) = DirectCast(w, ILayer1_1_110(Of Integer)).GetEnumerator()
    End Sub

    Public Class TestClass1_110(Of T)
        Implements ILayer3_1_110(Of T)

        Public ReadOnly Property Count As Integer Implements ILayer2_1_110(Of T).Count

        Public Function GetEnumerator() As IEnumerator(Of T) Implements ILayer1_1_110(Of T).GetEnumerator
            Return DirectCast(Array.Empty(Of T)(), IEnumerable(Of T)).GetEnumerator()
        End Function
    End Class

    Public Interface ILayer3_1_110(Of T)
        Inherits ILayer2_1_110(Of T)
    End Interface

    Public Interface ILayer2_1_110(Of T)
        Inherits ILayer1_1_110(Of T)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_1_110(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method1_111()
        GetType(ILayer1_1_111(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        GetType(ILayer2_1_111(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        GetType(ILayer3_1_111(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method1_111_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method1_111_Inner()
        Dim w As New TestClass1_111(Of Integer)
        Dim en As IEnumerator(Of Integer) = DirectCast(w, ILayer1_1_111(Of Integer)).GetEnumerator()
    End Sub

    Public Class TestClass1_111(Of T)
        Implements ILayer3_1_111(Of T)

        Public ReadOnly Property Count As Integer Implements ILayer2_1_111(Of T).Count

        Public Function GetEnumerator() As IEnumerator(Of T) Implements ILayer1_1_111(Of T).GetEnumerator
            Return DirectCast(Array.Empty(Of T)(), IEnumerable(Of T)).GetEnumerator()
        End Function
    End Class

    Public Interface ILayer3_1_111(Of T)
        Inherits ILayer2_1_111(Of T)
    End Interface

    Public Interface ILayer2_1_111(Of T)
        Inherits ILayer1_1_111(Of T)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_1_111(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    ' Test the scenario where the implied interface is in the interface map of a containing interface as a special marker type, and the containing interface itself is NOT a special marker type.
    ' The critical path is that when establishing the interface map for ILayer3_2(Of T) we find read ILayer1_2(Of T) as a special marker type member of ILayer2_2(Of T), and the ILayer2_2(Of T) itself is a special marker type.
    ' Then, it will also turn out that even though we had a special marker type in the interface map we're expanding, we will need to put an exact type into the map since that's what we actually need.
    <Fact>
    Public Shared Sub Test_Method2_000()
        Test_Method2_000_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method2_000_Inner()
        Dim w As New TestClass2_000(Of Integer)
        Dim en As IEnumerator(Of String) = DirectCast(w, ILayer1_2_000(Of String)).GetEnumerator()
    End Sub

    Public Class TestClass2_000(Of T)
        Implements ILayer3_2_000(Of T)
        Public ReadOnly Property Count As Integer Implements ILayer2_2_000(Of String).Count

        Public Function GetEnumerator() As IEnumerator(Of String) Implements ILayer1_2_000(Of String).GetEnumerator
            Return DirectCast(Array.Empty(Of String)(), IEnumerable(Of String)).GetEnumerator()
        End Function
    End Class

    Public Interface ILayer3_2_000(Of T)
        Inherits ILayer2_2_000(Of String)
    End Interface

    Public Interface ILayer2_2_000(Of T)
        Inherits ILayer1_2_000(Of T)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_2_000(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method2_001()
        GetType(ILayer3_2_001(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method2_001_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method2_001_Inner()
        Dim w As New TestClass2_001(Of Integer)
        Dim en As IEnumerator(Of String) = DirectCast(w, ILayer1_2_001(Of String)).GetEnumerator()
    End Sub

    Public Class TestClass2_001(Of T)
        Implements ILayer3_2_001(Of T)
        Public ReadOnly Property Count As Integer Implements ILayer2_2_001(Of String).Count

        Public Function GetEnumerator() As IEnumerator(Of String) Implements ILayer1_2_001(Of String).GetEnumerator
            Return DirectCast(Array.Empty(Of String)(), IEnumerable(Of String)).GetEnumerator()
        End Function
    End Class

    Public Interface ILayer3_2_001(Of T)
        Inherits ILayer2_2_001(Of String)
    End Interface

    Public Interface ILayer2_2_001(Of T)
        Inherits ILayer1_2_001(Of T)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_2_001(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method2_010()
        GetType(ILayer2_2_010(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method2_010_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method2_010_Inner()
        Dim w As New TestClass2_010(Of Integer)
        Dim en As IEnumerator(Of String) = DirectCast(w, ILayer1_2_010(Of String)).GetEnumerator()
    End Sub

    Public Class TestClass2_010(Of T)
        Implements ILayer3_2_010(Of T)
        Public ReadOnly Property Count As Integer Implements ILayer2_2_010(Of String).Count

        Public Function GetEnumerator() As IEnumerator(Of String) Implements ILayer1_2_010(Of String).GetEnumerator
            Return DirectCast(Array.Empty(Of String)(), IEnumerable(Of String)).GetEnumerator()
        End Function
    End Class

    Public Interface ILayer3_2_010(Of T)
        Inherits ILayer2_2_010(Of String)
    End Interface

    Public Interface ILayer2_2_010(Of T)
        Inherits ILayer1_2_010(Of T)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_2_010(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method2_011()
        GetType(ILayer2_2_011(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        GetType(ILayer3_2_011(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method2_011_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method2_011_Inner()
        Dim w As New TestClass2_011(Of Integer)
        Dim en As IEnumerator(Of String) = DirectCast(w, ILayer1_2_011(Of String)).GetEnumerator()
    End Sub

    Public Class TestClass2_011(Of T)
        Implements ILayer3_2_011(Of T)
        Public ReadOnly Property Count As Integer Implements ILayer2_2_011(Of String).Count

        Public Function GetEnumerator() As IEnumerator(Of String) Implements ILayer1_2_011(Of String).GetEnumerator
            Return DirectCast(Array.Empty(Of String)(), IEnumerable(Of String)).GetEnumerator()
        End Function
    End Class

    Public Interface ILayer3_2_011(Of T)
        Inherits ILayer2_2_011(Of String)
    End Interface

    Public Interface ILayer2_2_011(Of T)
        Inherits ILayer1_2_011(Of T)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_2_011(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method2_100()
        GetType(ILayer1_2_100(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method2_100_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method2_100_Inner()
        Dim w As New TestClass2_100(Of Integer)
        Dim en As IEnumerator(Of String) = DirectCast(w, ILayer1_2_100(Of String)).GetEnumerator()
    End Sub

    Public Class TestClass2_100(Of T)
        Implements ILayer3_2_100(Of T)
        Public ReadOnly Property Count As Integer Implements ILayer2_2_100(Of String).Count

        Public Function GetEnumerator() As IEnumerator(Of String) Implements ILayer1_2_100(Of String).GetEnumerator
            Return DirectCast(Array.Empty(Of String)(), IEnumerable(Of String)).GetEnumerator()
        End Function
    End Class

    Public Interface ILayer3_2_100(Of T)
        Inherits ILayer2_2_100(Of String)
    End Interface

    Public Interface ILayer2_2_100(Of T)
        Inherits ILayer1_2_100(Of T)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_2_100(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method2_101()
        GetType(ILayer1_2_101(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        GetType(ILayer3_2_101(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method2_101_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method2_101_Inner()
        Dim w As New TestClass2_101(Of Integer)
        Dim en As IEnumerator(Of String) = DirectCast(w, ILayer1_2_101(Of String)).GetEnumerator()
    End Sub

    Public Class TestClass2_101(Of T)
        Implements ILayer3_2_101(Of T)
        Public ReadOnly Property Count As Integer Implements ILayer2_2_101(Of String).Count

        Public Function GetEnumerator() As IEnumerator(Of String) Implements ILayer1_2_101(Of String).GetEnumerator
            Return DirectCast(Array.Empty(Of String)(), IEnumerable(Of String)).GetEnumerator()
        End Function
    End Class

    Public Interface ILayer3_2_101(Of T)
        Inherits ILayer2_2_101(Of String)
    End Interface

    Public Interface ILayer2_2_101(Of T)
        Inherits ILayer1_2_101(Of T)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_2_101(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method2_110()
        GetType(ILayer1_2_110(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        GetType(ILayer2_2_110(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method2_110_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method2_110_Inner()
        Dim w As New TestClass2_110(Of Integer)
        Dim en As IEnumerator(Of String) = DirectCast(w, ILayer1_2_110(Of String)).GetEnumerator()
    End Sub

    Public Class TestClass2_110(Of T)
        Implements ILayer3_2_110(Of T)
        Public ReadOnly Property Count As Integer Implements ILayer2_2_110(Of String).Count

        Public Function GetEnumerator() As IEnumerator(Of String) Implements ILayer1_2_110(Of String).GetEnumerator
            Return DirectCast(Array.Empty(Of String)(), IEnumerable(Of String)).GetEnumerator()
        End Function
    End Class

    Public Interface ILayer3_2_110(Of T)
        Inherits ILayer2_2_110(Of String)
    End Interface

    Public Interface ILayer2_2_110(Of T)
        Inherits ILayer1_2_110(Of T)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_2_110(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method2_111()
        GetType(ILayer1_2_111(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        GetType(ILayer2_2_111(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        GetType(ILayer3_2_111(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method2_111_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method2_111_Inner()
        Dim w As New TestClass2_111(Of Integer)
        Dim en As IEnumerator(Of String) = DirectCast(w, ILayer1_2_111(Of String)).GetEnumerator()
    End Sub

    Public Class TestClass2_111(Of T)
        Implements ILayer3_2_111(Of T)
        Public ReadOnly Property Count As Integer Implements ILayer2_2_111(Of String).Count

        Public Function GetEnumerator() As IEnumerator(Of String) Implements ILayer1_2_111(Of String).GetEnumerator
            Return DirectCast(Array.Empty(Of String)(), IEnumerable(Of String)).GetEnumerator()
        End Function
    End Class

    Public Interface ILayer3_2_111(Of T)
        Inherits ILayer2_2_111(Of String)
    End Interface

    Public Interface ILayer2_2_111(Of T)
        Inherits ILayer1_2_111(Of T)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_2_111(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    ' Test the scenario where the implied interface is in the interface map of a containing interface as a special marker type, and the containing interface itself is NOT a special marker type.
    ' The critical path is that when establishing the interface map for TestClass3 we find read ILayer1_3(Of T) as a special marker type member of ILayer3_3(Of TestClass, Integer), but when we place
    ' the interface onto TestClass3 we find that we can use the special marker type even though the containing interface is not the special marker type.
    <Fact>
    Public Shared Sub Test_Method3_000()
        Test_Method3_000_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method3_000_Inner()
        Dim w As New TestClass3_000
        Dim en As IEnumerator(Of TestClass3_000) = DirectCast(w, ILayer1_3_000(Of TestClass3_000)).GetEnumerator()
    End Sub

    Public Structure TestClass3_000
        Implements ILayer3_3_000(Of TestClass3_000, Integer)
        Public ReadOnly Property Count As Integer Implements ILayer2_3_000(Of TestClass3_000).Count

        Public Function GetEnumerator() As IEnumerator(Of TestClass3_000) Implements ILayer1_3_000(Of TestClass3_000).GetEnumerator
            Return DirectCast(Array.Empty(Of TestClass3_000)(), IEnumerable(Of TestClass3_000)).GetEnumerator()
        End Function
    End Structure

    Public Interface ILayer3_3_000(Of T, SomethingElse)
        Inherits ILayer2_3_000(Of T)
    End Interface

    Public Interface ILayer2_3_000(Of T)
        Inherits ILayer1_3_000(Of T)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_3_000(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method3_001()
        GetType(ILayer3_3_001(Of Integer, Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method3_001_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method3_001_Inner()
        Dim w As New TestClass3_001
        Dim en As IEnumerator(Of TestClass3_001) = DirectCast(w, ILayer1_3_001(Of TestClass3_001)).GetEnumerator()
    End Sub

    Public Structure TestClass3_001
        Implements ILayer3_3_001(Of TestClass3_001, Integer)
        Public ReadOnly Property Count As Integer Implements ILayer2_3_001(Of TestClass3_001).Count

        Public Function GetEnumerator() As IEnumerator(Of TestClass3_001) Implements ILayer1_3_001(Of TestClass3_001).GetEnumerator
            Return DirectCast(Array.Empty(Of TestClass3_001)(), IEnumerable(Of TestClass3_001)).GetEnumerator()
        End Function
    End Structure

    Public Interface ILayer3_3_001(Of T, SomethingElse)
        Inherits ILayer2_3_001(Of T)
    End Interface

    Public Interface ILayer2_3_001(Of T)
        Inherits ILayer1_3_001(Of T)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_3_001(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method3_010()
        GetType(ILayer2_3_010(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method3_010_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method3_010_Inner()
        Dim w As New TestClass3_010
        Dim en As IEnumerator(Of TestClass3_010) = DirectCast(w, ILayer1_3_010(Of TestClass3_010)).GetEnumerator()
    End Sub

    Public Structure TestClass3_010
        Implements ILayer3_3_010(Of TestClass3_010, Integer)
        Public ReadOnly Property Count As Integer Implements ILayer2_3_010(Of TestClass3_010).Count

        Public Function GetEnumerator() As IEnumerator(Of TestClass3_010) Implements ILayer1_3_010(Of TestClass3_010).GetEnumerator
            Return DirectCast(Array.Empty(Of TestClass3_010)(), IEnumerable(Of TestClass3_010)).GetEnumerator()
        End Function
    End Structure

    Public Interface ILayer3_3_010(Of T, SomethingElse)
        Inherits ILayer2_3_010(Of T)
    End Interface

    Public Interface ILayer2_3_010(Of T)
        Inherits ILayer1_3_010(Of T)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_3_010(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method3_011()
        GetType(ILayer2_3_011(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        GetType(ILayer3_3_011(Of Integer, Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method3_011_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method3_011_Inner()
        Dim w As New TestClass3_011
        Dim en As IEnumerator(Of TestClass3_011) = DirectCast(w, ILayer1_3_011(Of TestClass3_011)).GetEnumerator()
    End Sub

    Public Structure TestClass3_011
        Implements ILayer3_3_011(Of TestClass3_011, Integer)
        Public ReadOnly Property Count As Integer Implements ILayer2_3_011(Of TestClass3_011).Count

        Public Function GetEnumerator() As IEnumerator(Of TestClass3_011) Implements ILayer1_3_011(Of TestClass3_011).GetEnumerator
            Return DirectCast(Array.Empty(Of TestClass3_011)(), IEnumerable(Of TestClass3_011)).GetEnumerator()
        End Function
    End Structure

    Public Interface ILayer3_3_011(Of T, SomethingElse)
        Inherits ILayer2_3_011(Of T)
    End Interface

    Public Interface ILayer2_3_011(Of T)
        Inherits ILayer1_3_011(Of T)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_3_011(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method3_100()
        GetType(ILayer1_3_100(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method3_100_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method3_100_Inner()
        Dim w As New TestClass3_100
        Dim en As IEnumerator(Of TestClass3_100) = DirectCast(w, ILayer1_3_100(Of TestClass3_100)).GetEnumerator()
    End Sub

    Public Structure TestClass3_100
        Implements ILayer3_3_100(Of TestClass3_100, Integer)
        Public ReadOnly Property Count As Integer Implements ILayer2_3_100(Of TestClass3_100).Count

        Public Function GetEnumerator() As IEnumerator(Of TestClass3_100) Implements ILayer1_3_100(Of TestClass3_100).GetEnumerator
            Return DirectCast(Array.Empty(Of TestClass3_100)(), IEnumerable(Of TestClass3_100)).GetEnumerator()
        End Function
    End Structure

    Public Interface ILayer3_3_100(Of T, SomethingElse)
        Inherits ILayer2_3_100(Of T)
    End Interface

    Public Interface ILayer2_3_100(Of T)
        Inherits ILayer1_3_100(Of T)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_3_100(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method3_101()
        GetType(ILayer1_3_101(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        GetType(ILayer3_3_101(Of Integer, Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method3_101_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method3_101_Inner()
        Dim w As New TestClass3_101
        Dim en As IEnumerator(Of TestClass3_101) = DirectCast(w, ILayer1_3_101(Of TestClass3_101)).GetEnumerator()
    End Sub

    Public Structure TestClass3_101
        Implements ILayer3_3_101(Of TestClass3_101, Integer)
        Public ReadOnly Property Count As Integer Implements ILayer2_3_101(Of TestClass3_101).Count

        Public Function GetEnumerator() As IEnumerator(Of TestClass3_101) Implements ILayer1_3_101(Of TestClass3_101).GetEnumerator
            Return DirectCast(Array.Empty(Of TestClass3_101)(), IEnumerable(Of TestClass3_101)).GetEnumerator()
        End Function
    End Structure

    Public Interface ILayer3_3_101(Of T, SomethingElse)
        Inherits ILayer2_3_101(Of T)
    End Interface

    Public Interface ILayer2_3_101(Of T)
        Inherits ILayer1_3_101(Of T)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_3_101(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method3_110()
        GetType(ILayer1_3_110(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        GetType(ILayer2_3_110(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method3_110_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method3_110_Inner()
        Dim w As New TestClass3_110
        Dim en As IEnumerator(Of TestClass3_110) = DirectCast(w, ILayer1_3_110(Of TestClass3_110)).GetEnumerator()
    End Sub

    Public Structure TestClass3_110
        Implements ILayer3_3_110(Of TestClass3_110, Integer)
        Public ReadOnly Property Count As Integer Implements ILayer2_3_110(Of TestClass3_110).Count

        Public Function GetEnumerator() As IEnumerator(Of TestClass3_110) Implements ILayer1_3_110(Of TestClass3_110).GetEnumerator
            Return DirectCast(Array.Empty(Of TestClass3_110)(), IEnumerable(Of TestClass3_110)).GetEnumerator()
        End Function
    End Structure

    Public Interface ILayer3_3_110(Of T, SomethingElse)
        Inherits ILayer2_3_110(Of T)
    End Interface

    Public Interface ILayer2_3_110(Of T)
        Inherits ILayer1_3_110(Of T)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_3_110(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method3_111()
        GetType(ILayer1_3_111(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        GetType(ILayer2_3_111(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        GetType(ILayer3_3_111(Of Integer, Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method3_111_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method3_111_Inner()
        Dim w As New TestClass3_111
        Dim en As IEnumerator(Of TestClass3_111) = DirectCast(w, ILayer1_3_111(Of TestClass3_111)).GetEnumerator()
    End Sub

    Public Structure TestClass3_111
        Implements ILayer3_3_111(Of TestClass3_111, Integer)
        Public ReadOnly Property Count As Integer Implements ILayer2_3_111(Of TestClass3_111).Count

        Public Function GetEnumerator() As IEnumerator(Of TestClass3_111) Implements ILayer1_3_111(Of TestClass3_111).GetEnumerator
            Return DirectCast(Array.Empty(Of TestClass3_111)(), IEnumerable(Of TestClass3_111)).GetEnumerator()
        End Function
    End Structure

    Public Interface ILayer3_3_111(Of T, SomethingElse)
        Inherits ILayer2_3_111(Of T)
    End Interface

    Public Interface ILayer2_3_111(Of T)
        Inherits ILayer1_3_111(Of T)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_3_111(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    ' Test the scenario where the implied interface is in the interface map of a containing interface as a special marker type, and the containing interface itself is NOT a special marker type.
    ' The critical path is that when establishing the interface map for TestClass4 we find read ILayer3_4(Of String) and then under that there are exact types ILayer2_4(Of Integer) and ILayer1_4(Of TestClass4)
    ' Then the algorithm will decide to put a special marker type for ILayer1_4(Of T) into the map since it is the appropriate shape, and we will place the exact type ILayer2_4(Of Integer) into the map since that is the exact type needed.
    <Fact>
    Public Shared Sub Test_Method4_000()
        Test_Method4_000_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method4_000_Inner()
        Dim w As New TestClass4_000
        Dim en As IEnumerator(Of TestClass4_000) = DirectCast(w, ILayer1_4_000(Of TestClass4_000)).GetEnumerator()
        Dim countVal As Integer = DirectCast(w, ILayer2_4_000(Of Integer)).Count
    End Sub

    Public Structure TestClass4_000
        Implements ILayer3_4_000(Of String)
        Public ReadOnly Property Count As Integer Implements ILayer2_4_000(Of Integer).Count

        Public Function GetEnumerator() As IEnumerator(Of TestClass4_000) Implements ILayer1_4_000(Of TestClass4_000).GetEnumerator
            Return DirectCast(Array.Empty(Of TestClass4_000)(), IEnumerable(Of TestClass4_000)).GetEnumerator()
        End Function
    End Structure

    Public Interface ILayer3_4_000(Of T)
        Inherits ILayer2_4_000(Of Integer)
    End Interface

    Public Interface ILayer2_4_000(Of T)
        Inherits ILayer1_4_000(Of TestClass4_000)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_4_000(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method4_001()
        GetType(ILayer3_4_001(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method4_001_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method4_001_Inner()
        Dim w As New TestClass4_001
        Dim en As IEnumerator(Of TestClass4_001) = DirectCast(w, ILayer1_4_001(Of TestClass4_001)).GetEnumerator()
        Dim countVal As Integer = DirectCast(w, ILayer2_4_001(Of Integer)).Count
    End Sub

    Public Structure TestClass4_001
        Implements ILayer3_4_001(Of String)
        Public ReadOnly Property Count As Integer Implements ILayer2_4_001(Of Integer).Count

        Public Function GetEnumerator() As IEnumerator(Of TestClass4_001) Implements ILayer1_4_001(Of TestClass4_001).GetEnumerator
            Return DirectCast(Array.Empty(Of TestClass4_001)(), IEnumerable(Of TestClass4_001)).GetEnumerator()
        End Function
    End Structure

    Public Interface ILayer3_4_001(Of T)
        Inherits ILayer2_4_001(Of Integer)
    End Interface

    Public Interface ILayer2_4_001(Of T)
        Inherits ILayer1_4_001(Of TestClass4_001)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_4_001(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method4_010()
        GetType(ILayer2_4_010(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method4_010_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method4_010_Inner()
        Dim w As New TestClass4_010
        Dim en As IEnumerator(Of TestClass4_010) = DirectCast(w, ILayer1_4_010(Of TestClass4_010)).GetEnumerator()
        Dim countVal As Integer = DirectCast(w, ILayer2_4_010(Of Integer)).Count
    End Sub

    Public Structure TestClass4_010
        Implements ILayer3_4_010(Of String)
        Public ReadOnly Property Count As Integer Implements ILayer2_4_010(Of Integer).Count

        Public Function GetEnumerator() As IEnumerator(Of TestClass4_010) Implements ILayer1_4_010(Of TestClass4_010).GetEnumerator
            Return DirectCast(Array.Empty(Of TestClass4_010)(), IEnumerable(Of TestClass4_010)).GetEnumerator()
        End Function
    End Structure

    Public Interface ILayer3_4_010(Of T)
        Inherits ILayer2_4_010(Of Integer)
    End Interface

    Public Interface ILayer2_4_010(Of T)
        Inherits ILayer1_4_010(Of TestClass4_010)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_4_010(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method4_011()
        GetType(ILayer2_4_011(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        GetType(ILayer3_4_011(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method4_011_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method4_011_Inner()
        Dim w As New TestClass4_011
        Dim en As IEnumerator(Of TestClass4_011) = DirectCast(w, ILayer1_4_011(Of TestClass4_011)).GetEnumerator()
        Dim countVal As Integer = DirectCast(w, ILayer2_4_011(Of Integer)).Count
    End Sub

    Public Structure TestClass4_011
        Implements ILayer3_4_011(Of String)
        Public ReadOnly Property Count As Integer Implements ILayer2_4_011(Of Integer).Count

        Public Function GetEnumerator() As IEnumerator(Of TestClass4_011) Implements ILayer1_4_011(Of TestClass4_011).GetEnumerator
            Return DirectCast(Array.Empty(Of TestClass4_011)(), IEnumerable(Of TestClass4_011)).GetEnumerator()
        End Function
    End Structure

    Public Interface ILayer3_4_011(Of T)
        Inherits ILayer2_4_011(Of Integer)
    End Interface

    Public Interface ILayer2_4_011(Of T)
        Inherits ILayer1_4_011(Of TestClass4_011)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_4_011(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method4_100()
        GetType(ILayer1_4_100(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method4_100_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method4_100_Inner()
        Dim w As New TestClass4_100
        Dim en As IEnumerator(Of TestClass4_100) = DirectCast(w, ILayer1_4_100(Of TestClass4_100)).GetEnumerator()
        Dim countVal As Integer = DirectCast(w, ILayer2_4_100(Of Integer)).Count
    End Sub

    Public Structure TestClass4_100
        Implements ILayer3_4_100(Of String)
        Public ReadOnly Property Count As Integer Implements ILayer2_4_100(Of Integer).Count

        Public Function GetEnumerator() As IEnumerator(Of TestClass4_100) Implements ILayer1_4_100(Of TestClass4_100).GetEnumerator
            Return DirectCast(Array.Empty(Of TestClass4_100)(), IEnumerable(Of TestClass4_100)).GetEnumerator()
        End Function
    End Structure

    Public Interface ILayer3_4_100(Of T)
        Inherits ILayer2_4_100(Of Integer)
    End Interface

    Public Interface ILayer2_4_100(Of T)
        Inherits ILayer1_4_100(Of TestClass4_100)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_4_100(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method4_101()
        GetType(ILayer1_4_101(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        GetType(ILayer3_4_101(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method4_101_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method4_101_Inner()
        Dim w As New TestClass4_101
        Dim en As IEnumerator(Of TestClass4_101) = DirectCast(w, ILayer1_4_101(Of TestClass4_101)).GetEnumerator()
        Dim countVal As Integer = DirectCast(w, ILayer2_4_101(Of Integer)).Count
    End Sub

    Public Structure TestClass4_101
        Implements ILayer3_4_101(Of String)
        Public ReadOnly Property Count As Integer Implements ILayer2_4_101(Of Integer).Count

        Public Function GetEnumerator() As IEnumerator(Of TestClass4_101) Implements ILayer1_4_101(Of TestClass4_101).GetEnumerator
            Return DirectCast(Array.Empty(Of TestClass4_101)(), IEnumerable(Of TestClass4_101)).GetEnumerator()
        End Function
    End Structure

    Public Interface ILayer3_4_101(Of T)
        Inherits ILayer2_4_101(Of Integer)
    End Interface

    Public Interface ILayer2_4_101(Of T)
        Inherits ILayer1_4_101(Of TestClass4_101)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_4_101(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method4_110()
        GetType(ILayer1_4_110(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        GetType(ILayer2_4_110(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method4_110_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method4_110_Inner()
        Dim w As New TestClass4_110
        Dim en As IEnumerator(Of TestClass4_110) = DirectCast(w, ILayer1_4_110(Of TestClass4_110)).GetEnumerator()
        Dim countVal As Integer = DirectCast(w, ILayer2_4_110(Of Integer)).Count
    End Sub

    Public Structure TestClass4_110
        Implements ILayer3_4_110(Of String)
        Public ReadOnly Property Count As Integer Implements ILayer2_4_110(Of Integer).Count

        Public Function GetEnumerator() As IEnumerator(Of TestClass4_110) Implements ILayer1_4_110(Of TestClass4_110).GetEnumerator
            Return DirectCast(Array.Empty(Of TestClass4_110)(), IEnumerable(Of TestClass4_110)).GetEnumerator()
        End Function
    End Structure

    Public Interface ILayer3_4_110(Of T)
        Inherits ILayer2_4_110(Of Integer)
    End Interface

    Public Interface ILayer2_4_110(Of T)
        Inherits ILayer1_4_110(Of TestClass4_110)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_4_110(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method4_111()
        GetType(ILayer1_4_111(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        GetType(ILayer2_4_111(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        GetType(ILayer3_4_111(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method4_111_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method4_111_Inner()
        Dim w As New TestClass4_111
        Dim en As IEnumerator(Of TestClass4_111) = DirectCast(w, ILayer1_4_111(Of TestClass4_111)).GetEnumerator()
        Dim countVal As Integer = DirectCast(w, ILayer2_4_111(Of Integer)).Count
    End Sub

    Public Structure TestClass4_111
        Implements ILayer3_4_111(Of String)
        Public ReadOnly Property Count As Integer Implements ILayer2_4_111(Of Integer).Count

        Public Function GetEnumerator() As IEnumerator(Of TestClass4_111) Implements ILayer1_4_111(Of TestClass4_111).GetEnumerator
            Return DirectCast(Array.Empty(Of TestClass4_111)(), IEnumerable(Of TestClass4_111)).GetEnumerator()
        End Function
    End Structure

    Public Interface ILayer3_4_111(Of T)
        Inherits ILayer2_4_111(Of Integer)
    End Interface

    Public Interface ILayer2_4_111(Of T)
        Inherits ILayer1_4_111(Of TestClass4_111)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_4_111(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    ' Test the scenario where the implied interface is in the interface map of a containing interface as a special marker type, and the containing interface itself is ALSO a special marker type.
    ' The critical path is that when establishing the interface map for ILayer3_1(Of T) we find ILayer1_1(Of IList(Of T)) as an exact instantiation, and are able to identify that it is a problematic scenario which requires us to stop using all of the special marker type logic
    <Fact>
    Public Shared Sub Test_Method5_000()
        Test_Method5_000_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method5_000_Inner()
        Dim w As New TestClass5_000(Of Integer)
        Dim en As IEnumerator(Of IList(Of Integer)) = DirectCast(w, ILayer1_5_000(Of IList(Of Integer))).GetEnumerator()
    End Sub

    Public Class TestClass5_000(Of T)
        Implements ILayer3_5_000(Of T)

        Public ReadOnly Property Count As Integer Implements ILayer2_5_000(Of T).Count

        Public Function GetEnumerator() As IEnumerator(Of IList(Of T)) Implements ILayer1_5_000(Of IList(Of T)).GetEnumerator
            Return DirectCast(Array.Empty(Of IList(Of T))(), IEnumerable(Of IList(Of T))).GetEnumerator()
        End Function
    End Class

    Public Interface ILayer3_5_000(Of T)
        Inherits ILayer2_5_000(Of T)
    End Interface

    Public Interface ILayer2_5_000(Of T)
        Inherits ILayer1_5_000(Of IList(Of T))
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_5_000(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method5_001()
        GetType(ILayer3_5_001(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method5_001_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method5_001_Inner()
        Dim w As New TestClass5_001(Of Integer)
        Dim en As IEnumerator(Of IList(Of Integer)) = DirectCast(w, ILayer1_5_001(Of IList(Of Integer))).GetEnumerator()
    End Sub

    Public Class TestClass5_001(Of T)
        Implements ILayer3_5_001(Of T)

        Public ReadOnly Property Count As Integer Implements ILayer2_5_001(Of T).Count

        Public Function GetEnumerator() As IEnumerator(Of IList(Of T)) Implements ILayer1_5_001(Of IList(Of T)).GetEnumerator
            Return DirectCast(Array.Empty(Of IList(Of T))(), IEnumerable(Of IList(Of T))).GetEnumerator()
        End Function
    End Class

    Public Interface ILayer3_5_001(Of T)
        Inherits ILayer2_5_001(Of T)
    End Interface

    Public Interface ILayer2_5_001(Of T)
        Inherits ILayer1_5_001(Of IList(Of T))
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_5_001(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method5_010()
        GetType(ILayer2_5_010(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method5_010_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method5_010_Inner()
        Dim w As New TestClass5_010(Of Integer)
        Dim en As IEnumerator(Of IList(Of Integer)) = DirectCast(w, ILayer1_5_010(Of IList(Of Integer))).GetEnumerator()
    End Sub

    Public Class TestClass5_010(Of T)
        Implements ILayer3_5_010(Of T)

        Public ReadOnly Property Count As Integer Implements ILayer2_5_010(Of T).Count

        Public Function GetEnumerator() As IEnumerator(Of IList(Of T)) Implements ILayer1_5_010(Of IList(Of T)).GetEnumerator
            Return DirectCast(Array.Empty(Of IList(Of T))(), IEnumerable(Of IList(Of T))).GetEnumerator()
        End Function
    End Class

    Public Interface ILayer3_5_010(Of T)
        Inherits ILayer2_5_010(Of T)
    End Interface

    Public Interface ILayer2_5_010(Of T)
        Inherits ILayer1_5_010(Of IList(Of T))
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_5_010(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method5_011()
        GetType(ILayer2_5_011(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        GetType(ILayer3_5_011(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method5_011_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method5_011_Inner()
        Dim w As New TestClass5_011(Of Integer)
        Dim en As IEnumerator(Of IList(Of Integer)) = DirectCast(w, ILayer1_5_011(Of IList(Of Integer))).GetEnumerator()
    End Sub

    Public Class TestClass5_011(Of T)
        Implements ILayer3_5_011(Of T)

        Public ReadOnly Property Count As Integer Implements ILayer2_5_011(Of T).Count

        Public Function GetEnumerator() As IEnumerator(Of IList(Of T)) Implements ILayer1_5_011(Of IList(Of T)).GetEnumerator
            Return DirectCast(Array.Empty(Of IList(Of T))(), IEnumerable(Of IList(Of T))).GetEnumerator()
        End Function
    End Class

    Public Interface ILayer3_5_011(Of T)
        Inherits ILayer2_5_011(Of T)
    End Interface

    Public Interface ILayer2_5_011(Of T)
        Inherits ILayer1_5_011(Of IList(Of T))
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_5_011(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method5_100()
        GetType(ILayer1_5_100(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method5_100_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method5_100_Inner()
        Dim w As New TestClass5_100(Of Integer)
        Dim en As IEnumerator(Of IList(Of Integer)) = DirectCast(w, ILayer1_5_100(Of IList(Of Integer))).GetEnumerator()
    End Sub

    Public Class TestClass5_100(Of T)
        Implements ILayer3_5_100(Of T)

        Public ReadOnly Property Count As Integer Implements ILayer2_5_100(Of T).Count

        Public Function GetEnumerator() As IEnumerator(Of IList(Of T)) Implements ILayer1_5_100(Of IList(Of T)).GetEnumerator
            Return DirectCast(Array.Empty(Of IList(Of T))(), IEnumerable(Of IList(Of T))).GetEnumerator()
        End Function
    End Class

    Public Interface ILayer3_5_100(Of T)
        Inherits ILayer2_5_100(Of T)
    End Interface

    Public Interface ILayer2_5_100(Of T)
        Inherits ILayer1_5_100(Of IList(Of T))
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_5_100(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method5_101()
        GetType(ILayer1_5_101(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        GetType(ILayer3_5_101(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method5_101_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method5_101_Inner()
        Dim w As New TestClass5_101(Of Integer)
        Dim en As IEnumerator(Of IList(Of Integer)) = DirectCast(w, ILayer1_5_101(Of IList(Of Integer))).GetEnumerator()
    End Sub

    Public Class TestClass5_101(Of T)
        Implements ILayer3_5_101(Of T)

        Public ReadOnly Property Count As Integer Implements ILayer2_5_101(Of T).Count

        Public Function GetEnumerator() As IEnumerator(Of IList(Of T)) Implements ILayer1_5_101(Of IList(Of T)).GetEnumerator
            Return DirectCast(Array.Empty(Of IList(Of T))(), IEnumerable(Of IList(Of T))).GetEnumerator()
        End Function
    End Class

    Public Interface ILayer3_5_101(Of T)
        Inherits ILayer2_5_101(Of T)
    End Interface

    Public Interface ILayer2_5_101(Of T)
        Inherits ILayer1_5_101(Of IList(Of T))
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_5_101(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method5_110()
        GetType(ILayer1_5_110(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        GetType(ILayer2_5_110(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method5_110_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method5_110_Inner()
        Dim w As New TestClass5_110(Of Integer)
        Dim en As IEnumerator(Of IList(Of Integer)) = DirectCast(w, ILayer1_5_110(Of IList(Of Integer))).GetEnumerator()
    End Sub

    Public Class TestClass5_110(Of T)
        Implements ILayer3_5_110(Of T)

        Public ReadOnly Property Count As Integer Implements ILayer2_5_110(Of T).Count

        Public Function GetEnumerator() As IEnumerator(Of IList(Of T)) Implements ILayer1_5_110(Of IList(Of T)).GetEnumerator
            Return DirectCast(Array.Empty(Of IList(Of T))(), IEnumerable(Of IList(Of T))).GetEnumerator()
        End Function
    End Class

    Public Interface ILayer3_5_110(Of T)
        Inherits ILayer2_5_110(Of T)
    End Interface

    Public Interface ILayer2_5_110(Of T)
        Inherits ILayer1_5_110(Of IList(Of T))
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_5_110(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    <Fact>
    Public Shared Sub Test_Method5_111()
        GetType(ILayer1_5_111(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        GetType(ILayer2_5_111(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        GetType(ILayer3_5_111(Of Integer)).GetGenericTypeDefinition().GetInterfaces()
        Test_Method5_111_Inner()
    End Sub

    <MethodImpl(MethodImplOptions.NoInlining)>
    Public Shared Sub Test_Method5_111_Inner()
        Dim w As New TestClass5_111(Of Integer)
        Dim en As IEnumerator(Of IList(Of Integer)) = DirectCast(w, ILayer1_5_111(Of IList(Of Integer))).GetEnumerator()
    End Sub

    Public Class TestClass5_111(Of T)
        Implements ILayer3_5_111(Of T)

        Public ReadOnly Property Count As Integer Implements ILayer2_5_111(Of T).Count

        Public Function GetEnumerator() As IEnumerator(Of IList(Of T)) Implements ILayer1_5_111(Of IList(Of T)).GetEnumerator
            Return DirectCast(Array.Empty(Of IList(Of T))(), IEnumerable(Of IList(Of T))).GetEnumerator()
        End Function
    End Class

    Public Interface ILayer3_5_111(Of T)
        Inherits ILayer2_5_111(Of T)
    End Interface

    Public Interface ILayer2_5_111(Of T)
        Inherits ILayer1_5_111(Of IList(Of T))
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_5_111(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

End Class
