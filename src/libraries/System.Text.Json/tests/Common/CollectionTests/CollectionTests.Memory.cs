// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class CollectionTests
    {
        private static readonly byte[] s_testData = "This is some test data!!!"u8.ToArray();

        [Fact]
        public async Task SerializeMemoryOfTAsync()
        {
            Memory<int> memoryInt = new int[] { 1, 2, 3 }.AsMemory();
            Assert.Equal("[1,2,3]", await Serializer.SerializeWrapper(memoryInt));

            Memory<EmptyClass> memoryPoco = new EmptyClass[] { new(), new(), new() }.AsMemory();
            Assert.Equal("[{},{},{}]", await Serializer.SerializeWrapper(memoryPoco));
        }

        [Fact]
        public async Task SerializeReadOnlyMemoryOfTAsync()
        {
            ReadOnlyMemory<int> memoryInt = new int[] { 1, 2, 3 }.AsMemory();
            Assert.Equal("[1,2,3]", await Serializer.SerializeWrapper(memoryInt));

            ReadOnlyMemory<EmptyClass> memoryPoco = new EmptyClass[] { new(), new(), new() }.AsMemory();
            Assert.Equal("[{},{},{}]", await Serializer.SerializeWrapper(memoryPoco));
        }

        [Fact]
        public async Task DeserializeMemoryOfTAsync()
        {
            Memory<int> memoryInt = await Serializer.DeserializeWrapper<Memory<int>>("[1,2,3]");
            AssertExtensions.SequenceEqual(new int[] { 1, 2, 3 }.AsSpan(), memoryInt.Span);

            Memory<EmptyClass> memoryPoco = new EmptyClass[] { new(), new(), new() }.AsMemory();
            Assert.Equal(3, memoryPoco.Length);
        }

        [Fact]
        public async Task DeserializeReadOnlyMemoryOfTAsync()
        {
            ReadOnlyMemory<int> memoryInt = await Serializer.DeserializeWrapper<ReadOnlyMemory<int>>("[1,2,3]");
            AssertExtensions.SequenceEqual(new int[] { 1, 2, 3 }, memoryInt.Span);

            ReadOnlyMemory<EmptyClass> memoryPoco = await Serializer.DeserializeWrapper<ReadOnlyMemory<EmptyClass>>("[{},{},{}]");
            Assert.Equal(3, memoryPoco.Length);
        }

        [Fact]
        public async Task SerializeMemoryOfTClassAsync()
        {
            MemoryOfTClass<int> memoryOfIntClass = new MemoryOfTClass<int>();
            memoryOfIntClass.Memory = new int[] { 1, 2, 3 };

            string json = await Serializer.SerializeWrapper(memoryOfIntClass);
            Assert.Equal(@"{""Memory"":[1,2,3]}", json);
        }

        [Fact]
        public async Task SerializeReadOnlyMemoryOfTClassAsync()
        {
            ReadOnlyMemoryOfTClass<int> memoryOfIntClass = new ReadOnlyMemoryOfTClass<int>();
            memoryOfIntClass.ReadOnlyMemory = new int[] { 1, 2, 3 };

            string json = await Serializer.SerializeWrapper(memoryOfIntClass);
            Assert.Equal(@"{""ReadOnlyMemory"":[1,2,3]}", json);
        }

        [Fact]
        public async Task DeserializeMemoryOfTClassAsync()
        {
            string json = @"{""Memory"":[1,2,3]}";
            MemoryOfTClass<int> memoryOfIntClass = await Serializer.DeserializeWrapper<MemoryOfTClass<int>>(json);
            AssertExtensions.SequenceEqual(new int[] { 1, 2, 3 }.AsSpan(), memoryOfIntClass.Memory.Span);
        }

        [Fact]
        public async Task DeserializeReadOnlyMemoryOfTClassAsync()
        {
            string json = @"{""ReadOnlyMemory"":[1,2,3]}";
            ReadOnlyMemoryOfTClass<int> memoryOfIntClass = await Serializer.DeserializeWrapper<ReadOnlyMemoryOfTClass<int>>(json);
            AssertExtensions.SequenceEqual(new int[] { 1, 2, 3 }, memoryOfIntClass.ReadOnlyMemory.Span);
        }

        [Fact]
        public async Task SerializeMemoryByteAsync()
        {
            Assert.Equal("\"VGhpcyBpcyBzb21lIHRlc3QgZGF0YSEhIQ==\"", await Serializer.SerializeWrapper<Memory<byte>>(s_testData.AsMemory()));
            Assert.Equal("\"VGhpcyBpcyBzb21lIHRlc3QgZGF0YSEhIQ==\"", await Serializer.SerializeWrapper<ReadOnlyMemory<byte>>(s_testData.AsMemory()));
        }

        [Fact]
        public async Task DeserializeMemoryByteAsync()
        {
            Memory<byte> memory = await Serializer.DeserializeWrapper<Memory<byte>>("\"VGhpcyBpcyBzb21lIHRlc3QgZGF0YSEhIQ==\"");
            AssertExtensions.SequenceEqual(s_testData.AsSpan(), memory.Span);

            ReadOnlyMemory<byte> readOnlyMemory = await Serializer.DeserializeWrapper<ReadOnlyMemory<byte>>("\"VGhpcyBpcyBzb21lIHRlc3QgZGF0YSEhIQ==\"");
            AssertExtensions.SequenceEqual(s_testData, readOnlyMemory.Span);
        }

        [Fact]
        public async Task DeserializeNullAsMemory()
        {
            ReadOnlyMemory<int> readOnlyMemOfInt = await Serializer.DeserializeWrapper<ReadOnlyMemory<int>>("null");
            Assert.True(readOnlyMemOfInt.IsEmpty);

            Memory<int> memOfInt = await Serializer.DeserializeWrapper<Memory<int>>("null");
            Assert.True(memOfInt.IsEmpty);

            ReadOnlyMemory<byte> readOnlyMemOfByte = await Serializer.DeserializeWrapper<ReadOnlyMemory<byte>>("null");
            Assert.True(readOnlyMemOfByte.IsEmpty);

            Memory<byte> memOfByte = await Serializer.DeserializeWrapper<Memory<byte>>("null");
            Assert.True(memOfByte.IsEmpty);
        }

        [Fact]
        public async Task SerializeMemoryByteClassAsync()
        {
            MemoryOfTClass<byte> memoryOfByteClass = new MemoryOfTClass<byte>();
            memoryOfByteClass.Memory = s_testData;

            string json = await Serializer.SerializeWrapper(memoryOfByteClass);
            Assert.Equal(@"{""Memory"":""VGhpcyBpcyBzb21lIHRlc3QgZGF0YSEhIQ==""}", json);
        }

        [Fact]
        public async Task DeserializeMemoryByteClassAsync()
        {
            string json = @"{""Memory"":""VGhpcyBpcyBzb21lIHRlc3QgZGF0YSEhIQ==""}";

            MemoryOfTClass<byte> memoryOfByteClass = await Serializer.DeserializeWrapper<MemoryOfTClass<byte>>(json);
            AssertExtensions.SequenceEqual<byte>(s_testData.AsSpan(), memoryOfByteClass.Memory.Span);
        }

        [Fact]
        public async Task DeserializeMemoryFromStreamWithNullElements()
        {
            // Regression test for https://github.com/dotnet/runtime/issues/118346
            // Tests that ReadOnlyMemory/Memory converters handle null elements correctly during streaming deserialization
            if (StreamingSerializer is null)
            {
                return;
            }

            string json = """[{"Name":"Alice"},null,{"Name":"Bob"}]""";
            using var stream = new Utf8MemoryStream(json);

            ReadOnlyMemory<SimpleClass?> result = await StreamingSerializer.DeserializeWrapper<ReadOnlyMemory<SimpleClass?>>(stream);
            
            Assert.Equal(3, result.Length);
            Assert.NotNull(result.Span[0]);
            Assert.Equal("Alice", result.Span[0]!.Name);
            Assert.Null(result.Span[1]);
            Assert.NotNull(result.Span[2]);
            Assert.Equal("Bob", result.Span[2]!.Name);
        }

        [Fact]
        public async Task DeserializeReadOnlyMemoryFromStreamWithNullElements()
        {
            // Regression test for https://github.com/dotnet/runtime/issues/118346
            // Tests that Memory converters handle null elements correctly during streaming deserialization
            if (StreamingSerializer is null)
            {
                return;
            }

            string json = """[{"Name":"Alice"},null,{"Name":"Bob"}]""";
            using var stream = new Utf8MemoryStream(json);

            Memory<SimpleClass?> result = await StreamingSerializer.DeserializeWrapper<Memory<SimpleClass?>>(stream);
            
            Assert.Equal(3, result.Length);
            Assert.NotNull(result.Span[0]);
            Assert.Equal("Alice", result.Span[0]!.Name);
            Assert.Null(result.Span[1]);
            Assert.NotNull(result.Span[2]);
            Assert.Equal("Bob", result.Span[2]!.Name);
        }

        [Fact]
        public async Task DeserializeMemoryFromStreamManyElements()
        {
            // Regression test for https://github.com/dotnet/runtime/issues/118346
            // Tests that ReadOnlyMemory/Memory converters handle streaming deserialization with nulls
            if (StreamingSerializer is null)
            {
                return;
            }

            // Create a JSON array with nulls that may trigger continuation
            string json = """[{"Name":"Alice"},null,{"Name":"Bob"},null,{"Name":"Charlie"}]""";
            using var stream = new Utf8MemoryStream(json);

            ReadOnlyMemory<SimpleClass?> result = await StreamingSerializer.DeserializeWrapper<ReadOnlyMemory<SimpleClass?>>(stream);
            
            Assert.Equal(5, result.Length);
            Assert.NotNull(result.Span[0]);
            Assert.Equal("Alice", result.Span[0]!.Name);
            Assert.Null(result.Span[1]);
            Assert.NotNull(result.Span[2]);
            Assert.Equal("Bob", result.Span[2]!.Name);
            Assert.Null(result.Span[3]);
            Assert.NotNull(result.Span[4]);
            Assert.Equal("Charlie", result.Span[4]!.Name);
        }

        public class SimpleClass
        {
            public string? Name { get; set; }
        }
    }
}
