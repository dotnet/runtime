// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.Diagnostics
{
    /// <summary>Provides a strongly typed collection of <see cref="System.Diagnostics.ProcessThread" /> objects.</summary>
    public class ProcessThreadCollection : ReadOnlyCollectionBase
    {
        /// <summary>Initializes a new instance of the <see cref="System.Diagnostics.ProcessThreadCollection" /> class, with no associated <see cref="System.Diagnostics.ProcessThread" /> instances.</summary>
        protected ProcessThreadCollection()
        {
        }

        /// <summary>Initializes a new instance of the <see cref="System.Diagnostics.ProcessThreadCollection" /> class, using the specified array of <see cref="System.Diagnostics.ProcessThread" /> instances.</summary>
        /// <param name="processThreads">An array of <see cref="System.Diagnostics.ProcessThread" /> instances with which to initialize this <see cref="System.Diagnostics.ProcessThreadCollection" /> instance.</param>
        public ProcessThreadCollection(ProcessThread[] processThreads)
        {
            InnerList.AddRange(processThreads);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public ProcessThread this[int index]
        {
            get { return (ProcessThread)InnerList[index]!; }
        }

        /// <summary>Appends a process thread to the collection.</summary>
        /// <param name="thread">The thread to add to the collection.</param>
        /// <returns>The zero-based index of the thread in the collection.</returns>
        public int Add(ProcessThread thread)
        {
            return ((IList)InnerList).Add(thread);
        }

        /// <summary>Inserts a process thread at the specified location in the collection.</summary>
        /// <param name="index">The zero-based index indicating the location at which to insert the thread.</param>
        /// <param name="thread">The thread to insert into the collection.</param>
        public void Insert(int index, ProcessThread thread)
        {
            InnerList.Insert(index, thread);
        }

        /// <summary>Provides the location of a specified thread within the collection.</summary>
        /// <param name="thread">The <see cref="System.Diagnostics.ProcessThread" /> whose index is retrieved.</param>
        /// <returns>The zero-based index that defines the location of the thread within the <see cref="System.Diagnostics.ProcessThreadCollection" />.</returns>
        public int IndexOf(ProcessThread thread)
        {
            return InnerList.IndexOf(thread);
        }

        /// <summary>Determines whether the specified process thread exists in the collection.</summary>
        /// <param name="thread">A <see cref="System.Diagnostics.ProcessThread" /> instance that indicates the thread to find in this collection.</param>
        /// <returns><see langword="true" /> if the thread exists in the collection; otherwise, <see langword="false" />.</returns>
        /// <remarks>A module is identified by its module name and its fully qualified file path.</remarks>
        public bool Contains(ProcessThread thread)
        {
            return InnerList.Contains(thread);
        }

        /// <summary>Deletes a process thread from the collection.</summary>
        /// <param name="thread">The thread to remove from the collection.</param>
        public void Remove(ProcessThread thread)
        {
            InnerList.Remove(thread);
        }

        /// <summary>Copies an array of <see cref="System.Diagnostics.ProcessThread" /> instances to the collection, at the specified index.</summary>
        /// <param name="array">An array of <see cref="System.Diagnostics.ProcessThread" /> instances to add to the collection.</param>
        /// <param name="index">The location at which to add the new instances.</param>
        public void CopyTo(ProcessThread[] array, int index)
        {
            InnerList.CopyTo(array, index);
        }

        internal void Dispose()
        {
            foreach (ProcessThread processThread in this)
            {
                processThread.Dispose();
            }
        }
    }
}
