// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Xunit;

namespace System.Text.Json.SourceGeneration.Tests
{
    public static partial class JsonSerializerContextTests
    {
        [Fact]
        public static void VariousNestingAndVisibilityLevelsAreSupported()
        {
            Assert.NotNull(PublicContext.Default);
            Assert.NotNull(NestedContext.Default);
            Assert.NotNull(NestedPublicContext.Default);
            Assert.NotNull(NestedPublicContext.NestedProtectedInternalClass.Default);
        }

        [JsonSerializable(typeof(JsonMessage))]
        internal partial class NestedContext : JsonSerializerContext { }

        [JsonSerializable(typeof(JsonMessage))]
        public partial class NestedPublicContext : JsonSerializerContext
        {
            [JsonSerializable(typeof(JsonMessage))]
            protected internal partial class NestedProtectedInternalClass : JsonSerializerContext { }
        }
    }
}
