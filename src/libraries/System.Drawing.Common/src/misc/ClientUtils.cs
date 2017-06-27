// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.Globalization;

namespace System.Drawing
{
    // Miscellaneous utilities
    internal static class ClientUtils
    {
        // ExecutionEngineException is obsolete and shouldn't be used (to catch, throw or reference) anymore.
        // Pragma added to prevent converting the "type is obsolete" warning into build error.
        // File owner should fix this.
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
            return (ex is System.Security.SecurityException) || IsCriticalException(ex);
        }

        public static int GetBitCount(uint x)
        {
            int count = 0;
            while (x > 0)
            {
                x &= x - 1;
                count++;
            }
            return count;
        }


        // Sequential version
        // assumes sequential enum members 0,1,2,3,4 -etc.            
        // 
        public static bool IsEnumValid(Enum enumValue, int value, int minValue, int maxValue)
        {
            bool valid = (value >= minValue) && (value <= maxValue);
#if DEBUG            
            Debug_SequentialEnumIsDefinedCheck(enumValue, minValue, maxValue);
#endif
            return valid;
        }

        // Useful for sequential enum values which only use powers of two 0,1,2,4,8 etc: IsEnumValid(val, min, max, 1)
        // Valid example: TextImageRelation 0,1,2,4,8 - only one bit can ever be on, and the value is between 0 and 8.
        //
        //   ClientUtils.IsEnumValid((int)(relation), /*min*/(int)TextImageRelation.None, (int)TextImageRelation.TextBeforeImage,1);
        //
        public static bool IsEnumValid(Enum enumValue, int value, int minValue, int maxValue, int maxNumberOfBitsOn)
        {
            System.Diagnostics.Debug.Assert(maxNumberOfBitsOn >= 0 && maxNumberOfBitsOn < 32, "expect this to be greater than zero and less than 32");

            bool valid = (value >= minValue) && (value <= maxValue);
            //Note: if it's 0, it'll have no bits on.  If it's a power of 2, it'll have 1.
            valid = (valid && GetBitCount((uint)value) <= maxNumberOfBitsOn);
#if DEBUG
            Debug_NonSequentialEnumIsDefinedCheck(enumValue, minValue, maxValue, maxNumberOfBitsOn, valid);
#endif
            return valid;
        }

        // Useful for enums that are a subset of a bitmask
        // Valid example: EdgeEffects  0, 0x800 (FillInterior), 0x1000 (Flat), 0x4000(Soft), 0x8000(Mono)
        //
        //   ClientUtils.IsEnumValid((int)(effects), /*mask*/ FillInterior | Flat | Soft | Mono,
        //          ,2);
        //
        public static bool IsEnumValid_Masked(Enum enumValue, int value, UInt32 mask)
        {
            bool valid = ((value & mask) == value);

#if DEBUG
            Debug_ValidateMask(enumValue, mask);
#endif

            return valid;
        }

        // Useful for cases where you have discontiguous members of the enum.
        // Valid example: AutoComplete source.
        // if (!ClientUtils.IsEnumValid(value, AutoCompleteSource.None, 
        //                                            AutoCompleteSource.AllSystemSources
        //                                            AutoCompleteSource.AllUrl,
        //                                            AutoCompleteSource.CustomSource,
        //                                            AutoCompleteSource.FileSystem,
        //                                            AutoCompleteSource.FileSystemDirectories,
        //                                            AutoCompleteSource.HistoryList,
        //                                            AutoCompleteSource.ListItems,
        //                                            AutoCompleteSource.RecentlyUsedList))
        //
        // PERF tip: put the default value in the enum towards the front of the argument list.
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static bool IsEnumValid_NotSequential(System.Enum enumValue, int value, params int[] enumValues)
        {
            System.Diagnostics.Debug.Assert(Enum.GetValues(enumValue.GetType()).Length == enumValues.Length, "Not all the enum members were passed in.");
            for (int i = 0; i < enumValues.Length; i++)
            {
                if (enumValues[i] == value)
                {
                    return true;
                }
            }
            return false;
        }

#if DEBUG      
        [ThreadStatic]
        private static Hashtable t_enumValueInfo;
        public const int MaxCache = 300;  // we think we're going to get O(100) of these, put in a tripwire if it gets larger.

