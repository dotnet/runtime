// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

//
// Types in this file are used for generated p/invokes (docs/design/features/source-generator-pinvokes.md).
//
namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Delegate)]
#if LIBRARYIMPORT_GENERATOR_TEST
    public
#else
    internal
#endif
    sealed class NativeMarshallingAttribute : Attribute
    {
        public NativeMarshallingAttribute(Type nativeType)
        {
            NativeType = nativeType;
        }

        public Type NativeType { get; }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = true)]
#if LIBRARYIMPORT_GENERATOR_TEST
    public
#else
    internal
#endif
    sealed class MarshalUsingAttribute : Attribute
    {
        public MarshalUsingAttribute()
        {
            CountElementName = string.Empty;
        }

        public MarshalUsingAttribute(Type nativeType)
            : this()
        {
            NativeType = nativeType;
        }

        public Type? NativeType { get; }

        public string CountElementName { get; set; }

        public int ConstantElementCount { get; set; }

        public int ElementIndirectionDepth { get; set; }

        public const string ReturnsCountValue = "return-value";
    }

    /// <summary>
    /// Attribute used to indicate that the type can be used to convert a value of the provided <see cref="ManagedType"/> to a native representation.
    /// </summary>
    /// <remarks>
    /// This attribute is recognized by the runtime-provided source generators for source-generated interop scenarios.
    /// It is not used by the interop marshalling system at runtime.
    /// <seealso cref="LibraryImportAttribute"/>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Struct)]
#if LIBRARYIMPORT_GENERATOR_TEST
    public
#else
    internal
#endif
    sealed class CustomTypeMarshallerAttribute : Attribute
    {
        public CustomTypeMarshallerAttribute(Type managedType, CustomTypeMarshallerKind marshallerKind = CustomTypeMarshallerKind.Value)
        {
            ManagedType = managedType;
            MarshallerKind = marshallerKind;
        }

        /// <summary>
        /// The managed type for which the attributed type is a marshaller
        /// </summary>
        public Type ManagedType { get; }

        /// <summary>
        /// The required shape of the attributed type
        /// </summary>
        public CustomTypeMarshallerKind MarshallerKind { get; }

        /// <summary>
        /// When the <see cref="CustomTypeMarshallerFeatures.CallerAllocatedBuffer"/> flag is set on <see cref="Features"/> the size of the caller-allocated buffer in number of elements.
        /// </summary>
        public int BufferSize { get; set; }

        /// <summary>
        /// The marshalling directions this custom type marshaller supports.
        /// </summary>
        /// <remarks>Default is <see cref="CustomTypeMarshallerDirection.Ref"/></remarks>
        public CustomTypeMarshallerDirection Direction { get; set; } = CustomTypeMarshallerDirection.Ref;

        /// <summary>
        /// The optional features for the <see cref="MarshallerKind"/> that the marshaller supports.
        /// </summary>
        public CustomTypeMarshallerFeatures Features { get; set; }

        /// <summary>
        /// This type is used as a placeholder for the first generic parameter when generic parameters cannot be used
        /// to identify the managed type (i.e. when the marshaller type is generic over T and the managed type is T[])
        /// </summary>
        public struct GenericPlaceholder
        {
        }
    }
}
