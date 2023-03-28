// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// An attribute to mark this class as a type whose instances should be exposed to COM.
    /// </summary>
    /// <typeparam name="T">The type that provides information about how to expose the attributed type to COM.</typeparam>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ComExposedClassAttribute<T> : Attribute, IComExposedDetails
        where T : IComExposedClass
    {
        /// <inheritdoc />
        public unsafe ComWrappers.ComInterfaceEntry* GetComInterfaceEntries(out int count) => T.GetComInterfaceEntries(out count);
    }
}
