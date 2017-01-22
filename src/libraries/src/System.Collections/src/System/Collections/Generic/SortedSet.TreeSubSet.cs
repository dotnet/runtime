// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.Serialization;

namespace System.Collections.Generic
{
    public partial class SortedSet<T>
    {
        /// <summary>
        /// This class represents a subset view into the tree. Any changes to this view
        /// are reflected in the actual tree. It uses the comparer of the underlying tree.
        /// </summary>
        [Serializable]
        internal sealed class TreeSubSet : SortedSet<T>, ISerializable, IDeserializationCallback
        {
            private SortedSet<T> _underlying;
            private T _min, _max;
            // these exist for unbounded collections
            // for instance, you could allow this subset to be defined for i > 10. The set will throw if
            // anything <= 10 is added, but there is no upper bound. These features Head(), Tail(), were punted
            // in the spec, and are not available, but the framework is there to make them available at some point.
            private bool _lBoundActive, _uBoundActive;
            // used to see if the count is out of date

#if DEBUG
            internal override bool versionUpToDate()
            {
                return (_version == _underlying._version);
            }
#endif

            public TreeSubSet(SortedSet<T> Underlying, T Min, T Max, bool lowerBoundActive, bool upperBoundActive)
                : base(Underlying.Comparer)
            {
                _underlying = Underlying;
                _min = Min;
                _max = Max;
                _lBoundActive = lowerBoundActive;
                _uBoundActive = upperBoundActive;
                _root = _underlying.FindRange(_min, _max, _lBoundActive, _uBoundActive); // root is first element within range                                
                _count = 0;
                _version = -1;
                VersionCheckImpl();
            }

            private TreeSubSet(SerializationInfo info, StreamingContext context)
            {
                _siInfo = info;
                OnDeserializationImpl(info);
            }

            internal override bool AddIfNotPresent(T item)
            {
                if (!IsWithinRange(item))
                {
                    throw new ArgumentOutOfRangeException(nameof(item));
                }

                bool ret = _underlying.AddIfNotPresent(item);
                VersionCheck();
#if DEBUG
                Debug.Assert(this.versionUpToDate() && _root == _underlying.FindRange(_min, _max));
#endif

                return ret;
            }

            public override bool Contains(T item)
            {
                VersionCheck();
#if DEBUG
                Debug.Assert(versionUpToDate() && _root == _underlying.FindRange(_min, _max));
#endif
                return base.Contains(item);
            }

            internal override bool DoRemove(T item)
            {
                if (!IsWithinRange(item))
                {
                    return false;
                }

                bool ret = _underlying.Remove(item);
                VersionCheck();
#if DEBUG
                Debug.Assert(versionUpToDate() && _root == _underlying.FindRange(_min, _max));
#endif
                return ret;
            }

            public override void Clear()
            {
                if (_count == 0)
                {
                    return;
                }

                List<T> toRemove = new List<T>();
                BreadthFirstTreeWalk(n => { toRemove.Add(n.Item); return true; });
                while (toRemove.Count != 0)
                {
                    _underlying.Remove(toRemove[toRemove.Count - 1]);
                    toRemove.RemoveAt(toRemove.Count - 1);
                }

                _root = null;
                _count = 0;
                _version = _underlying._version;
            }

            internal override bool IsWithinRange(T item)
            {
                int comp = _lBoundActive ? Comparer.Compare(_min, item) : -1;
                if (comp > 0)
                {
                    return false;
                }

                comp = _uBoundActive ? Comparer.Compare(_max, item) : 1;
                return comp >= 0;
            }

            internal override bool InOrderTreeWalk(TreeWalkPredicate<T> action, bool reverse)
            {
                VersionCheck();

                if (_root == null)
                {
                    return true;
                }

                // The maximum height of a red-black tree is 2*lg(n+1).
                // See page 264 of "Introduction to algorithms" by Thomas H. Cormen
                Stack<Node> stack = new Stack<Node>(2 * (int)SortedSet<T>.Log2(_count + 1)); // this is not exactly right if count is out of date, but the stack can grow
                Node current = _root;
                while (current != null)
                {
                    if (IsWithinRange(current.Item))
                    {
                        stack.Push(current);
                        current = (reverse ? current.Right : current.Left);
                    }
                    else if (_lBoundActive && Comparer.Compare(_min, current.Item) > 0)
                    {
                        current = current.Right;
                    }
                    else
                    {
                        current = current.Left;
                    }
                }

                while (stack.Count != 0)
                {
                    current = stack.Pop();
                    if (!action(current))
                    {
                        return false;
                    }

                    Node node = (reverse ? current.Left : current.Right);
                    while (node != null)
                    {
                        if (IsWithinRange(node.Item))
                        {
                            stack.Push(node);
                            node = (reverse ? node.Right : node.Left);
                        }
                        else if (_lBoundActive && Comparer.Compare(_min, node.Item) > 0)
                        {
                            node = node.Right;
                        }
                        else
                        {
                            node = node.Left;
                        }
                    }
                }
                return true;
            }

