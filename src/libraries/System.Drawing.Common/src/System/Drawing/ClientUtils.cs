// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security;

namespace System.Drawing
{
    internal static class ClientUtils
    {
        // ExecutionEngineException is obsolete and shouldn't be used (to catch, throw or reference) anymore.
        // Pragma added to prevent converting the "type is obsolete" warning into build error.
#pragma warning disable 618
        public static bool IsCriticalException(Exception ex)
        {
            return ex is NullReferenceException
                    || ex is StackOverflowException
                    || ex is OutOfMemoryException
                    || ex is System.Threading.ThreadAbortException
                    || ex is ExecutionEngineException
                    || ex is IndexOutOfRangeException
                    || ex is AccessViolationException;
        }
#pragma warning restore 618

        public static bool IsSecurityOrCriticalException(Exception ex)
        {
            return (ex is SecurityException) || IsCriticalException(ex);
        }

        /// <summary>
        /// WeakRefCollection - a collection that holds onto weak references.
        ///
        /// Essentially you pass in the object as it is, and under the covers
        /// we only hold a weak reference to the object.
        ///
        /// -----------------------------------------------------------------
        /// !!!IMPORTANT USAGE NOTE!!!
        /// Users of this class should set the RefCheckThreshold property
        /// explicitly or call ScavengeReferences every once in a while to
        /// remove dead references.
        /// Also avoid calling Remove(item). Instead call RemoveByHashCode(item)
        /// to make sure dead refs are removed.
        /// </summary>
        internal sealed class WeakRefCollection : IList
        {
            internal WeakRefCollection() : this(4) { }

            internal WeakRefCollection(int size) => InnerList = new ArrayList(size);

            internal ArrayList InnerList { get; }

            /// <summary>
            /// Indicates the value where the collection should check its items to remove dead weakref left over.
            /// Note: When GC collects weak refs from this collection the WeakRefObject identity changes since its
            ///       Target becomes null. This makes the item unrecognizable by the collection and cannot be
            ///       removed - Remove(item) and Contains(item) will not find it anymore.
            /// A value of int.MaxValue means disabled by default.
            /// </summary>
            public int RefCheckThreshold { get; set; } = int.MaxValue;

            public object? this[int index]
            {
                get
                {
                    if (InnerList[index] is WeakRefObject weakRef && weakRef.IsAlive)
                    {
                        return weakRef.Target;
                    }

                    return null;
                }
                set => InnerList[index] = CreateWeakRefObject(value);
            }

            public void ScavengeReferences()
            {
                int currentIndex = 0;
                int currentCount = Count;
                for (int i = 0; i < currentCount; i++)
                {
                    object? item = this[currentIndex];

                    if (item == null)
                    {
                        InnerList.RemoveAt(currentIndex);
                    }
                    else
                    {
                        // Only incriment if we have not removed the item.
                        currentIndex++;
                    }
                }
            }

            public override bool Equals(object? obj)
            {
                if (!(obj is WeakRefCollection other))
                {
                    return true;
                }

                if (other == null || Count != other.Count)
                {
                    return false;
                }

                for (int i = 0; i < Count; i++)
                {
                    object? thisObj = InnerList[i];
                    object? otherObj = other.InnerList[i];
                    if (thisObj != otherObj)
                    {
                        if (thisObj is null || !thisObj.Equals(otherObj))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            public override int GetHashCode() => base.GetHashCode();

            [return: NotNullIfNotNull("value")]
            private WeakRefObject? CreateWeakRefObject(object? value)
            {
                if (value == null)
                {
                    return null;
                }

                return new WeakRefObject(value);
            }

            private static void Copy(WeakRefCollection sourceList, int sourceIndex, WeakRefCollection destinationList, int destinationIndex, int length)
            {
                if (sourceIndex < destinationIndex)
                {
                    // We need to copy from the back forward to prevent overwrite if source and
                    // destination lists are the same, so we need to flip the source/dest indices
                    // to point at the end of the spans to be copied.
                    sourceIndex = sourceIndex + length;
                    destinationIndex = destinationIndex + length;
                    for (; length > 0; length--)
                    {
                        destinationList.InnerList[--destinationIndex] = sourceList.InnerList[--sourceIndex];
                    }
                }
                else
                {
                    for (; length > 0; length--)
                    {
                        destinationList.InnerList[destinationIndex++] = sourceList.InnerList[sourceIndex++];
                    }
                }
            }

            /// <summary>
            /// Removes the value using its hash code as its identity.
            /// This is needed because the underlying item in the collection may have already been collected changing
            /// the identity of the WeakRefObject making it impossible for the collection to identify it.
            /// See WeakRefObject for more info.
            /// </summary>
            public void RemoveByHashCode(object value)
            {
                if (value == null)
                {
                    return;
                }

                int hash = value.GetHashCode();

                for (int idx = 0; idx < InnerList.Count; idx++)
                {
                    if (InnerList[idx] != null && InnerList[idx]!.GetHashCode() == hash)
                    {
                        RemoveAt(idx);
                        return;
                    }
                }
            }

            public void Clear() => InnerList.Clear();

            public bool IsFixedSize => InnerList.IsFixedSize;

            public bool Contains(object? value) => InnerList.Contains(CreateWeakRefObject(value));

            public void RemoveAt(int index) => InnerList.RemoveAt(index);

            public void Remove(object? value) => InnerList.Remove(CreateWeakRefObject(value));

            public int IndexOf(object? value) => InnerList.IndexOf(CreateWeakRefObject(value));

            public void Insert(int index, object? value) => InnerList.Insert(index, CreateWeakRefObject(value));

            public int Add(object? value)
            {
                if (Count > RefCheckThreshold)
                {
                    ScavengeReferences();
                }

                return InnerList.Add(CreateWeakRefObject(value));
            }

            public int Count => InnerList.Count;

            object ICollection.SyncRoot => InnerList.SyncRoot;

            public bool IsReadOnly => InnerList.IsReadOnly;

            public void CopyTo(Array array, int index) => InnerList.CopyTo(array, index);

            bool ICollection.IsSynchronized => InnerList.IsSynchronized;

            public IEnumerator GetEnumerator() => InnerList.GetEnumerator();

            /// <summary>
            /// Wraps a weak ref object.
            /// WARNING: Use this class carefully!
            /// When the weak ref is collected, this object looses its identity. This is bad when the object has been
            /// added to a collection since Contains(WeakRef(item)) and Remove(WeakRef(item)) would not be able to
            /// identify the item.
            /// </summary>
            internal sealed class WeakRefObject
            {
                private readonly int _hash;
                private readonly WeakReference _weakHolder;

                internal WeakRefObject(object obj)
                {
                    Debug.Assert(obj != null, "Unexpected null object!");
                    _weakHolder = new WeakReference(obj);
                    _hash = obj.GetHashCode();
                }

                internal bool IsAlive => _weakHolder.IsAlive;

                internal object? Target => _weakHolder.Target;

                public override int GetHashCode() => _hash;

                public override bool Equals(object? obj)
                {
                    WeakRefObject? other = obj as WeakRefObject;

                    if (other == this)
                    {
                        return true;
                    }

                    if (other == null)
                    {
                        return false;
                    }

                    if (other.Target != Target)
                    {
                        if (Target == null || !Target.Equals(other.Target))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }
        }
    }
}