        private class SequentialEnumInfo
        {
            public SequentialEnumInfo(Type t)
            {
                int actualMinimum = int.MaxValue;
                int actualMaximum = int.MinValue;
                int countEnumVals = 0;

                foreach (int iVal in Enum.GetValues(t))
                {
                    actualMinimum = Math.Min(actualMinimum, iVal);
                    actualMaximum = Math.Max(actualMaximum, iVal);
                    countEnumVals++;
                }

                Debug.Assert(countEnumVals - 1 == actualMaximum - actualMinimum);
                MinValue = actualMinimum;
                MaxValue = actualMaximum;
            }
            public int MinValue;
            public int MaxValue;
        }

        private static void Debug_SequentialEnumIsDefinedCheck(Enum value, int minVal, int maxVal)
        {
            Type enumType = value.GetType();

            if (t_enumValueInfo == null)
            {
                t_enumValueInfo = new Hashtable();
            }

            SequentialEnumInfo sequentialEnumInfo = null;

            if (t_enumValueInfo.ContainsKey(enumType))
            {
                sequentialEnumInfo = t_enumValueInfo[enumType] as SequentialEnumInfo;
            }
            if (sequentialEnumInfo == null)
            {
                sequentialEnumInfo = new SequentialEnumInfo(enumType);
                Debug.Assert(t_enumValueInfo.Count <= MaxCache);
                t_enumValueInfo[enumType] = sequentialEnumInfo;
            }
            Debug.Assert(minVal == sequentialEnumInfo.MinValue, "Minimum passed in is not the actual minimum for the enum.  Consider changing the parameters or using a different function.");
            Debug.Assert(maxVal == sequentialEnumInfo.MaxValue, "Maximum passed in is not the actual maximum for the enum.  Consider changing the parameters or using a different function.");
        }

        private static void Debug_ValidateMask(Enum value, uint mask)
        {
            Type enumType = value.GetType();
            uint newMask = 0;
            foreach (int iVal in Enum.GetValues(enumType))
            {
                newMask = newMask | (uint)iVal;
            }
            Debug.Assert(newMask == mask, "Mask not valid in IsEnumValid!");
        }

        private static void Debug_NonSequentialEnumIsDefinedCheck(Enum value, int minVal, int maxVal, int maxBitsOn, bool isValid)
        {
            Type enumType = value.GetType();
            int actualMinimum = int.MaxValue;
            int actualMaximum = int.MinValue;
            int checkedValue = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            int maxBitsFound = 0;
            bool foundValue = false;
            foreach (int iVal in Enum.GetValues(enumType))
            {
                actualMinimum = Math.Min(actualMinimum, iVal);
                actualMaximum = Math.Max(actualMaximum, iVal);
                maxBitsFound = Math.Max(maxBitsFound, GetBitCount((uint)iVal));
                if (checkedValue == iVal)
                {
                    foundValue = true;
                }
            }

            Debug.Assert(minVal == actualMinimum, "Minimum passed in is not the actual minimum for the enum.  Consider changing the parameters or using a different function.");
            Debug.Assert(minVal == actualMinimum, "Maximum passed in is not the actual maximum for the enum.  Consider changing the parameters or using a different function.");
            Debug.Assert(maxBitsFound == maxBitsOn, "Incorrect usage of IsEnumValid function. The bits set to 1 in this enum was found to be: " + maxBitsFound.ToString(CultureInfo.InvariantCulture) + "this does not match what's passed in: " + maxBitsOn.ToString(CultureInfo.InvariantCulture));
            Debug.Assert(foundValue == isValid, string.Format(CultureInfo.InvariantCulture, "Returning {0} but we actually {1} found the value in the enum! Consider using a different overload to IsValidEnum.", isValid, ((foundValue) ? "have" : "have not")));
        }
#endif

        /// <summary>
        /// WeakRefCollection - a collection that holds onto weak references
        ///
        /// Essentially you pass in the object as it is, and under the covers
        /// we only hold a weak reference to the object.
        ///
        /// -----------------------------------------------------------------
        /// !!!IMPORTANT USAGE NOTE!!!        
        /// Users of this class should set the RefCheckThreshold property 
        /// explicitly or call ScavengeReferences every once in a while to 
        /// remove dead references.
        /// Also avoid calling Remove(item).  Instead call RemoveByHashCode(item)
        /// to make sure dead refs are removed.
        /// -----------------------------------------------------------------
        ///
        /// </summary>        
        internal class WeakRefCollection : IList
        {
            private int _refCheckThreshold = Int32.MaxValue; // this means this is disabled by default.
            private ArrayList _innerList;

            internal WeakRefCollection()
            {
                _innerList = new ArrayList(4);
            }

