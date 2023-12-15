// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Details about a managed class type exposed to COM.
    /// </summary>
    [CLSCompliant(false)]
    public unsafe interface IComExposedDetails
    {
        /// <summary>
        /// Get the COM interface information to provide to a <see cref="ComWrappers"/> instance to expose this type to COM.
        /// </summary>
        /// <param name="count">The number of COM interfaces this type implements.</param>
        /// <returns>The interface entry information for the interfaces the type implements.</returns>
        ComWrappers.ComInterfaceEntry* GetComInterfaceEntries(out int count);

        internal static IComExposedDetails? GetFromAttribute(RuntimeTypeHandle handle)
        {
            var type = Type.GetTypeFromHandle(handle);
            if (type is null)
            {
                return null;
            }
            return (IComExposedDetails?)type.GetCustomAttribute(typeof(ComExposedClassAttribute<>));
        }
    }
}
