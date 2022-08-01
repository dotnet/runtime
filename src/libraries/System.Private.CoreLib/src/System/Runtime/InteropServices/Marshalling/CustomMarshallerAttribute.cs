// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Attribute to indicate an entry point type for defining a marshaller.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class CustomMarshallerAttribute : Attribute
    {
        /// <summary>
        /// Create a <see cref="CustomMarshallerAttribute"/> instance.
        /// </summary>
        /// <param name="managedType">Managed type to marshal.</param>
        /// <param name="marshalMode">The marshalling mode this attribute applies to.</param>
        /// <param name="marshallerType">Type used for marshalling.</param>
        public CustomMarshallerAttribute(Type managedType, MarshalMode marshalMode, Type marshallerType)
        {
            ManagedType = managedType;
            MarshalMode = marshalMode;
            MarshallerType = marshallerType;
        }

        /// <summary>
        /// The managed type to marshal.
        /// </summary>
        public Type ManagedType { get; }

        /// <summary>
        /// The marshalling mode this attribute applies to.
        /// </summary>
        public MarshalMode MarshalMode { get; }

        /// <summary>
        /// Type used for marshalling.
        /// </summary>
        public Type MarshallerType { get; }

        /// <summary>
        /// Placeholder type for generic parameter
        /// </summary>
        public struct GenericPlaceholder
        {
        }
    }
}
