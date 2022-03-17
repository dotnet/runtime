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

    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
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

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Field, AllowMultiple = true)]
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

        public int ElementIndirectionLevel { get; set; }

        public const string ReturnsCountValue = "return-value";
    }

    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
#if LIBRARYIMPORT_GENERATOR_TEST
    public
#else
    internal
#endif
    sealed class GenericContiguousCollectionMarshallerAttribute : Attribute
    {
        public GenericContiguousCollectionMarshallerAttribute()
        {
        }
    }
}
