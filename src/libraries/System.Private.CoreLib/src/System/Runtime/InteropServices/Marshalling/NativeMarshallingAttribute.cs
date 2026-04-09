// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Provides a default custom marshaller type for a given managed type.
    /// </summary>
    /// <remarks>
    /// This attribute is recognized by the runtime-provided source generators for source-generated interop scenarios.
    /// It's not used by the interop marshalling system at run time.
    /// </remarks>
    /// <seealso cref="LibraryImportAttribute" />
    /// <seealso cref="CustomMarshallerAttribute" />
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Delegate)]
    public sealed class NativeMarshallingAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the  <see cref="NativeMarshallingAttribute" /> class that provides a native marshalling type.
        /// </summary>
        /// <param name="nativeType">The marshaller type used to convert the attributed type from managed to native code. This type must be attributed with <see cref="CustomMarshallerAttribute" />.</param>
        public NativeMarshallingAttribute(Type nativeType)
        {
            NativeType = nativeType;
        }

        /// <summary>
        /// Gets the marshaller type used to convert the attributed type from managed to native code. This type must be attributed with <see cref="CustomMarshallerAttribute" />.
        /// </summary>
        public Type NativeType { get; }
    }
}
