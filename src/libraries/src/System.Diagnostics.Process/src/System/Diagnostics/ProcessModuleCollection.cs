// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.Diagnostics
{
    public class ProcessModuleCollection : ReadOnlyCollectionBase
    {
        protected ProcessModuleCollection()
        {
        }

        public ProcessModuleCollection(ProcessModule[] processModules)
        {
            InnerList.AddRange(processModules);
        }

        internal ProcessModuleCollection(int capacity)
        {
            if (capacity > 0)
            {
                InnerList.Capacity = capacity;
            }
        }

        internal void Add(ProcessModule module) => InnerList.Add(module);

        internal void Insert(int index, ProcessModule module) => InnerList.Insert(index, module);

        internal void RemoveAt(int index) => InnerList.RemoveAt(index);

        public ProcessModule this[int index] => (ProcessModule)InnerList[index];

        public int IndexOf(ProcessModule module) => InnerList.IndexOf(module);

        public bool Contains(ProcessModule module) => InnerList.Contains(module);

        public void CopyTo(ProcessModule[] array, int index) => InnerList.CopyTo(array, index);
    }
}
