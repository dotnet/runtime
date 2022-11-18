// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Tests;

namespace System.Text.Json.SourceGeneration.Tests
{
    public sealed partial class UnsupportedTypesTests_Metadata : UnsupportedTypesTests
    {
        public UnsupportedTypesTests_Metadata() : base(
            new StringSerializerWrapper(
                UnsupportedTypesTestsContext_Metadata.Default,
                (options) => new UnsupportedTypesTestsContext_Metadata(options)),
            supportsJsonPathOnSerialize: true)
        {
        }

        [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
        // Supported types:
        [JsonSerializable(typeof(int))]
        [JsonSerializable(typeof(ClassWithIntPtrConverter))]
        // Unsupported types:
        [JsonSerializable(typeof(Type))]
        [JsonSerializable(typeof(ClassWithType<Type>))]
        [JsonSerializable(typeof(ConstructorInfo))]
        [JsonSerializable(typeof(ClassWithType<ConstructorInfo>))]
        [JsonSerializable(typeof(PropertyInfo))]
        [JsonSerializable(typeof(ClassWithType<PropertyInfo>))]
        [JsonSerializable(typeof(SerializationInfo))]
        [JsonSerializable(typeof(ClassWithType<SerializationInfo>))]
        [JsonSerializable(typeof(IntPtr))]
        [JsonSerializable(typeof(ClassWithType<IntPtr>))]
        [JsonSerializable(typeof(ClassWithIntPtr))]
        [JsonSerializable(typeof(IntPtr?))]
        [JsonSerializable(typeof(ClassWithType<IntPtr?>))]
        [JsonSerializable(typeof(UIntPtr))]
        [JsonSerializable(typeof(ClassWithType<UIntPtr>))]
        [JsonSerializable(typeof(IAsyncEnumerable<int>))]
        [JsonSerializable(typeof(ClassWithType<IAsyncEnumerable<int>>))]
        [JsonSerializable(typeof(ClassThatImplementsIAsyncEnumerable))]
        [JsonSerializable(typeof(ClassWithType<ClassThatImplementsIAsyncEnumerable>))]
        [JsonSerializable(typeof(ClassWithAsyncEnumerableConverter))]
        internal sealed partial class UnsupportedTypesTestsContext_Metadata : JsonSerializerContext
        {
        }
    }

    public sealed partial class UnsupportedTypesTests_Default : UnsupportedTypesTests
    {
        public UnsupportedTypesTests_Default() : base(
            new StringSerializerWrapper(
                UnsupportedTypesTestsContext_Default.Default,
                (options) => new UnsupportedTypesTestsContext_Default(options)),
            supportsJsonPathOnSerialize: false)
        {
        }

        // Supported types:
        [JsonSerializable(typeof(int))]
        [JsonSerializable(typeof(ClassWithIntPtrConverter))]
        // Unsupported types:
        [JsonSerializable(typeof(Type))]
        [JsonSerializable(typeof(ClassWithType<Type>))]
        [JsonSerializable(typeof(ConstructorInfo))]
        [JsonSerializable(typeof(ClassWithType<ConstructorInfo>))]
        [JsonSerializable(typeof(PropertyInfo))]
        [JsonSerializable(typeof(ClassWithType<PropertyInfo>))]
        [JsonSerializable(typeof(SerializationInfo))]
        [JsonSerializable(typeof(ClassWithType<SerializationInfo>))]
        [JsonSerializable(typeof(IntPtr))]
        [JsonSerializable(typeof(ClassWithType<IntPtr>))]
        [JsonSerializable(typeof(ClassWithIntPtr))]
        [JsonSerializable(typeof(IntPtr?))]
        [JsonSerializable(typeof(ClassWithType<IntPtr?>))]
        [JsonSerializable(typeof(UIntPtr))]
        [JsonSerializable(typeof(ClassWithType<UIntPtr>))]
        [JsonSerializable(typeof(IAsyncEnumerable<int>))]
        [JsonSerializable(typeof(ClassWithType<IAsyncEnumerable<int>>))]
        [JsonSerializable(typeof(ClassThatImplementsIAsyncEnumerable))]
        [JsonSerializable(typeof(ClassWithType<ClassThatImplementsIAsyncEnumerable>))]
        [JsonSerializable(typeof(ClassWithAsyncEnumerableConverter))]
        internal sealed partial class UnsupportedTypesTestsContext_Default : JsonSerializerContext
        {
        }
    }
}
