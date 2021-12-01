// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class JsonSerializerWrapperForString
    {
        /// <summary>
        /// Do the deserialize methods allow a value of 'null'.
        /// For example, deserializing JSON to a String supports null by returning a 'null' String reference from a literal value of "null".
        /// </summary>
        protected internal abstract bool SupportsNullValueOnDeserialize { get; }

        protected internal abstract Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerOptions options = null);

        protected internal abstract Task<string> SerializeWrapper<T>(T value, JsonSerializerOptions options = null);

        protected internal abstract Task<string> SerializeWrapper(object value, Type inputType, JsonSerializerContext context);

        protected internal abstract Task<string> SerializeWrapper<T>(T value, JsonTypeInfo<T> jsonTypeInfo);

        protected internal abstract Task<T> DeserializeWrapper<T>(string json, JsonSerializerOptions options = null);

        protected internal abstract Task<object> DeserializeWrapper(string json, Type type, JsonSerializerOptions options = null);

        protected internal abstract Task<T> DeserializeWrapper<T>(string json, JsonTypeInfo<T> jsonTypeInfo);

        protected internal abstract Task<object> DeserializeWrapper(string json, Type type, JsonSerializerContext context);
    }
}
