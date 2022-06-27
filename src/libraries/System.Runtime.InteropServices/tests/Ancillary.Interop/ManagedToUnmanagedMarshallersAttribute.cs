// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable enable

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Specify marshallers used in the managed to unmanaged direction (that is, P/Invoke)
    /// </summary>
#if LIBRARYIMPORT_GENERATOR_TEST
    public
#else
    internal
#endif
    sealed class ManagedToUnmanagedMarshallersAttribute : CustomUnmanagedTypeMarshallersAttributeBase
    {
        /// <summary>
        /// Create instance of <see cref="ManagedToUnmanagedMarshallersAttribute"/>.
        /// </summary>
        /// <param name="managedType">Managed type to marshal</param>
        public ManagedToUnmanagedMarshallersAttribute(Type managedType) { }

        /// <summary>
        /// Marshaller to use when a parameter of the managed type is passed by-value or with the <c>in</c> keyword.
        /// </summary>
        public Type? InMarshaller { get; set; }

        /// <summary>
        /// Marshaller to use when a parameter of the managed type is passed by-value or with the <c>ref</c> keyword.
        /// </summary>
        public Type? RefMarshaller { get; set; }

        /// <summary>
        /// Marshaller to use when a parameter of the managed type is passed by-value or with the <c>out</c> keyword.
        /// </summary>
        public Type? OutMarshaller { get; set; }
    }
}
