// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;

namespace System.Diagnostics
{
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public class ProcessModuleCollection : ICollection
    {
        private List<ProcessModule> _list;

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected ProcessModuleCollection()
        {
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public ProcessModuleCollection(ProcessModule[] processModules)
        {
            InnerList.AddRange(processModules);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public ProcessModule this[int index]
        {
            get { return (ProcessModule)InnerList[index]; }
        }

        protected List<ProcessModule> InnerList
        {
            get
            {
                if (_list == null)
                    _list = new List<ProcessModule>();
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
        public int IndexOf(ProcessModule module)
        {
            return InnerList.IndexOf(module);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool Contains(ProcessModule module)
        {
            return InnerList.Contains(module);
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void CopyTo(ProcessModule[] array, int index)
        {
            InnerList.CopyTo(array, index);
        }
    }
}

