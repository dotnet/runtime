// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.Marshalling
{
    [AttributeUsage(AttributeTargets.Interface)]
    public class GeneratedComInterfaceAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets how to marshal string arguments to all methods on the interface.
        /// If the attributed interface inherits from another interface with <see cref="GeneratedComInterfaceAttribute"/>,
        /// it must have the same values for <see cref="StringMarshalling"/> and <see cref="StringMarshallingCustomType"/>.
        /// </summary>
        /// <remarks>
        /// If this field is set to a value other than <see cref="StringMarshalling.Custom" />,
        /// <see cref="StringMarshallingCustomType" /> must not be specified.
        /// </remarks>
        public StringMarshalling StringMarshalling { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Type"/> used to control how string arguments are marshalled for all methods on the interface.
        /// If the attributed interface inherits from another interface with <see cref="GeneratedComInterfaceAttribute"/>,
        /// it must have the same values for <see cref="StringMarshalling"/> and <see cref="StringMarshallingCustomType"/>.
        /// </summary>
        /// <remarks>
        /// If this field is specified, <see cref="StringMarshalling" /> must not be specified
        /// or must be set to <see cref="StringMarshalling.Custom" />.
        /// </remarks>
        public Type? StringMarshallingCustomType { get; set; }
    }
}
