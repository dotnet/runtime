// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Collections.Generic
{
    /// <summary>Internal helper functions for working with enumerables.</summary>
    internal static class EnumerableHelpers
    {
        /// <summary>Converts an enumerable to an array.</summary>
        /// <param name="source">The enumerable to convert.</param>
        /// <returns>The resulting array.</returns>
        internal static T[] ToArray<T>(IEnumerable<T> source)
        {
            Debug.Assert(source != null);

            var collection = source as ICollection<T>;
            if (collection != null)
            {
                int count = collection.Count;
                if (count == 0)
                    return Array.Empty<T>();
                
                var result = new T[count];
                collection.CopyTo(result, arrayIndex: 0);
                return result;
            }

            // Generic methods are compiled per-instantiation for value types.
            // Do not force the jit to compile a bunch of code it will never
            // use if we only go down the ICollection path.
            return LazyToArray(source);
        }

        private static T[] LazyToArray<T>(IEnumerable<T> source)
        {
            Debug.Assert(source != null);
            Debug.Assert(!(source is ICollection<T>), $"We should have gone down the optimized route if {nameof(source)} was a regular collection.");

            const int InitialCapacity = 4;
            const int BufferListThreshold = 32;

            using (var en = source.GetEnumerator())
            {
                if (!en.MoveNext())
                    return Array.Empty<T>();

                T[] buffer = null;
                int index = 0; // index into the buffer we are reading into

                do
                {
                    if (buffer == null)
                    {
                        // First iteration: allocate an array with the initial capacity
                        buffer = new T[InitialCapacity];
                    }
                    else
                    {
                        // Resize from a previous iteration
                        Array.Resize(ref buffer, buffer.Length * 2);
                    }

                    do
                    {
                        buffer[index++] = en.Current;

                        if (index == buffer.Length)
                        {
                            // We used up the last slot
                            break;
                        }
                    }
                    while (en.MoveNext());

                    // If the first condition is hit that means that we exited from
                    // the loop via MoveNext returning false, so we're done enumerating.
                    // Otherwise we must have exited after calling Current, however
                    // the loop expects to be entered with MoveNext already having been
                    // called. So we need to call MoveNext ourselves in that case.
                    
                    // If the next MoveNext returns false, then we allocated just enough
                    // space to fit the enumerable's elements.

                    if (index < buffer.Length || !en.MoveNext())
                    {
                        int finalLength = index;
                        Array.Resize(ref buffer, finalLength);
                        return buffer;
                    }
                }
                while (buffer.Length != BufferListThreshold);

                Debug.Assert(buffer.Length == BufferListThreshold); // The only way we should have exited the loop was if the loop condition was false

                // Since this path is only going to get called for lazy enumerables of length > 32, do not
                // force the jit to compile lots of code it will never use (esp. since this method is generic).
                return LazyToArrayUsingBufferList(en, buffer);
            }
        }

        private static T[] LazyToArrayUsingBufferList<T>(IEnumerator<T> en, T[] first)
        {
            // NOTE: You are expected to call MoveNext after the last call to Current
            // before passing in the enumerator to this method.

            Debug.Assert(en != null);
            Debug.Assert(first != null);

            // Instead of further resizing the array, we're going to maintain a list
            // of buffers. Each time we can't fit the sequence into a buffer, we're
            // going to store the buffer in this list, create a new one 2x the size,
            // and continue reading in the sequence.
            // The data is no longer going to be contiguous in memory, but that doesn't
            // matter since even if we were using the resize-every-time approach, we'd
            // still have to resize again at the end to make the array exactly the right size.

            // Here's a visualization showing what first, buffers, and current
            // might look like for a 200-length enumerable:
            
            /*
                first: [items 0-31]

                buffers:
                [0]: [items 32-63]
                [1]: [items 64-127]

                current: [items 128-199], [slots 200-255 empty]
            */

            // There will be no up-front allocation for the ArrayBuilder, since
            // the backing store will be nil if you do not pass in a capacity.
            // It only starts allocating arrays when we add the first item.

            var buffers = new ArrayBuilder<T[]>(); // list of previous buffers
            var current = new T[first.Length]; // the current buffer we're reading the sequence into
            int read = first.Length; // number of items we've read so far, updated every time we exhaust a buffer

            while (true)
            {
                int index = 0; // index into the current buffer

                // Continue reading the data into the current buffer
                do
                {
                    current[index++] = en.Current;

                    if (index == current.Length)
                    {
                        index = -1;
                        break;
                    }
                }
                while (en.MoveNext());

                // If the index is -1, we called Current but not MoveNext, however
                // the loop expects to be entered with MoveNext already having been
                // called. So we need to call MoveNext ourselves in that case.
                if (index >= 0 || !en.MoveNext())
                {
                    // We've finished enumerating the sequence.
                    // Copy the data from the first buffer, then walk the list of buffers
                    // and copy the data from those. Finally, copy the data from the
                    // buffer we were just processing.
                    
                    int remainder = index >= 0 ? index : current.Length; // If we got here from !en.MoveNext() that means index == -1 and there was just enough space
                    int finalLength = checked(read + remainder);

                    var result = new T[finalLength];

                    Array.Copy(first, 0, result, 0, first.Length);

                    // Copy from the buffers in the list that came before this one
                    int copied = first.Length;
                    for (int i = 0; i < buffers.Count; i++)
                    {
                        T[] buffer = buffers[i];
                        Array.Copy(buffer, 0, result, copied, buffer.Length);
                        copied += buffer.Length;
                    }

                    // Copy the remaining data from this buffer
                    Debug.Assert(remainder <= current.Length);
                    Debug.Assert(copied + remainder == result.Length);
                    Array.Copy(current, 0, result, copied, remainder);

                    // Done!
                    return result;
                }

                // Since we exhausted the buffer, update read
                checked
                {
                    read += current.Length;
                }

                // There was not enough space in the current buffer- add it to the list,
                // and allocate a new buffer twice our size for the next iteration.
                buffers.Add(current);
                current = new T[current.Length * 2];
            }
        }

        /// <summary>Converts an enumerable to an array using the same logic as List{T}.</summary>
        /// <param name="source">The enumerable to convert.</param>
        /// <param name="length">The number of items stored in the resulting array, 0-indexed.</param>
        /// <returns>
        /// The resulting array.  The length of the array may be greater than <paramref name="length"/>,
        /// which is the actual number of elements in the array.
        /// </returns>
        internal static T[] ToArray<T>(IEnumerable<T> source, out int length)
        {
            ICollection<T> ic = source as ICollection<T>;
            if (ic != null)
            {
                int count = ic.Count;
                if (count != 0)
                {
                    // Allocate an array of the desired size, then copy the elements into it. Note that this has the same 
                    // issue regarding concurrency as other existing collections like List<T>. If the collection size 
                    // concurrently changes between the array allocation and the CopyTo, we could end up either getting an 
                    // exception from overrunning the array (if the size went up) or we could end up not filling as many 
                    // items as 'count' suggests (if the size went down).  This is only an issue for concurrent collections 
                    // that implement ICollection<T>, which as of .NET 4.6 is just ConcurrentDictionary<TKey, TValue>.
                    T[] arr = new T[count];
                    ic.CopyTo(arr, 0);
                    length = count;
                    return arr;
                }
            }
            else
            {
                using (var en = source.GetEnumerator())
                {
                    if (en.MoveNext())
                    {
                        const int DefaultCapacity = 4;
                        T[] arr = new T[DefaultCapacity];
                        arr[0] = en.Current;
                        int count = 1;

                        while (en.MoveNext())
                        {
                            if (count == arr.Length)
                            {
                                // MaxArrayLength is defined in Array.MaxArrayLength and in gchelpers in CoreCLR.
                                // It represents the maximum number of elements that can be in an array where
                                // the size of the element is greater than one byte; a separate, slightly larger constant,
                                // is used when the size of the element is one.
                                const int MaxArrayLength = 0x7FEFFFFF;

                                // This is the same growth logic as in List<T>:
                                // If the array is currently empty, we make it a default size.  Otherwise, we attempt to 
                                // double the size of the array.  Doubling will overflow once the size of the array reaches
                                // 2^30, since doubling to 2^31 is 1 larger than Int32.MaxValue.  In that case, we instead 
                                // constrain the length to be MaxArrayLength (this overflow check works because of the 
                                // cast to uint).  Because a slightly larger constant is used when T is one byte in size, we 
                                // could then end up in a situation where arr.Length is MaxArrayLength or slightly larger, such 
                                // that we constrain newLength to be MaxArrayLength but the needed number of elements is actually 
                                // larger than that.  For that case, we then ensure that the newLength is large enough to hold 
                                // the desired capacity.  This does mean that in the very rare case where we've grown to such a 
                                // large size, each new element added after MaxArrayLength will end up doing a resize.
                                int newLength = count << 1;
                                if ((uint)newLength > MaxArrayLength)
                                {
                                    newLength = MaxArrayLength <= count ? count + 1 : MaxArrayLength;
                                }

                                Array.Resize(ref arr, newLength);
                            }

                            arr[count++] = en.Current;
                        }

                        length = count;
                        return arr;
                    }
                }
            }

            length = 0;
            return Array.Empty<T>();
        }
    }
}
