// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

//
// Types in this file are used for generated p/invokes (docs/design/features/source-generator-pinvokes.md).
//
namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
#if LIBRARYIMPORT_GENERATOR_TEST
    public
#else
    internal
#endif
    sealed class GeneratedMarshallingAttribute : Attribute
    {
    }

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

        public Type ManagedType { get; }
        public CustomTypeMarshallerKind MarshallerKind { get; }
        public int BufferSize { get; set; }

        /// <summary>
        /// This type is used as a placeholder for the first generic parameter when generic parameters cannot be used
        /// to identify the managed type (i.e. when the marshaller type is generic over T and the managed type is T[])
        /// </summary>
        public struct GenericPlaceholder
        {
        }
    }
}
