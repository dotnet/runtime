// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;

namespace System.Diagnostics
{
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class ProcessThreadCollection : ICollection
    {
        private List<ProcessThread> _list;

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

        protected List<ProcessThread> InnerList
        {
            get
            {
                if (_list == null)
                    _list = new List<ProcessThread>();
                return _list;
            }
        }

        public virtual int Count
        {
            get { return InnerList.Count; }
        }

        bool ICollection.IsSynchronized
        {
            get { return ((ICollection)InnerList).IsSynchronized; }
        }

        object ICollection.SyncRoot
        {
            get { return ((ICollection)InnerList).SyncRoot; }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)InnerList).CopyTo(array, index);
        }

        public virtual IEnumerator GetEnumerator()
        {
            return InnerList.GetEnumerator();
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

