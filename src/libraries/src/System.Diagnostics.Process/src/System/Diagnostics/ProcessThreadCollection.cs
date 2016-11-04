// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace System.Diagnostics
{
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class ProcessThreadCollection : ReadOnlyCollectionBase
    {
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected ProcessThreadCollection()
        {
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public ProcessThreadCollection(ProcessThread[] processThreads)
        {
            InnerList.AddRange(processThreads);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public ProcessThread this[int index]
        {
            get { return (ProcessThread)InnerList[index]; }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int Add(ProcessThread thread)
        {
            return ((IList)InnerList).Add(thread);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Insert(int index, ProcessThread thread)
        {
            InnerList.Insert(index, thread);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public int IndexOf(ProcessThread thread)
        {
            return InnerList.IndexOf(thread);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool Contains(ProcessThread thread)
        {
            return InnerList.Contains(thread);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void Remove(ProcessThread thread)
        {
            InnerList.Remove(thread);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void CopyTo(ProcessThread[] array, int index)
        {
            InnerList.CopyTo(array, index);
        }
    }
}

