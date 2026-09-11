// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public sealed class UnionTests_String : UnionTests
    {
        public UnionTests_String() : base(JsonSerializerWrapper.StringSerializer) { }

        [Union]
        public class UnionWithInParameterConstructor
        {
            public UnionWithInParameterConstructor(in int value)
            {
                Value = value;
            }

            public object Value { get; }
        }

        [Fact]
        public void UnionWithInParameterConstructor_IsRejected()
        {
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => Serializer.GetTypeInfo<UnionWithInParameterConstructor>());

            Assert.Contains(nameof(UnionWithInParameterConstructor), ex.Message);
        }
    }

    public sealed class UnionTests_AsyncStreamWithSmallBuffer : UnionTests
    {
        public UnionTests_AsyncStreamWithSmallBuffer() : base(JsonSerializerWrapper.AsyncStreamSerializerWithSmallBuffer) { }
    }

    public sealed class UnionTests_SyncStreamWithSmallBuffer : UnionTests
    {
        public UnionTests_SyncStreamWithSmallBuffer() : base(JsonSerializerWrapper.SyncStreamSerializerWithSmallBuffer) { }
    }
}
