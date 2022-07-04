// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable enable

using System;

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Attribute to indicate an entry point type for defining a marshaller.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
#if LIBRARYIMPORT_GENERATOR_TEST
    public
#else
    internal
#endif
    sealed class CustomMarshallerAttribute : Attribute
    {
        /// <summary>
        /// Create a <see cref="CustomMarshallerAttribute"/> instance.
        /// </summary>
        /// <param name="managedType">Managed type to marshal.</param>
        /// <param name="scenario">Marshalling scenario.</param>
        /// <param name="marshallerType">Type used for marshalling.</param>
        public CustomMarshallerAttribute(Type managedType, Scenario scenario, Type marshallerType) { }

        /// <summary>
        /// Placeholder type for generic parameter
        /// </summary>
        public struct GenericPlaceholder
        {
        }
    }
}
