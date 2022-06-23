// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Base class attribute for custom marshaller attributes.
    /// </summary>
    /// <remarks>
    /// Use a base class here to allow doing ManagedToUnmanagedMarshallersAttribute.GenericPlaceholder, etc. without having 3 separate placeholder types.
    /// For the following attribute types, any marshaller types that are provided will be validated by an analyzer to have the correct members to prevent
    /// developers from accidentally typoing a member like Free() and causing memory leaks.
    /// </remarks>
#if LIBRARYIMPORT_GENERATOR_TEST
    public
#else
    internal
#endif
    abstract class CustomUnmanagedTypeMarshallersAttributeBase : Attribute
    {
        /// <summary>
        /// Placeholder type for generic parameter
        /// </summary>
        public struct GenericPlaceholder { }
    }
}
