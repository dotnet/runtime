// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public partial class DefaultJsonTypeInfoResolverMultiContextTests : SerializerTests
    {
        public DefaultJsonTypeInfoResolverMultiContextTests()
            : base(JsonSerializerWrapper.StringSerializer)
        {
        }

        [Fact]
        public async Task TypeInfoWithNullCreateObjectFailsDeserialization()
        {
            DefaultJsonTypeInfoResolver resolver = new();
            resolver.Modifiers.Add(ti =>
            {
                if (ti.Type == typeof(Poco))
                {
                    ti.CreateObject = null;
                }
            });

            JsonSerializerOptions o = new();
            o.TypeInfoResolver = resolver;

            string json = """{"StringProperty":"test"}""";
            await TestMultiContextDeserialization<Poco>(json, options: o, expectedExceptionType: typeof(NotSupportedException));
        }

        private class Poco
        {
            public string StringProperty { get; set; }
        }
    }
}
