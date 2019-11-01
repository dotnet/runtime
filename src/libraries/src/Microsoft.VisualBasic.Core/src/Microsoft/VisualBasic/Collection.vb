' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Diagnostics
Imports Microsoft.VisualBasic.CompilerServices
Imports Microsoft.VisualBasic.CompilerServices.ExceptionUtils
Imports Microsoft.VisualBasic.CompilerServices.Utils

Namespace Microsoft.VisualBasic

    <DebuggerTypeProxy(GetType(Collection.CollectionDebugView))>
    <DebuggerDisplay("Count = {Count}")>
    Public NotInheritable Class Collection : Implements ICollection, IList
        Private m_CultureInfo As CultureInfo 'The CultureInfo used for key comparisons
        Private m_KeyedNodesHash As Generic.Dictionary(Of String, Node) 'Hashtable mapping key (string) -> Node, contains only items added to the collection with a key
        Private m_ItemsList As FastList 'Doubly-linked list of Node containing all items in the collection
        Private m_Iterators As List(Of Object) 'List of iterators currently iterating this collection

        Public Sub New()
            MyBase.New()
            Initialize(GetCultureInfo())
        End Sub

        '*****************************************************************************
        'These methods are 1 based
        '*****************************************************************************

        ''' <summary>
        ''' Add an item to the collection
        ''' </summary>
        ''' <param name="Item"></param>
        ''' <param name="Key"></param>
        ''' <param name="Before"></param>
        ''' <param name="After"></param>
        Public Sub Add(ByVal Item As Object, Optional ByVal Key As String = Nothing, Optional ByVal Before As Object = Nothing, Optional ByVal After As Object = Nothing)

            'Before and After are mutually exclusive
            If (Before IsNot Nothing) AndAlso (After IsNot Nothing) Then
                Throw New ArgumentException(SR.Collection_BeforeAfterExclusive)
            End If

            'Create a new node
            Dim newNode As Node = New Node(Key, Item)

            'If a key was specified, add the new node to the hashtable of keys.  If this is 
            '  a duplicate key, we'll get an argument exception.  This prevents us from
            '  having to do a hashtable look-up to verify the key is unique.
            'However, we then have to be careful to remove the node from the hashtable if we fail to add the item to the
            '  list because the Before or After keys were bad.
            If Key IsNot Nothing Then
                Try
                    m_KeyedNodesHash.Add(Key, newNode)
                Catch ex As ArgumentException
                    Debug.Assert(m_KeyedNodesHash.ContainsKey(Key), "We got an argumentexception from a hashtable add, but it wasn't a duplicate key.")
                    ' Duplicate key.  Throw our own exception and our own message.
                    Throw VbMakeException(New ArgumentException(SR.Collection_DuplicateKey), vbErrors.DuplicateKey)
                End Try
            End If

            Try
                'Add the item to list and also (if a key was specified) to the hashtable 
                If (Before Is Nothing) AndAlso (After Is Nothing) Then  'Neither Before nor After have been specified
                    'Add the value to the linked list
                    m_ItemsList.Add(newNode)
                ElseIf Before IsNot Nothing Then
                    'Before has been specified
                    Dim beforeString As String = TryCast(Before, String)

                    If beforeString IsNot Nothing Then
                        Dim BeforeNode As Node = Nothing
                        If Not m_KeyedNodesHash.TryGetValue(beforeString, BeforeNode) Then
                            Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, NameOf(Before)), NameOf(Before))
                        End If
                        Debug.Assert(BeforeNode IsNot Nothing)

                        m_ItemsList.InsertBefore(newNode, BeforeNode)
                    Else
                        m_ItemsList.Insert(CInt(Before) - 1, newNode) 'Convert from 1 based to 0 based.
                    End If
                Else
                    'After has been specified
                    Dim afterString As String = TryCast(After, String)

                    If afterString IsNot Nothing Then
                        Dim AfterNode As Node = Nothing
                        If Not m_KeyedNodesHash.TryGetValue(afterString, AfterNode) Then
                            Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, NameOf(After)), NameOf(After))
                        End If
                        Debug.Assert(AfterNode IsNot Nothing)

                        m_ItemsList.InsertAfter(newNode, AfterNode)
                    Else
                        m_ItemsList.Insert(CInt(After), newNode)  'Conversion from 1 based to 0 based offsets need to add 1.
                    End If
                End If
            Catch ex As OutOfMemoryException
                Throw
            Catch ex As Threading.ThreadAbortException
                Throw
            Catch ex As StackOverflowException
                Throw
            Catch ex As Exception
                'We couldn't add the item to the list because the Before or After key was not found.  We need to back out the
                '  insert that we did into the hash table.
                If Key IsNot Nothing Then
                    m_KeyedNodesHash.Remove(Key)
                End If

                Throw
            End Try

            'Adjust the ForEach iterators
            AdjustEnumeratorsOnNodeInserted(newNode)
        End Sub

        ''' <summary>
        ''' Clears all items in the collection
        ''' </summary>
        Public Sub Clear()
            m_KeyedNodesHash.Clear()
            m_ItemsList.Clear()

            'Notify the enumerators
            Dim i As Integer = m_Iterators.Count - 1
            While i >= 0
                Dim ref As WeakReference = DirectCast(m_Iterators(i), WeakReference)
                If ref.IsAlive Then
                    Dim enumerator As ForEachEnum = CType(ref.Target, ForEachEnum)
                    If Not enumerator Is Nothing Then
                        enumerator.AdjustOnListCleared()
                    End If
                Else
                    m_Iterators.RemoveAt(i)
                End If
                i -= 1
            End While
        End Sub

        ''' <summary>
        ''' Returns true if the given key is in the collection
        ''' </summary>
        ''' <param name="Key"></param>
        ''' <returns>True - the given key is in the collection.  False otherwise.</returns>
        Public Function Contains(ByVal Key As String) As Boolean
            If Key Is Nothing Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, NameOf(Key)), NameOf(Key))
            End If
            Return m_KeyedNodesHash.ContainsKey(Key)
        End Function

        Public Overloads Sub Remove(ByVal Key As String)
            Dim node As Node = Nothing
            If m_KeyedNodesHash.TryGetValue(Key, node) Then
                Debug.Assert(node IsNot Nothing)

                'Adjust the ForEach iterators
                AdjustEnumeratorsOnNodeRemoved(node) 'Must be done before the prev/next pointers are removed

                'Remove the item from the list and hash table (we know it has a key)
                Debug.Assert(node.m_Key IsNot Nothing, "How can that be?  We just found it by its key.")
                m_KeyedNodesHash.Remove(Key)
                m_ItemsList.RemoveNode(node)

                'Remove prev/next pointers because the iterators will be thrown off if this isn't done.  Also it's easier for debugging.
                node.m_Prev = Nothing
                node.m_Next = Nothing
            Else
                Debug.Assert(node Is Nothing)
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, NameOf(Key)), NameOf(Key))
            End If
        End Sub

        Public Overloads Sub Remove(ByVal Index As Integer)
            IndexCheck(Index)
            Dim node As Node = m_ItemsList.RemoveAt(Index - 1) '0 based
            Debug.Assert(node IsNot Nothing, "Should have thrown exception rather than return Nothing")

            AdjustEnumeratorsOnNodeRemoved(node) 'Must be done before the prev/next pointers are removed

            'Remove from the hash table if it has a key
            If node.m_Key IsNot Nothing Then
                m_KeyedNodesHash.Remove(node.m_Key)
            End If

            'Remove prev/next pointers because the iterators will be thrown off if this isn't done.  Also it's easier for debugging.
            node.m_Prev = Nothing
            node.m_Next = Nothing
        End Sub

        Default Public Overloads ReadOnly Property Item(ByVal Index As Integer) As Object
            'This method uses 1 based arrays.
            Get
                IndexCheck(Index)
                Dim node As Node = m_ItemsList.Item(Index - 1)
                Debug.Assert(node IsNot Nothing, "Should have thrown rather than returning Nothing")
                Return node.m_Value
            End Get
        End Property

        Default Public Overloads ReadOnly Property Item(ByVal Key As String) As Object
            Get
                If Key Is Nothing Then
                    'Backwards compat - throw IndexOutOfRange if Key = Nothing
                    Throw New IndexOutOfRangeException(SR.Argument_CollectionIndex)
                End If

                Dim node As Node = Nothing
                If Not m_KeyedNodesHash.TryGetValue(Key, node) Then
                    Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, "Index"), NameOf(Key))
                End If
                Debug.Assert(node IsNot Nothing)

                Return node.m_Value
            End Get
        End Property

        <System.ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Advanced)>
        Default Public Overloads ReadOnly Property Item(ByVal Index As Object) As Object
            Get
                If (TypeOf Index Is String) OrElse (TypeOf Index Is Char) OrElse (TypeOf Index Is Char()) Then
                    ' Index is string and is being treated as key
                    Dim key As String = CStr(Index)
                    Return Me.Item(key)
                Else
                    ' Index is being treated as numeric expression
                    Dim indexValue As Integer
                    Try
                        indexValue = CInt(Index)
                    Catch ex As StackOverflowException
                        Throw ex
                    Catch ex As OutOfMemoryException
                        Throw ex
                    Catch ex As System.Threading.ThreadAbortException
                        Throw ex
                    Catch
                        Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, NameOf(Index)), NameOf(Index))
                    End Try

                    Return Me.Item(indexValue)
                End If
            End Get
        End Property

        Public ReadOnly Property Count() As Integer
            Get
                Return m_ItemsList.Count
            End Get
        End Property

        Public Function GetEnumerator() As IEnumerator

            ' Remove Dead Iterators if any from Iterator list m_Iterators
            Dim oldWeakref As WeakReference
            Dim i As Integer = m_Iterators.Count - 1

            While (i >= 0)
                oldWeakref = CType(m_Iterators(i), WeakReference)
                If Not oldWeakref.IsAlive Then
                    m_Iterators.RemoveAt(i)
                End If
                i -= 1
            End While

            ' Create A New Iterator, add to Iterator List and return
            Dim enumerator As ForEachEnum = New ForEachEnum(Me)
            Dim weakref As WeakReference = New WeakReference(enumerator)
            enumerator.WeakRef = weakref
            m_Iterators.Add(weakref)
            Return enumerator
        End Function

        Friend Sub RemoveIterator(ByVal weakref As WeakReference)
            m_Iterators.Remove(weakref)
        End Sub

        Friend Sub AddIterator(ByVal weakref As WeakReference)
            m_Iterators.Add(weakref)
        End Sub

        'Returns the first node in the linked list
        Friend Function GetFirstListNode() As Node
            Return m_ItemsList.GetFirstListNode()
        End Function

        Friend NotInheritable Class Node
            'Constructor.  Key or Value may be Nothing
            Friend Sub New(ByVal Key As String, ByVal Value As Object)
                m_Value = Value
                m_Key = Key
            End Sub

            Friend m_Value As Object 'The value.
            Friend m_Key As String   'The key.  If Nothing, the collection item has no user-specified key.
            Friend m_Next As Node    'Doubly-linked list pointer to the next node
            Friend m_Prev As Node    'Doubly-linked list pointer to the previous node
        End Class 'Node

        ''' <summary>
        ''' Debugger proxy for the Collection class.  Provides a view of the collection 
        ''' that just displays the collection contents. 
        ''' </summary>
        Friend NotInheritable Class CollectionDebugView
            <DebuggerBrowsable(DebuggerBrowsableState.Never)>
            Private m_InstanceBeingWatched As Collection

            Public Sub New(ByVal RealClass As Collection)
                m_InstanceBeingWatched = RealClass
            End Sub

            'Returns an array with all items in the collection.  
            <DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>
            Public ReadOnly Property Items() As Object()
                Get
                    Dim count As Integer = m_InstanceBeingWatched.Count
                    If count = 0 Then
                        Return Nothing
                    End If

                    Dim results(count) As Object
                    results(0) = SR.EmptyPlaceHolderMessage

                    For Index As Integer = 1 To count
                        Dim newNode As Node = m_InstanceBeingWatched.InternalItemsList.Item(Index - 1)
                        results(Index) = New KeyValuePair(newNode.m_Key, newNode.m_Value)
                    Next Index
                    Return results
                End Get
            End Property
        End Class

        ''' <summary>
        ''' Initialize all internal structures to set up an empty collection.
        ''' </summary>
        ''' <param name="CultureInfo">the culture info to use for key comparisons for the lifetime of this collection.</param>
        ''' <param name="StartingHashCapacity"></param>
        Private Sub Initialize(ByVal CultureInfo As CultureInfo, Optional ByVal StartingHashCapacity As Integer = 0)
            Debug.Assert(CultureInfo IsNot Nothing)

            If StartingHashCapacity > 0 Then
                m_KeyedNodesHash = New Generic.Dictionary(Of String, Node)(StartingHashCapacity, StringComparer.Create(CultureInfo, ignoreCase:=True))
            Else
                m_KeyedNodesHash = New Generic.Dictionary(Of String, Node)(StringComparer.Create(CultureInfo, ignoreCase:=True))
            End If
            m_ItemsList = New FastList()
            m_Iterators = New List(Of Object)
            m_CultureInfo = CultureInfo
        End Sub

        Private NotInheritable Class FastList
            Private m_StartOfList As Node
            Private m_EndOfList As Node
            Private m_Count As Integer

            Friend Sub New()
                MyBase.New()
            End Sub

            Friend Sub Add(ByVal Node As Node)
                If m_StartOfList Is Nothing Then
                    m_StartOfList = Node
                Else
                    m_EndOfList.m_Next = Node
                    Node.m_Prev = m_EndOfList
                End If
                m_EndOfList = Node
                m_Count += 1
            End Sub

            'Searches for a given value in the list.  Returns its node's index or -1 if not found.
            Friend Function IndexOfValue(ByVal Value As Object) As Integer
                Dim currentNode As Node = m_StartOfList
                Dim index As Integer = 0

                While Not currentNode Is Nothing
                    If DataIsEqual(currentNode.m_Value, Value) Then
                        Return index
                    End If
                    currentNode = currentNode.m_Next
                    index += 1
                End While
                Return -1
            End Function

            Friend Sub RemoveNode(ByVal NodeToBeDeleted As Node)
                Debug.Assert(NodeToBeDeleted IsNot Nothing, "How can we remove a non-existent node ?")
                DeleteNode(NodeToBeDeleted, NodeToBeDeleted.m_Prev)
            End Sub

            'Removes the node at the given index.
            '  Returns the node that was removed
            Friend Function RemoveAt(ByVal Index As Integer) As Node
                Dim currentNode As Node = m_StartOfList
                Dim currentIndex As Integer = 0
                Dim prevNode As Node = Nothing

                While currentIndex < Index AndAlso (currentNode IsNot Nothing)
                    prevNode = currentNode
                    currentNode = currentNode.m_Next
                    currentIndex += 1
                End While

                If (currentNode Is Nothing) Then
                    Throw New ArgumentOutOfRangeException(NameOf(Index))
                End If

                DeleteNode(currentNode, prevNode)
                Return currentNode
            End Function

            Friend Function Count() As Integer
                Return m_Count
            End Function

            Friend Sub Clear()
                m_StartOfList = Nothing
                m_EndOfList = Nothing
                m_Count = 0
            End Sub

            'Retrieves the node at the given index (0-based)
            Friend ReadOnly Property Item(ByVal Index As Integer) As Node
                Get
                    Dim node As Node = GetNodeAtIndex(Index)
                    If (node Is Nothing) Then
                        Throw New ArgumentOutOfRangeException(NameOf(Index))
                    End If
                    Return node
                End Get
            End Property

            Friend Sub Insert(ByVal Index As Integer, ByVal Node As Node)
                Dim prevNode As Node = Nothing

                ' (Index > m_Count) is here for a reason. We allow insertion immediately beyond the end of the
                ' list i.e. if there are 0 to m_Count -1 elements, we allow insertion into the
                ' m_Count index
                If (Index < 0) OrElse (Index > m_Count) Then
                    Throw New ArgumentOutOfRangeException(NameOf(Index))
                End If

                Dim nodeAtIndex As Node = GetNodeAtIndex(Index, prevNode) 'Note: PrevNode passed ByRef
                Insert(Node, prevNode, nodeAtIndex)
            End Sub

            'Inserts a node into the list.
            '  The item is inserted before NodeToInsertBefore (may not be Nothing).
            Friend Sub InsertBefore(ByVal Node As Node, ByVal NodeToInsertBefore As Node)
                Debug.Assert(NodeToInsertBefore IsNot Nothing, "FastList.InsertBefore: NodeToInsertBefore may not be nothing")
                Insert(Node, NodeToInsertBefore.m_Prev, NodeToInsertBefore)
            End Sub


            'Inserts a node into the list.
            '  The item is inserted after NodeToInsertAfter (may not be Nothing).
            Friend Sub InsertAfter(ByVal Node As Node, ByVal NodeToInsertAfter As Node)
                Debug.Assert(NodeToInsertAfter IsNot Nothing, "FastList.InsertAfter: NodeToInsertAfter may not be nothing")
                Insert(Node, NodeToInsertAfter, NodeToInsertAfter.m_Next)
            End Sub

            'Returns the first node in the list
            Friend Function GetFirstListNode() As Node
                Return m_StartOfList
            End Function

            Private Function DataIsEqual(ByVal obj1 As Object, ByVal obj2 As Object) As Boolean
                If obj1 Is obj2 Then
                    Return True
                End If

                If obj1.GetType() Is obj2.GetType() Then
                    Return Object.Equals(obj1, obj2)
                Else
                    Return False
                End If
            End Function

            Private Function GetNodeAtIndex(ByVal Index As Integer, Optional ByRef PrevNode As Node = Nothing) As Node
                Dim currentNode As Node = m_StartOfList
                Dim currentIndex As Integer = 0
                PrevNode = Nothing

                While currentIndex < Index AndAlso (currentNode IsNot Nothing)
                    PrevNode = currentNode
                    currentNode = currentNode.m_Next
                    currentIndex += 1
                End While

                Return currentNode
            End Function

            'Inserts the given node into the list between the two given nodes (may be Nothing at the beginning/end of the list)
            Private Sub Insert(ByVal Node As Node, ByVal PrevNode As Node, ByVal CurrentNode As Node)
                Node.m_Next = CurrentNode

                If Not CurrentNode Is Nothing Then
                    CurrentNode.m_Prev = Node
                End If

                If PrevNode Is Nothing Then
                    m_StartOfList = Node
                Else
                    PrevNode.m_Next = Node
                    Node.m_Prev = PrevNode
                End If

                If Node.m_Next Is Nothing Then
                    m_EndOfList = Node
                End If

                m_Count += 1
            End Sub

            Private Sub DeleteNode(ByVal NodeToBeDeleted As Node, ByVal PrevNode As Node)
                Debug.Assert(NodeToBeDeleted IsNot Nothing, "How can we delete a non-existent node ?")

                If PrevNode Is Nothing Then ' are we are deleting the first node ?

                    Debug.Assert(NodeToBeDeleted Is m_StartOfList, "How can any node besides the first node not have a previous node ?")

                    m_StartOfList = m_StartOfList.m_Next

                    If m_StartOfList Is Nothing Then
                        m_EndOfList = Nothing
                    Else
                        m_StartOfList.m_Prev = Nothing
                    End If
                Else
                    PrevNode.m_Next = NodeToBeDeleted.m_Next
                    If PrevNode.m_Next Is Nothing Then
                        m_EndOfList = PrevNode
                    Else
                        PrevNode.m_Next.m_Prev = PrevNode
                    End If
                End If
                m_Count -= 1
            End Sub
        End Class 'FastList

        'NOTE:  This structure exists so that the debugger windows can show the items in a collection.  See the Items property.
        Private Structure KeyValuePair
            Private m_Key As Object
            Private m_Value As Object

            Friend Sub New(ByVal NewKey As Object, ByVal NewValue As Object)
                m_Key = NewKey
                m_Value = NewValue
            End Sub

            Public ReadOnly Property Key() As Object
                Get
                    Return m_Key
                End Get
            End Property

            Public ReadOnly Property Value() As Object
                Get
                    Return m_Value
                End Get
            End Property
        End Structure

        Private Sub AdjustEnumeratorsOnNodeInserted(ByVal NewNode As Node)
            AdjustEnumeratorsHelper(NewNode, ForEachEnum.AdjustIndexType.Insert)
        End Sub

        Private Sub AdjustEnumeratorsOnNodeRemoved(ByVal RemovedNode As Node)
            AdjustEnumeratorsHelper(RemovedNode, ForEachEnum.AdjustIndexType.Remove)
        End Sub

        Private Sub AdjustEnumeratorsHelper(ByVal NewOrRemovedNode As Node, ByVal Type As ForEachEnum.AdjustIndexType)
            Debug.Assert(NewOrRemovedNode IsNot Nothing, "AdjustIndexes: Node shouldn't be Nothing")
            Dim weakref As WeakReference
            Dim i As Integer = m_Iterators.Count - 1

            While (i >= 0)
                weakref = CType(m_Iterators(i), WeakReference)
                If weakref.IsAlive Then
                    Dim enumerator As ForEachEnum = CType(weakref.Target, ForEachEnum)
                    If Not enumerator Is Nothing Then
                        enumerator.Adjust(NewOrRemovedNode, Type)  '1 based
                    End If
                Else
                    m_Iterators.RemoveAt(i)
                End If

                i -= 1
            End While
        End Sub

        Private Sub IndexCheck(ByVal Index As Integer)
            If (Index < 1 OrElse Index > m_ItemsList.Count) Then
                Throw New IndexOutOfRangeException(SR.Argument_CollectionIndex)
            End If
        End Sub

        Private Function InternalItemsList() As FastList
            Return m_ItemsList
        End Function

