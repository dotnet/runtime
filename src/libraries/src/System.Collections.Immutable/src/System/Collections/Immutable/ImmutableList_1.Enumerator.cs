// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;

namespace System.Collections.Immutable
{
    public sealed partial class ImmutableList<T>
    {
        /// <summary>
        /// Enumerates the contents of a binary tree.
        /// </summary>
        /// <remarks>
        /// This struct can and should be kept in exact sync with the other binary tree enumerators: 
        /// <see cref="ImmutableList{T}.Enumerator"/>, <see cref="ImmutableSortedDictionary{TKey, TValue}.Enumerator"/>, and <see cref="ImmutableSortedSet{T}.Enumerator"/>.
        /// 
        /// CAUTION: when this enumerator is actually used as a valuetype (not boxed) do NOT copy it by assigning to a second variable 
        /// or by passing it to another method.  When this enumerator is disposed of it returns a mutable reference type stack to a resource pool,
        /// and if the value type enumerator is copied (which can easily happen unintentionally if you pass the value around) there is a risk
        /// that a stack that has already been returned to the resource pool may still be in use by one of the enumerator copies, leading to data
        /// corruption and/or exceptions.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public struct Enumerator : IEnumerator<T>, ISecurePooledObjectUser, IStrongEnumerator<T>
        {
            /// <summary>
            /// The resource pool of reusable mutable stacks for purposes of enumeration.
            /// </summary>
            /// <remarks>
            /// We utilize this resource pool to make "allocation free" enumeration achievable.
            /// </remarks>
            private static readonly SecureObjectPool<Stack<RefAsValueType<Node>>, Enumerator> s_EnumeratingStacks =
                new SecureObjectPool<Stack<RefAsValueType<Node>>, Enumerator>();

            /// <summary>
            /// The builder being enumerated, if applicable.
            /// </summary>
            private readonly Builder _builder;

            /// <summary>
            /// A unique ID for this instance of this enumerator.
            /// Used to protect pooled objects from use after they are recycled.
            /// </summary>
            private readonly int _poolUserId;

            /// <summary>
            /// The starting index of the collection at which to begin enumeration.
            /// </summary>
            private readonly int _startIndex;

            /// <summary>
            /// The number of elements to include in the enumeration.
            /// </summary>
            private readonly int _count;

            /// <summary>
            /// The number of elements left in the enumeration.
            /// </summary>
            private int _remainingCount;

            /// <summary>
            /// A value indicating whether this enumerator walks in reverse order.
            /// </summary>
            private bool _reversed;

            /// <summary>
            /// The set being enumerated.
            /// </summary>
            private Node _root;

            /// <summary>
            /// The stack to use for enumerating the binary tree.
            /// </summary>
            private SecurePooledObject<Stack<RefAsValueType<Node>>> _stack;

            /// <summary>
            /// The node currently selected.
            /// </summary>
            private Node _current;

            /// <summary>
            /// The version of the builder (when applicable) that is being enumerated.
            /// </summary>
            private int _enumeratingBuilderVersion;

            /// <summary>
            /// Initializes an <see cref="Enumerator"/> structure.
            /// </summary>
            /// <param name="root">The root of the set to be enumerated.</param>
            /// <param name="builder">The builder, if applicable.</param>
            /// <param name="startIndex">The index of the first element to enumerate.</param>
            /// <param name="count">The number of elements in this collection.</param>
            /// <param name="reversed"><c>true</c> if the list should be enumerated in reverse order.</param>
            internal Enumerator(Node root, Builder builder = null, int startIndex = -1, int count = -1, bool reversed = false)
            {
                Requires.NotNull(root, nameof(root));
                Requires.Range(startIndex >= -1, nameof(startIndex));
                Requires.Range(count >= -1, nameof(count));
                Requires.Argument(reversed || count == -1 || (startIndex == -1 ? 0 : startIndex) + count <= root.Count);
                Requires.Argument(!reversed || count == -1 || (startIndex == -1 ? root.Count - 1 : startIndex) - count + 1 >= 0);

                _root = root;
                _builder = builder;
                _current = null;
                _startIndex = startIndex >= 0 ? startIndex : (reversed ? root.Count - 1 : 0);
                _count = count == -1 ? root.Count : count;
                _remainingCount = _count;
                _reversed = reversed;
                _enumeratingBuilderVersion = builder != null ? builder.Version : -1;
                _poolUserId = SecureObjectPool.NewId();
                _stack = null;
                if (_count > 0)
                {
                    if (!s_EnumeratingStacks.TryTake(this, out _stack))
                    {
                        _stack = s_EnumeratingStacks.PrepNew(this, new Stack<RefAsValueType<Node>>(root.Height));
                    }

                    this.ResetStack();
                }
            }

            /// <inheritdoc/>
            int ISecurePooledObjectUser.PoolUserId => _poolUserId;

            /// <summary>
            /// The current element.
            /// </summary>
            public T Current
            {
                get
                {
                    this.ThrowIfDisposed();
                    if (_current != null)
                    {
                        return _current.Value;
                    }

                    throw new InvalidOperationException();
                }
            }

            /// <summary>
            /// The current element.
            /// </summary>
            object System.Collections.IEnumerator.Current => this.Current;

            /// <summary>
            /// Disposes of this enumerator and returns the stack reference to the resource pool.
            /// </summary>
            public void Dispose()
            {
                _root = null;
                _current = null;
                Stack<RefAsValueType<Node>> stack;
                if (_stack != null && _stack.TryUse(ref this, out stack))
                {
                    stack.ClearFastWhenEmpty();
                    s_EnumeratingStacks.TryAdd(this, _stack);
                }

                _stack = null;
            }

            /// <summary>
            /// Advances enumeration to the next element.
            /// </summary>
            /// <returns>A value indicating whether there is another element in the enumeration.</returns>
            public bool MoveNext()
            {
                this.ThrowIfDisposed();
                this.ThrowIfChanged();

                if (_stack != null)
                {
                    var stack = _stack.Use(ref this);
                    if (_remainingCount > 0 && stack.Count > 0)
                    {
                        Node n = stack.Pop().Value;
                        _current = n;
                        this.PushNext(this.NextBranch(n));
                        _remainingCount--;
                        return true;
                    }
                }

                _current = null;
                return false;
            }

            /// <summary>
            /// Restarts enumeration.
            /// </summary>
            public void Reset()
            {
                this.ThrowIfDisposed();

                _enumeratingBuilderVersion = _builder != null ? _builder.Version : -1;
                _remainingCount = _count;
                if (_stack != null)
                {
                    this.ResetStack();
                }
            }

            /// <summary>Resets the stack used for enumeration.</summary>
            private void ResetStack()
            {
                var stack = _stack.Use(ref this);
                stack.ClearFastWhenEmpty();

                var node = _root;
                var skipNodes = _reversed ? _root.Count - _startIndex - 1 : _startIndex;
                while (!node.IsEmpty && skipNodes != this.PreviousBranch(node).Count)
                {
                    if (skipNodes < this.PreviousBranch(node).Count)
                    {
                        stack.Push(new RefAsValueType<Node>(node));
                        node = this.PreviousBranch(node);
                    }
                    else
                    {
                        skipNodes -= this.PreviousBranch(node).Count + 1;
                        node = this.NextBranch(node);
                    }
                }

                if (!node.IsEmpty)
                {
                    stack.Push(new RefAsValueType<Node>(node));
                }
            }

            /// <summary>
            /// Obtains the right branch of the given node (or the left, if walking in reverse).
            /// </summary>
            private Node NextBranch(Node node) => _reversed ? node.Left : node.Right;

            /// <summary>
            /// Obtains the left branch of the given node (or the right, if walking in reverse).
            /// </summary>
            private Node PreviousBranch(Node node) => _reversed ? node.Right : node.Left;

            /// <summary>
            /// Throws an <see cref="ObjectDisposedException"/> if this enumerator has been disposed.
            /// </summary>
            private void ThrowIfDisposed()
            {
                // Since this is a struct, copies might not have been marked as disposed.
                // But the stack we share across those copies would know.
                // This trick only works when we have a non-null stack.
                // For enumerators of empty collections, there isn't any natural
                // way to know when a copy of the struct has been disposed of.

                if (_root == null || (_stack != null && !_stack.IsOwned(ref this)))
                {
                    Requires.FailObjectDisposed(this);
                }
            }

            /// <summary>
            /// Throws an exception if the underlying builder's contents have been changed since enumeration started.
            /// </summary>
            /// <exception cref="System.InvalidOperationException">Thrown if the collection has changed.</exception>
            private void ThrowIfChanged()
            {
                if (_builder != null && _builder.Version != _enumeratingBuilderVersion)
                {
                    throw new InvalidOperationException(SR.CollectionModifiedDuringEnumeration);
                }
            }

            /// <summary>
            /// Pushes this node and all its Left descendants onto the stack.
            /// </summary>
            /// <param name="node">The starting node to push onto the stack.</param>
            private void PushNext(Node node)
            {
                Requires.NotNull(node, nameof(node));
                if (!node.IsEmpty)
                {
                    var stack = _stack.Use(ref this);
                    while (!node.IsEmpty)
                    {
                        stack.Push(new RefAsValueType<Node>(node));
                        node = this.PreviousBranch(node);
                    }
                }
            }
        }
    }
}