            internal WeakRefCollection(int size)
            {
                _innerList = new ArrayList(size);
            }

            internal ArrayList InnerList
            {
                get { return _innerList; }
            }

            /// <summary>
            /// Indicates the value where the collection should check its items to remove dead weakref left over.
            /// Note: When GC collects weak refs from this collection the WeakRefObject identity changes since its 
            ///       Target becomes null.  This makes the item unrecognizable by the collection and cannot be
            ///       removed - Remove(item) and Contains(item) will not find it anymore.
            /// </summary>
            public int RefCheckThreshold
            {
                get
                {
                    return _refCheckThreshold;
                }
                set
                {
                    _refCheckThreshold = value;
                }
            }

            public object this[int index]
            {
                get
                {
                    WeakRefObject weakRef = InnerList[index] as WeakRefObject;

                    if ((weakRef != null) && (weakRef.IsAlive))
                    {
                        return weakRef.Target;
                    }

                    return null;
                }
                set
                {
                    InnerList[index] = CreateWeakRefObject(value);
                }
            }

            public void ScavengeReferences()
            {
                int currentIndex = 0;
                int currentCount = Count;
                for (int i = 0; i < currentCount; i++)
                {
                    object item = this[currentIndex];

                    if (item == null)
                    {
                        InnerList.RemoveAt(currentIndex);
                    }
                    else
                    {   // only incriment if we have not removed the item
                        currentIndex++;
                    }
                }
            }

            public override bool Equals(object obj)
            {
                WeakRefCollection other = obj as WeakRefCollection;

                if (other == this)
                {
                    return true;
                }

                if (other == null || Count != other.Count)
                {
                    return false;
                }

                for (int i = 0; i < Count; i++)
                {
                    if (InnerList[i] != other.InnerList[i])
                    {
                        if (InnerList[i] == null || !InnerList[i].Equals(other.InnerList[i]))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            private WeakRefObject CreateWeakRefObject(object value)
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
                    if (InnerList[idx] != null && InnerList[idx].GetHashCode() == hash)
                    {
                        RemoveAt(idx);
                        return;
                    }
                }
            }

            #region IList Members
            public void Clear() { InnerList.Clear(); }
            public bool IsFixedSize { get { return InnerList.IsFixedSize; } }
            public bool Contains(object value) { return InnerList.Contains(CreateWeakRefObject(value)); }
            public void RemoveAt(int index) { InnerList.RemoveAt(index); }
            public void Remove(object value) { InnerList.Remove(CreateWeakRefObject(value)); }
            public int IndexOf(object value) { return InnerList.IndexOf(CreateWeakRefObject(value)); }
            public void Insert(int index, object value) { InnerList.Insert(index, CreateWeakRefObject(value)); }
            public int Add(object value)
            {
                if (Count > RefCheckThreshold)
                {
                    ScavengeReferences();
                }
                return InnerList.Add(CreateWeakRefObject(value));
            }
            #endregion

            #region ICollection Members
            public int Count { get { return InnerList.Count; } }
            object ICollection.SyncRoot { get { return InnerList.SyncRoot; } }
            public bool IsReadOnly { get { return InnerList.IsReadOnly; } }
            public void CopyTo(Array array, int index) { InnerList.CopyTo(array, index); }
            bool ICollection.IsSynchronized { get { return InnerList.IsSynchronized; } }
            #endregion

            #region IEnumerable Members
            public IEnumerator GetEnumerator()
            {
                return InnerList.GetEnumerator();
            }
            #endregion

            /// <summary>
            /// Wraps a weak ref object.
            /// WARNING: Use this class carefully!  
            /// When the weak ref is collected, this object looses its identity. This is bad when the object has been
            /// added to a collection since Contains(WeakRef(item)) and Remove(WeakRef(item)) would not be able to
            /// identify the item.
            /// </summary>
            internal class WeakRefObject
            {
                private int _hash;
                private WeakReference _weakHolder;

                internal WeakRefObject(object obj)
                {
                    Debug.Assert(obj != null, "Unexpected null object!");
                    _weakHolder = new WeakReference(obj);
                    _hash = obj.GetHashCode();
                }

                internal bool IsAlive
                {
                    get { return _weakHolder.IsAlive; }
                }

                internal object Target
                {
                    get
                    {
                        return _weakHolder.Target;
                    }
                }

                public override int GetHashCode()
                {
                    return _hash;
                }

                public override bool Equals(object obj)
                {
                    WeakRefObject other = obj as WeakRefObject;

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