#Region "Interface Implementation"

        '*****************************************************************************
        'These methods are 0 based.
        '*****************************************************************************
        Private Function ICollectionGetEnumerator() As IEnumerator Implements ICollection.GetEnumerator
            Return GetEnumerator()
        End Function

        Private ReadOnly Property ICollectionCount() As Integer Implements ICollection.Count
            Get
                Return m_ItemsList.Count
            End Get
        End Property

        Private ReadOnly Property ICollectionIsSynchronized() As Boolean Implements ICollection.IsSynchronized
            Get
                Return False
            End Get
        End Property

        Private ReadOnly Property ICollectionSyncRoot() As Object Implements ICollection.SyncRoot
            Get
                Return Me
            End Get
        End Property

        Private ReadOnly Property IListIsFixedSize() As Boolean Implements IList.IsFixedSize
            Get
                Return False
            End Get
        End Property

        Private ReadOnly Property IListIsReadOnly() As Boolean Implements IList.IsReadOnly
            Get
                Return False
            End Get
        End Property

        Private Sub ICollectionCopyTo(ByVal [array] As System.Array, ByVal index As Integer) Implements ICollection.CopyTo
            If [array] Is Nothing Then
                Throw New ArgumentNullException(NameOf(array), SR.Format(SR.Argument_InvalidNullValue1, NameOf(array)))
            End If

            If [array].Rank <> 1 Then
                Throw New ArgumentException(SR.Format(SR.Argument_RankEQOne1, NameOf(array)), NameOf(array))
            End If

            If (index < 0) OrElse ([array].Length - index < Count) Then
                Throw New ArgumentException(SR.Format(SR.Argument_InvalidValue1, NameOf(index)), NameOf(index))
            End If

            Dim i As Integer
            Dim objArray As Object() = TryCast(array, Object())

            If objArray IsNot Nothing Then
                For i = 1 To Count
                    objArray(index + i - 1) = Me.Item(i)
                Next i
            Else
                For i = 1 To Count
                    [array].SetValue(Me.Item(i), index + i - 1)
                Next i
            End If
        End Sub

        Private Function IListAdd(ByVal value As Object) As Integer Implements IList.Add
            Add(value, Nothing)
            Return m_ItemsList.Count - 1 'IList is 0 based.  Return a 0-based index.  If m_ItemsList.Count is zero, we threw during Add() so no need to gate the Count = 0 case.
        End Function

        Private Sub IListInsert(ByVal index As Integer, ByVal value As Object) Implements IList.Insert
            Dim newNode As New Node(Nothing, value)
            m_ItemsList.Insert(index, newNode) 'FastList is 0-indexed just like IList, so no transformation of "index" needed
            'No key, so no need to add to the hash table.
            'Adjust the ForEach iterators
            AdjustEnumeratorsOnNodeInserted(newNode)
        End Sub

        Private Sub IListRemoveAt(ByVal index As Integer) Implements IList.RemoveAt
            Dim node As Node = m_ItemsList.RemoveAt(index)    '0 based
            Debug.Assert(node IsNot Nothing, "Should have thrown exception rather than return Nothing")

            AdjustEnumeratorsOnNodeRemoved(node) 'Adjust the ForEach iterators
            If node.m_Key IsNot Nothing Then
                m_KeyedNodesHash.Remove(node.m_Key)
            End If

            node.m_Prev = Nothing
            node.m_Next = Nothing
        End Sub

        Private Sub IListRemove(ByVal value As Object) Implements IList.Remove
            Dim index As Integer
            index = IListIndexOf(value)
            If index <> -1 Then
                IListRemoveAt(index)
            End If
            'No exception thrown if not found
        End Sub

        Private Sub IListClear() Implements IList.Clear
            Clear()
        End Sub

        Private Property IListItem(ByVal index As Integer) As Object Implements IList.Item
            Get
                Dim node As Node

                node = m_ItemsList.Item(index)
                Return node.m_Value
            End Get

            Set(ByVal value As Object)
                Dim node As Node = m_ItemsList.Item(index)
                node.m_Value = value
            End Set
        End Property

        Private Function IListContains(ByVal value As Object) As Boolean Implements IList.Contains
            Return (IListIndexOf(value) <> -1)
        End Function

        Private Function IListIndexOf(ByVal value As Object) As Integer Implements IList.IndexOf
            Return m_ItemsList.IndexOfValue(value)
        End Function

#End Region
    End Class 'Collection
End Namespace

