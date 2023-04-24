// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization.Tests
{
    internal class TestResolver : IJsonTypeInfoResolver
    {
        private Func<Type, JsonSerializerOptions, JsonTypeInfo?> _getTypeInfo;

        public TestResolver(Func<Type, JsonSerializerOptions, JsonTypeInfo> getTypeInfo)
        {
            _getTypeInfo = getTypeInfo;
        }

        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            return _getTypeInfo(type, options);
        }
    }
}
