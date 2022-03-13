' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.

Imports System.Collections.Generic
Imports System.Runtime.InteropServices

Namespace Microsoft.VisualBasic.CompilerServices

    ' Implements a MRU collection for caching dynamic methods used in IDO late binding.
    Friend NotInheritable Class CacheDict(Of TKey, TValue)
        ' The Dictionary to quickly access cached data
        Private ReadOnly _dict As Dictionary(Of TKey, KeyInfo)
        ' MRU sorted linked list
        Private ReadOnly _list As LinkedList(Of TKey)
        ' Maximum size
        Private ReadOnly _maxSize As Integer

        Friend Sub New(ByVal maxSize As Integer)
            _dict = New Dictionary(Of TKey, KeyInfo)
            _list = New LinkedList(Of TKey)
            _maxSize = maxSize
        End Sub

        Friend Sub Add(ByVal key As TKey, ByVal value As TValue)
            Dim info As New KeyInfo
            If _dict.TryGetValue(key, info) Then
                ' If the key is already in the collection, remove it
                _list.Remove(info.List)
            ElseIf (_list.Count = _maxSize) Then
                ' Age out the last element if we hit the max size
                Dim last As LinkedListNode(Of TKey) = _list.Last
                _list.RemoveLast()
                _dict.Remove(last.Value)
            End If

            ' Add the new element
            Dim node As New LinkedListNode(Of TKey)(key)
            _list.AddFirst(node)
            _dict.Item(key) = New KeyInfo(value, node)
        End Sub

        Friend Function TryGetValue(ByVal key As TKey, <Out()> ByRef value As TValue) As Boolean
            Dim info As New KeyInfo
            If _dict.TryGetValue(key, info) Then
                Dim list As LinkedListNode(Of TKey) = info.List
                If (list.Previous IsNot Nothing) Then
                    _list.Remove(list)
                    _list.AddFirst(list)
                End If
                value = info.Value
                Return True
            End If
            value = Nothing
            Return False
        End Function

        ' KeyInfo to store in the dictionary
        Private Structure KeyInfo
            Friend ReadOnly Value As TValue
            Friend ReadOnly List As LinkedListNode(Of TKey)

            Friend Sub New(ByVal v As TValue, ByVal l As LinkedListNode(Of TKey))
                Value = v
                List = l
            End Sub
        End Structure
    End Class

End Namespace
