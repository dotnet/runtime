// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Indicates an entry point type for defining a marshaller.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class CustomMarshallerAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomMarshallerAttribute"/> class.
        /// </summary>
        /// <param name="managedType">The managed type to marshal.</param>
        /// <param name="marshalMode">The marshalling mode this attribute applies to.</param>
        /// <param name="marshallerType">The type used for marshalling.</param>
        public CustomMarshallerAttribute(Type managedType, MarshalMode marshalMode, Type marshallerType)
        {
            ManagedType = managedType;
            MarshalMode = marshalMode;
            MarshallerType = marshallerType;
        }

        /// <summary>
        /// Gets the managed type to marshal.
        /// </summary>
        public Type ManagedType { get; }

        /// <summary>
        /// Gets the marshalling mode this attribute applies to.
        /// </summary>
        public MarshalMode MarshalMode { get; }

        /// <summary>
        /// Gets the type used for marshalling.
        /// </summary>
        public Type MarshallerType { get; }

        /// <summary>
        /// Placeholder type for a generic parameter.
        /// </summary>
        public struct GenericPlaceholder
        {
        }
    }
}
