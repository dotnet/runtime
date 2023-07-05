// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This type is for the COM Source Generator and defines basic vtable interactions that we would need in the COM source generator in one form or another.
namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Information about a virtual method table and the unmanaged instance pointer.
    /// </summary>
    [CLSCompliant(false)]
    public readonly unsafe struct VirtualMethodTableInfo
    {
        /// <summary>
        /// Construct a <see cref="VirtualMethodTableInfo"/> from a given instance pointer and table memory.
        /// </summary>
        /// <param name="thisPointer">The pointer to the instance.</param>
        /// <param name="virtualMethodTable">The block of memory that represents the virtual method table.</param>
        public VirtualMethodTableInfo(void* thisPointer, void** virtualMethodTable)
        {
            ThisPointer = thisPointer;
            VirtualMethodTable = virtualMethodTable;
        }

        /// <summary>
        /// The unmanaged instance pointer
        /// </summary>
        public void* ThisPointer { get; }

        /// <summary>
        /// The virtual method table.
        /// </summary>
        public void** VirtualMethodTable { get; }

        /// <summary>
        /// Deconstruct this structure into its two fields.
        /// </summary>
        /// <param name="thisPointer">The <see cref="ThisPointer"/> result</param>
        /// <param name="virtualMethodTable">The <see cref="VirtualMethodTable"/> result</param>
        public void Deconstruct(out void* thisPointer, out void** virtualMethodTable)
        {
            thisPointer = ThisPointer;
            virtualMethodTable = VirtualMethodTable;
        }
    }
}
