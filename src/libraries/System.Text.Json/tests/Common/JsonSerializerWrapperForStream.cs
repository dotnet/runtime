// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Tests
{
    /// <summary>
    /// Base class for wrapping Stream-based JsonSerializer methods which allows tests to run under different configurations.
    /// </summary>
    public abstract partial class JsonSerializerWrapperForStream
    {
        protected internal abstract Task SerializeWrapper<T>(Stream stream, T value, JsonSerializerOptions options = null);
        protected internal abstract Task SerializeWrapper(Stream stream, object value, Type inputType, JsonSerializerOptions options = null);
        protected internal abstract Task SerializeWrapper<T>(Stream stream, T value, JsonTypeInfo<T> jsonTypeInfo);
        protected internal abstract Task<T> DeserializeWrapper<T>(Stream utf8Json, JsonSerializerOptions options = null);
        protected internal abstract Task<object> DeserializeWrapper(Stream utf8Json, Type returnType, JsonSerializerOptions options = null);
        protected internal abstract Task<T> DeserializeWrapper<T>(Stream utf8Json, JsonTypeInfo<T> jsonTypeInfo);
    }
}
