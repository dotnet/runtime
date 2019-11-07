' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections
Imports System.Diagnostics
Imports Microsoft.VisualBasic.CompilerServices

Namespace Microsoft.VisualBasic

    'This is in the helpers directory but not in the compilerservices namespace

    'The ForEachEnum class is publicly exposed through Collection.GetEnumerator().
    <System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)>
    Friend NotInheritable Class ForEachEnum
        Implements IEnumerator
        Implements IDisposable

        'No Finalize - this is intentional because we do not want to invoke RemoveIterator in finalize
        '              because of threading and synchronization issues involved which would then cause
        '              perf degrade.

        Private mDisposed As Boolean = False

        'The collection this enumerator is enumerating over
        Private mCollectionObject As Microsoft.VisualBasic.Collection

        'The current element being iterated.  Note: this element may no longer be in the list,
        '  (if it was deleted), so do *not* assume that its previous and next pointers are valid.
        Private mCurrent As Collection.Node

        'The next item to enumerate.  This is always updated on MoveNext and is used to determine where
        '  the enumeration goes next (not mCurrent).
        Private mNext As Collection.Node

        'A flag indicating that we are ready to start enumerating but have not done so yet (allows us
        '  to delay fixing mNext until the next MoveNext).
        Private mAtBeginning As Boolean

        Friend WeakRef As WeakReference

        Private Sub Dispose() Implements IDisposable.Dispose
            If Not mDisposed Then
                mCollectionObject.RemoveIterator(WeakRef)
                mDisposed = True
            End If
            mCurrent = Nothing
            mNext = Nothing
        End Sub

        Public Sub New(ByVal coll As Microsoft.VisualBasic.Collection)
            MyBase.New()
            mCollectionObject = coll

            Reset()
        End Sub

        Public Function MoveNext() As Boolean Implements IEnumerator.MoveNext
            If mDisposed Then
                Return False
            End If

            If mAtBeginning Then
                'We haven't started iterating yet.  Start at the beginning.

                mAtBeginning = False
                mNext = mCollectionObject.GetFirstListNode()
            End If

            Debug.Assert(Not mAtBeginning)
            If mNext Is Nothing Then
                Dispose()
                Return False
            End If

            mCurrent = mNext
            If mCurrent IsNot Nothing Then
                mNext = mCurrent.m_Next
                Return True
            Else
                Debug.Assert(mNext Is Nothing)
                Dispose()
                Return False
            End If
        End Function

        Public Sub Reset() Implements IEnumerator.Reset
            If mDisposed Then
                mCollectionObject.AddIterator(WeakRef)
                mDisposed = False
            End If

            mCurrent = Nothing
            mNext = Nothing
            mAtBeginning = True
        End Sub

        Public ReadOnly Property Current() As Object Implements IEnumerator.Current
            Get
                If mCurrent Is Nothing Then
                    Return Nothing
                Else
                    Return mCurrent.m_Value
                End If
            End Get
        End Property

        Friend Enum AdjustIndexType
            Insert
            Remove
        End Enum

        'Adjusts the enumerator to account for newly-inserted or removed items in/from the collection.
        '  For insertion, this call must be made *after* the insertion has taken place.  For deletion, 
        '  this call must have been made before the next/prev pointers in the deleted node have been
        '  invalidated (they must still be pointing to the values before the deletion).
        Public Sub Adjust(ByVal Node As Collection.Node, ByVal Type As AdjustIndexType)

            If Node Is Nothing Then
                Debug.Fail("Node shouldn't be nothing")
                Exit Sub 'defensive
            End If

            If mDisposed Then
                'Nothing to do
                Exit Sub
            End If

            Select Case Type
                Case AdjustIndexType.Insert
                    Debug.Assert(Node IsNot mCurrent, "If we just inserted Node, then it couldn't be the current node because it's not in the list yet")

                    'mCurrent may not necessarily be still in the list, so we have to be wary of using mCurrent.m_Next.  However, if
                    '  there is a current node, and its next is pointing to Node, then mCurrent must still be in the list (since Node wasn't
                    '  in the list before).  So in this case we'll go ahead and set our mNext to the newly-inserted node.
                    'It would seem to make sense also to set mNext to the new node if mNext is Nothing (i.e., we're at the end of the list), but
                    '  this would be a difference in RTM/Everett behavior that doesn't seem warranted.
                    If mCurrent IsNot Nothing AndAlso Node Is mCurrent.m_Next Then
                        'The new node was inserted right after our current node.
                        '  Iterate through it next.
                        mNext = Node
                    End If

                    'Note that the case of inserting at the beginning before we've
                    '  iterated through any nodes will be handled in MoveNext with the
                    '  mAtBeginning flag.

                Case AdjustIndexType.Remove
                    If Node Is mCurrent Then
                        'Current node was removed.  No need to do anything, because we want GetCurrent() to continue to
                        '  return this same node's data until the next MoveNext().
                    ElseIf Node Is mNext Then
                        'The next node was removed.  Make our next node to iterate through
                        '  be the one after that instead
                        mNext = mNext.m_Next
                    End If
                Case Else
                    Debug.Fail("Unexpected adjustment type in enumerator")
            End Select
        End Sub

        'Should be called if the list this is enumerating is cleared.  It will set the
        '  enumerator past the end of the list (nothing more to enumerate).
        Friend Sub AdjustOnListCleared()
            mNext = Nothing
        End Sub

    End Class

End Namespace
