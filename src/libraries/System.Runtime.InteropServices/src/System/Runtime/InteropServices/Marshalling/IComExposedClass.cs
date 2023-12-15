// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Type level information for managed class types exposed to COM.
    /// </summary>
    [CLSCompliant(false)]
    public unsafe interface IComExposedClass
    {
        /// <summary>
        /// Get the COM interface information to provide to a <see cref="ComWrappers"/> instance to expose this type to COM.
        /// </summary>
        /// <param name="count">The number of COM interfaces this type implements.</param>
        /// <returns>The interface entry information for the interfaces the type implements.</returns>
        public static abstract ComWrappers.ComInterfaceEntry* GetComInterfaceEntries(out int count);
    }
}
