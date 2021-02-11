// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.Diagnostics
{
    /// <summary>Provides a strongly typed collection of <see cref="System.Diagnostics.ProcessModule" /> objects.</summary>
    /// <remarks>A module is an executable file or a dynamic link library (DLL). Each process consists of one or more modules. You can use this class to iterate over a collection of process modules on the system. A module is identified by its module name and fully qualified file path.</remarks>
    /// <altmember cref="System.Diagnostics.ProcessModule.ModuleName"/>
    /// <altmember cref="System.Diagnostics.ProcessModule.FileName"/>
    public class ProcessModuleCollection : ReadOnlyCollectionBase
    {
        /// <summary>Initializes a new instance of the <see cref="System.Diagnostics.ProcessModuleCollection" /> class, with no associated <see cref="System.Diagnostics.ProcessModule" /> instances.</summary>
        protected ProcessModuleCollection()
        {
        }

        /// <summary>Initializes a new instance of the <see cref="System.Diagnostics.ProcessModuleCollection" /> class, using the specified array of <see cref="System.Diagnostics.ProcessModule" /> instances.</summary>
        /// <param name="processModules">An array of <see cref="System.Diagnostics.ProcessModule" /> instances with which to initialize this <see cref="System.Diagnostics.ProcessModuleCollection" /> instance.</param>
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

        public ProcessModule this[int index] => (ProcessModule)InnerList[index]!;

        /// <summary>Provides the location of a specified module within the collection.</summary>
        /// <param name="module">The <see cref="System.Diagnostics.ProcessModule" /> whose index is retrieved.</param>
        /// <returns>The zero-based index that defines the location of the module within the <see cref="System.Diagnostics.ProcessModuleCollection" />.</returns>
        public int IndexOf(ProcessModule module) => InnerList.IndexOf(module);

        /// <summary>Determines whether the specified process module exists in the collection.</summary>
        /// <param name="module">A <see cref="System.Diagnostics.ProcessModule" /> instance that indicates the module to find in this collection.</param>
        /// <returns><see langword="true" /> if the module exists in the collection; otherwise, <see langword="false" />.</returns>
        /// <remarks>A module is identified by its module name and its fully qualified file path.</remarks>
        public bool Contains(ProcessModule module) => InnerList.Contains(module);

        /// <summary>Copies an array of <see cref="System.Diagnostics.ProcessModule" /> instances to the collection, at the specified index.</summary>
        /// <param name="array">An array of <see cref="System.Diagnostics.ProcessModule" /> instances to add to the collection.</param>
        /// <param name="index">The location at which to add the new instances.</param>
        public void CopyTo(ProcessModule[] array, int index) => InnerList.CopyTo(array, index);

        internal void Dispose()
        {
            foreach (ProcessModule processModule in this)
            {
                processModule.Dispose();
            }
        }
    }
}
