// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Attribute used to provide a default custom marshaller type for a given managed type.
    /// </summary>
    /// <remarks>
    /// This attribute is recognized by the runtime-provided source generators for source-generated interop scenarios.
    /// It is not used by the interop marshalling system at runtime.
    /// <seealso cref="LibraryImportAttribute"/>
    /// <seealso cref="CustomMarshallerAttribute" />
    /// </remarks>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Delegate)]
    public sealed class NativeMarshallingAttribute : Attribute
    {
        /// <summary>
        /// Create a <see cref="NativeMarshallingAttribute" /> that provides a native marshalling type.
        /// </summary>
        /// <param name="nativeType">The marshaller type used to convert the attributed type from managed to native code. This type must be attributed with <see cref="CustomMarshallerAttribute" /></param>
        public NativeMarshallingAttribute(Type nativeType)
        {
            NativeType = nativeType;
        }

        /// <summary>
        /// The marshaller type used to convert the attributed type from managed to native code. This type must be attributed with <see cref="CustomMarshallerAttribute" />
        /// </summary>
        public Type NativeType { get; }
    }
}
