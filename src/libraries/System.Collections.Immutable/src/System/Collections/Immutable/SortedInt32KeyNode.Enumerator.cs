// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;

namespace System.Collections.Immutable
{
    internal sealed partial class SortedInt32KeyNode<TValue>
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
        public struct Enumerator : IEnumerator<KeyValuePair<int, TValue>>, ISecurePooledObjectUser
        {
            /// <summary>
            /// The resource pool of reusable mutable stacks for purposes of enumeration.
            /// </summary>
            /// <remarks>
            /// We utilize this resource pool to make "allocation free" enumeration achievable.
            /// </remarks>
            private static readonly SecureObjectPool<Stack<RefAsValueType<SortedInt32KeyNode<TValue>>>, Enumerator> s_enumeratingStacks =
                new SecureObjectPool<Stack<RefAsValueType<SortedInt32KeyNode<TValue>>>, Enumerator>();

            /// <summary>
            /// A unique ID for this instance of this enumerator.
            /// Used to protect pooled objects from use after they are recycled.
            /// </summary>
            private readonly int _poolUserId;

            /// <summary>
            /// The set being enumerated.
            /// </summary>
            private SortedInt32KeyNode<TValue> _root;

            /// <summary>
            /// The stack to use for enumerating the binary tree.
            /// </summary>
            private SecurePooledObject<Stack<RefAsValueType<SortedInt32KeyNode<TValue>>>> _stack;

            /// <summary>
            /// The node currently selected.
            /// </summary>
            private SortedInt32KeyNode<TValue> _current;

            /// <summary>
            /// Initializes an <see cref="Enumerator"/> structure.
            /// </summary>
            /// <param name="root">The root of the set to be enumerated.</param>
            internal Enumerator(SortedInt32KeyNode<TValue> root)
            {
                Requires.NotNull(root, nameof(root));

                _root = root;
                _current = null;
                _poolUserId = SecureObjectPool.NewId();
                _stack = null;
                if (!_root.IsEmpty)
                {
                    if (!s_enumeratingStacks.TryTake(this, out _stack))
                    {
                        _stack = s_enumeratingStacks.PrepNew(this, new Stack<RefAsValueType<SortedInt32KeyNode<TValue>>>(root.Height));
                    }

                    this.PushLeft(_root);
                }
            }

            /// <summary>
            /// The current element.
            /// </summary>
            public KeyValuePair<int, TValue> Current
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

            /// <inheritdoc/>
            int ISecurePooledObjectUser.PoolUserId
            {
                get { return _poolUserId; }
            }

            /// <summary>
            /// The current element.
            /// </summary>
            object IEnumerator.Current
            {
                get { return this.Current; }
            }

            /// <summary>
            /// Disposes of this enumerator and returns the stack reference to the resource pool.
            /// </summary>
            public void Dispose()
            {
                _root = null;
                _current = null;
                Stack<RefAsValueType<SortedInt32KeyNode<TValue>>> stack;
                if (_stack != null && _stack.TryUse(ref this, out stack))
                {
                    stack.ClearFastWhenEmpty();
                    s_enumeratingStacks.TryAdd(this, _stack);
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

                if (_stack != null)
                {
                    var stack = _stack.Use(ref this);
                    if (stack.Count > 0)
                    {
                        SortedInt32KeyNode<TValue> n = stack.Pop().Value;
                        _current = n;
                        this.PushLeft(n.Right);
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

                _current = null;
                if (_stack != null)
                {
                    var stack = _stack.Use(ref this);
                    stack.ClearFastWhenEmpty();
                    this.PushLeft(_root);
                }
            }

            /// <summary>
            /// Throws an <see cref="ObjectDisposedException"/> if this enumerator has been disposed.
            /// </summary>
            internal void ThrowIfDisposed()
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
            /// Pushes this node and all its Left descendants onto the stack.
            /// </summary>
            /// <param name="node">The starting node to push onto the stack.</param>
            private void PushLeft(SortedInt32KeyNode<TValue> node)
            {
                Requires.NotNull(node, nameof(node));
                var stack = _stack.Use(ref this);
                while (!node.IsEmpty)
                {
                    stack.Push(new RefAsValueType<SortedInt32KeyNode<TValue>>(node));
                    node = node.Left;
                }
            }
        }
    }
}