            internal override bool BreadthFirstTreeWalk(TreeWalkPredicate<T> action)
            {
                VersionCheck();

                if (_root == null)
                {
                    return true;
                }

                Queue<Node> processQueue = new Queue<Node>();
                processQueue.Enqueue(_root);
                Node current;

                while (processQueue.Count != 0)
                {
                    current = processQueue.Dequeue();
                    if (IsWithinRange(current.Item) && !action(current))
                    {
                        return false;
                    }
                    if (current.Left != null && (!_lBoundActive || Comparer.Compare(_min, current.Item) < 0))
                    {
                        processQueue.Enqueue(current.Left);
                    }
                    if (current.Right != null && (!_uBoundActive || Comparer.Compare(_max, current.Item) > 0))
                    {
                        processQueue.Enqueue(current.Right);
                    }
                }
                return true;
            }

            internal override SortedSet<T>.Node FindNode(T item)
            {
                if (!IsWithinRange(item))
                {
                    return null;
                }

                VersionCheck();
#if DEBUG
                Debug.Assert(this.versionUpToDate() && _root == _underlying.FindRange(_min, _max));
#endif
                return base.FindNode(item);
            }

            // this does indexing in an inefficient way compared to the actual sortedset, but it saves a
            // lot of space
            internal override int InternalIndexOf(T item)
            {
                int count = -1;
                foreach (T i in this)
                {
                    count++;
                    if (Comparer.Compare(item, i) == 0)
                        return count;
                }
#if DEBUG
                Debug.Assert(this.versionUpToDate() && _root == _underlying.FindRange(_min, _max));
#endif
                return -1;
            }

            /// <summary>
            /// Checks whether this subset is out of date, and updates it if necessary.
            /// </summary>
            internal override void VersionCheck() => VersionCheckImpl();

            private void VersionCheckImpl()
            {
                Debug.Assert(_underlying != null);
                if (_version != _underlying._version)
                {
                    _root = _underlying.FindRange(_min, _max, _lBoundActive, _uBoundActive);
                    _version = _underlying._version;
                    _count = 0;
                    InOrderTreeWalk(n => { _count++; return true; });
                }
            }

            // This passes functionality down to the underlying tree, clipping edges if necessary
            // There's nothing gained by having a nested subset. May as well draw it from the base
            // Cannot increase the bounds of the subset, can only decrease it
            public override SortedSet<T> GetViewBetween(T lowerValue, T upperValue)
            {
                if (_lBoundActive && Comparer.Compare(_min, lowerValue) > 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(lowerValue));
                }
                if (_uBoundActive && Comparer.Compare(_max, upperValue) < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(upperValue));
                }
                return (TreeSubSet)_underlying.GetViewBetween(lowerValue, upperValue);
            }

#if DEBUG
            internal override void IntersectWithEnumerable(IEnumerable<T> other)
            {
                base.IntersectWithEnumerable(other);
                Debug.Assert(versionUpToDate() && _root == _underlying.FindRange(_min, _max));
            }
#endif

            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) => GetObjectData(info, context);

            protected override void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                if (info == null)
                {
                    throw new ArgumentNullException(nameof(info));
                }

                info.AddValue(maxName, _max, typeof(T));
                info.AddValue(minName, _min, typeof(T));
                info.AddValue(lBoundActiveName, _lBoundActive);
                info.AddValue(uBoundActiveName, _uBoundActive);

                base.GetObjectData(info, context);
            }

            void IDeserializationCallback.OnDeserialization(Object sender)
            {
                // Don't do anything here as the work has already been done by the constructor
            }

            protected override void OnDeserialization(Object sender) => OnDeserializationImpl(sender);

            private void OnDeserializationImpl(Object sender)
            {
                if (_siInfo == null)
                {
                    throw new SerializationException(SR.Serialization_InvalidOnDeser);
                }

                _comparer = (IComparer<T>)_siInfo.GetValue(ComparerName, typeof(IComparer<T>));
                int savedCount = _siInfo.GetInt32(CountName);
                _max = (T)_siInfo.GetValue(maxName, typeof(T));
                _min = (T)_siInfo.GetValue(minName, typeof(T));
                _lBoundActive = _siInfo.GetBoolean(lBoundActiveName);
                _uBoundActive = _siInfo.GetBoolean(uBoundActiveName);
                _underlying = new SortedSet<T>();

                if (savedCount != 0)
                {
                    T[] items = (T[])_siInfo.GetValue(ItemsName, typeof(T[]));

                    if (items == null)
                    {
                        throw new SerializationException(SR.Serialization_MissingValues);
                    }

                    for (int i = 0; i < items.Length; i++)
                    {
                        _underlying.Add(items[i]);
                    }
                }

                _underlying._version = _siInfo.GetInt32(VersionName);
                _count = _underlying._count;
                _version = _underlying._version - 1;
                VersionCheck(); // this should update the count to be right and update root to be right

                if (_count != savedCount)
                {
                    throw new SerializationException(SR.Serialization_MismatchedCount);
                }

                _siInfo = null;
            }
        }
    }
}
