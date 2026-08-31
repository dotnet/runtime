' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports Xunit

' This test needs to be written in VB, since the C# compiler has a different rule around
' filling in the InterfaceImpl table. Notably, the C# compiler will emit into the table, all of the
' interfaces that are transitively implemented by a class that the C# compiler is aware of, and
' the VB compiler will only emit the interfaces as specified in the source. In practice, this isn't
' supposed to have any meaningful impact unless the set of assemblies being used at runtime does not
' match the set of assemblies used at compile time, but it does impact some of the runtimes internal
' algorithms around interface resolution, notably the behavior of LoadExactInterfaceMap has an optimization
' that avoids doing quite a lot of work.
Public Class GitHub_123254

    ' Test the scenario where the implied interface is in the interface map of a containing interface as a special marker type, and the containing interface itself is ALSO a special marker type.
    ' The critical path is that when establishing the interface map for ILayer3_1(Of T) we find read ILayer1_1(Of T) as a special marker type member of ILayer2_1(Of T), and the ILayer2_1(Of T) itself is a special marker type.
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

    ' Test the scenario where the implied interface is in the interface map of a containing interface as a special marker type, and the containing interface itself is NOT a special marker type.
    ' The critical path is that when establishing the interface map for ILayer3_2(Of T) we find read ILayer1_2(Of T) as a special marker type member of ILayer2_2(Of T), and the ILayer2_2(Of T) itself is a special marker type.
    ' Then, it will also turn out that even though we had a special marker type in the interface map we're expanding, we will need to put an exact type into the map since that's what we actually need.
    <Fact>
    Public Shared Sub Test_Method2()
        Dim w As New TestClass2(Of Integer)
        Dim en As IEnumerator(Of String) = DirectCast(w, ILayer1_2(Of String)).GetEnumerator()
    End Sub

    Public Class TestClass2(Of T)
        Implements ILayer3_2(Of T)
        Public ReadOnly Property Count As Integer Implements ILayer2_2(Of String).Count

        Public Function GetEnumerator() As IEnumerator(Of String) Implements ILayer1_2(Of String).GetEnumerator
            Return DirectCast(Array.Empty(Of String)(), IEnumerable(Of String)).GetEnumerator()
        End Function
    End Class

    Public Interface ILayer3_2(Of T)
        Inherits ILayer2_2(Of String)
    End Interface

    Public Interface ILayer2_2(Of T)
        Inherits ILayer1_2(Of T)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_2(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    ' Test the scenario where the implied interface is in the interface map of a containing interface as a special marker type, and the containing interface itself is NOT a special marker type.
    ' The critical path is that when establishing the interface map for TestClass3 we find read ILayer1_3(Of T) as a special marker type member of ILayer3_3(Of TestClass, Integer), but when we place
    ' the interface onto TestClass3 we find that we can use the special marker type even though the containing interface is not the special marker type.
    <Fact>
    Public Shared Sub Test_Method3()
        Dim w As New TestClass3
        Dim en As IEnumerator(Of TestClass3) = DirectCast(w, ILayer1_3(Of TestClass3)).GetEnumerator()
    End Sub

    Public Structure TestClass3
        Implements ILayer3_3(Of TestClass3, Integer)
        Public ReadOnly Property Count As Integer Implements ILayer2_3(Of TestClass3).Count

        Public Function GetEnumerator() As IEnumerator(Of TestClass3) Implements ILayer1_3(Of TestClass3).GetEnumerator
            Return DirectCast(Array.Empty(Of TestClass3)(), IEnumerable(Of TestClass3)).GetEnumerator()
        End Function
    End Structure

    Public Interface ILayer3_3(Of T, SomethingElse)
        Inherits ILayer2_3(Of T)
    End Interface

    Public Interface ILayer2_3(Of T)
        Inherits ILayer1_3(Of T)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_3(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    ' Test the scenario where the implied interface is in the interface map of a containing interface as a special marker type, and the containing interface itself is NOT a special marker type.
    ' The critical path is that when establishing the interface map for TestClass4 we find read ILayer3_4(Of String) and then under that there are exact types ILayer2_4(Of Integer) and ILayer1_4(Of TestClass4)
    ' Then the algorithm will decide to put a special marker type for ILayer1_4(Of T) into the map since it is the appropriate shape, and we will place the exact type ILayer2_4(Of Integer) into the map since that is the exact type needed.
    <Fact>
    Public Shared Sub Test_Method4()
        Dim w As New TestClass4
        Dim en As IEnumerator(Of TestClass4) = DirectCast(w, ILayer1_4(Of TestClass4)).GetEnumerator()
        Dim countVal As Integer = DirectCast(w, ILayer2_4(Of Integer)).Count
    End Sub

    Public Structure TestClass4
        Implements ILayer3_4(Of String)
        Public ReadOnly Property Count As Integer Implements ILayer2_4(Of Integer).Count

        Public Function GetEnumerator() As IEnumerator(Of TestClass4) Implements ILayer1_4(Of TestClass4).GetEnumerator
            Return DirectCast(Array.Empty(Of TestClass4)(), IEnumerable(Of TestClass4)).GetEnumerator()
        End Function
    End Structure

    Public Interface ILayer3_4(Of T)
        Inherits ILayer2_4(Of Integer)
    End Interface

    Public Interface ILayer2_4(Of T)
        Inherits ILayer1_4(Of TestClass4)
        ReadOnly Property Count As Integer
    End Interface

    Public Interface ILayer1_4(Of T)
        Function GetEnumerator() As IEnumerator(Of T)
    End Interface

    ' Test path for forcing the interface map out of the supporting special marker types due to a conflict with the concept of special marker types
    ' I could only find a way to hit this path with reflection, and since reflection is imperfect on NativeAOT, just skip this test there.
    <ConditionalFact(GetType(TestLibrary.Utilities), NameOf(TestLibrary.Utilities.IsNotNativeAot))>
    Public Shared Sub Test_Method5()
        ' Test indirect implementation of interface
        Dim testClassType As Type = GetType(TestClass5(Of Integer)).GetGenericTypeDefinition().MakeGenericType(GetType(ILayer1_5(Of Integer)).GetGenericTypeDefinition().GetGenericArguments()(0))
        Console.WriteLine("testClassType first generic argument: " & testClassType.GetGenericArguments()(0).Name)
        For Each iface As Type In testClassType.GetInterfaces()
            Console.WriteLine("IFace name: " & iface.Name)
            Console.WriteLine("IFace first generic argument: " & iface.GetGenericArguments()(0).Name)
            Assert.Equal("Z", iface.GetGenericArguments()(0).Name)
        Next

        ' Test direct implementation of interface
        testClassType = GetType(TestClass6(Of Integer)).GetGenericTypeDefinition().MakeGenericType(GetType(ILayer1_5(Of Integer)).GetGenericTypeDefinition().GetGenericArguments()(0))
        Console.WriteLine("testClassType first generic argument: " & testClassType.GetGenericArguments()(0).Name)
        For Each iface As Type In testClassType.GetInterfaces()
            Console.WriteLine("IFace name: " & iface.Name)
            Console.WriteLine("IFace first generic argument: " & iface.GetGenericArguments()(0).Name)
            Assert.Equal("Z", iface.GetGenericArguments()(0).Name)
        Next

        ' Test implementation via containing interface
        testClassType = GetType(ILayer3_5(Of Integer)).GetGenericTypeDefinition().MakeGenericType(GetType(ILayer1_5(Of Integer)).GetGenericTypeDefinition().GetGenericArguments()(0))
        Console.WriteLine("testClassType first generic argument: " & testClassType.GetGenericArguments()(0).Name)
        For Each iface As Type In testClassType.GetInterfaces()
            Console.WriteLine("IFace name: " & iface.Name)
            Console.WriteLine("IFace first generic argument: " & iface.GetGenericArguments()(0).Name)
            Assert.Equal("Z", iface.GetGenericArguments()(0).Name)
        Next
    End Sub

    Public Structure TestClass5(Of T)
        Implements ILayer2_5(Of T, Integer)
        Sub GetEnumerator(argument As TestClass5(Of T)) Implements ILayer1_5(Of T).GetEnumerator
        End Sub
    End Structure

    Public Structure TestClass6(Of U)
        Implements ILayer1_5(Of U)
        Sub GetEnumerator(argument As TestClass5(Of U)) Implements ILayer1_5(Of U).GetEnumerator
        End Sub
    End Structure

    Public Interface ILayer3_5(Of R)
        Inherits ILayer2_5(Of R, Integer)
    End Interface

    Public Interface ILayer2_5(Of V, W)
        Inherits ILayer1_5(Of V)
    End Interface

    Public Interface ILayer1_5(Of Z)
        Sub GetEnumerator(argument As TestClass5(Of Z))
    End Interface
End Class