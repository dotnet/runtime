// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Metadata;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public sealed class StructuralJsonTypeClassifierTests_String : StructuralJsonTypeClassifierTests
    {
        public StructuralJsonTypeClassifierTests_String() : base(JsonSerializerWrapper.StringSerializer) { }

        [JsonUnion(TypeClassifier = typeof(NonPublicClassifierFactory))]
        public union NonPublicFactoryUnion(int, string);

        internal sealed class NonPublicClassifierFactory : JsonTypeClassifierFactory
        {
            private NonPublicClassifierFactory() { }

            public override bool CanClassify(JsonTypeClassifierContext context) => true;

            public override JsonTypeClassifier CreateJsonClassifier(
                JsonTypeClassifierContext context,
                JsonSerializerOptions options)
            {
                return static (ref Utf8JsonReader _) => null;
            }
        }

        [Fact]
        public void ReflectionMode_NonPublicClassifierFactory_IsRejected()
        {
            // [JsonUnion(TypeClassifier = ...)] uses reflection to construct the factory.
            // Activator.CreateInstance with the default access modifiers cannot instantiate
            // types whose accessible constructors are not public, so resolving the union's
            // JsonTypeInfo must surface a clear failure instead of silently dropping the
            // declared classifier.
            Assert.ThrowsAny<Exception>(
                () => Serializer.GetTypeInfo<NonPublicFactoryUnion>());
        }
    }

    public sealed class StructuralJsonTypeClassifierTests_AsyncStreamWithSmallBuffer : StructuralJsonTypeClassifierTests
    {
        public StructuralJsonTypeClassifierTests_AsyncStreamWithSmallBuffer() : base(JsonSerializerWrapper.AsyncStreamSerializerWithSmallBuffer) { }
    }

    public sealed class StructuralJsonTypeClassifierTests_SyncStreamWithSmallBuffer : StructuralJsonTypeClassifierTests
    {
        public StructuralJsonTypeClassifierTests_SyncStreamWithSmallBuffer() : base(JsonSerializerWrapper.SyncStreamSerializerWithSmallBuffer) { }
    }
}
