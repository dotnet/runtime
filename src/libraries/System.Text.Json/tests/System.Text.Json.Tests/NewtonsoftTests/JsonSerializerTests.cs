// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Tests
{
    public class IncompleteTestClass
    {
        public int Key { get; set; }
    }

    public class JsonSerializerTests
    {
        [Fact]
        public void DeserializeBoolean_Null()
        {
            Assert.Throws<JsonException>(
                () => JsonSerializer.Deserialize<IList<bool>>(@"[null]"));
        }

        [Fact]
        public void DeserializeBoolean_DateTime()
        {
            Assert.Throws<JsonException>(
                () => JsonSerializer.Deserialize<IList<bool>>(@"['2000-12-20T10:55:55Z']"));
        }

        [Fact]
        public void DeserializeBoolean_BadString()
        {
            Assert.Throws<JsonException>(
                () => JsonSerializer.Deserialize<IList<bool>>(@"['pie']"));
        }

        [Fact]
        public void DeserializeBoolean_EmptyString()
        {
            Assert.Throws<JsonException>(
                () => JsonSerializer.Deserialize<IList<bool>>(@"['']"));
        }

        [Fact]
        public void IncompleteContainers()
        {
            JsonException e = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IList<object>>("[1,"));
            Assert.Contains("Path: $[1] | LineNumber: 0 | BytePositionInLine: 2.", e.Message);

            e = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IList<int>>("[1,"));
            Assert.Contains("Path: $[1] | LineNumber: 0 | BytePositionInLine: 2.", e.Message);

            e = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IList<int>>("[1"));
            Assert.Contains("Path: $[0] | LineNumber: 0 | BytePositionInLine: 2.", e.Message);

            e = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IDictionary<string, int>>("{\"key\":1,"));
            Assert.Contains("Path: $.key | LineNumber: 0 | BytePositionInLine: 8.", e.Message);

            e = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IDictionary<string, int>>("{\"key\":1"));
            Assert.Contains("Path: $.key | LineNumber: 0 | BytePositionInLine: 8.", e.Message);

            e = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IncompleteTestClass>("{\"key\":1,"));
            Assert.Contains("$ | LineNumber: 0 | BytePositionInLine: 8.", e.Message);
        }

        [Fact]
        public void NewProperty()
        {
            Assert.Equal(@"{""IsTransient"":true}", JsonSerializer.Serialize(new ChildClass { IsTransient = true }));

            ChildClass childClass = JsonSerializer.Deserialize<ChildClass>(@"{""IsTransient"":true}");
            Assert.True(childClass.IsTransient);
        }

        [Fact]
        public void NewPropertyVirtual()
        {
            Assert.Equal(@"{""IsTransient"":true}", JsonSerializer.Serialize(new ChildClassVirtual { IsTransient = true }));

            ChildClassVirtual childClass = JsonSerializer.Deserialize<ChildClassVirtual>(@"{""IsTransient"":true}");
            Assert.True(childClass.IsTransient);
        }

        [Fact]
        public void DeserializeCommentTestObjectWithComments()
        {
            CommentTestObject o = JsonSerializer.Deserialize<CommentTestObject>(@"{/* Test */}", new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip });
            Assert.False(o.A);

            o = JsonSerializer.Deserialize<CommentTestObject>(@"{""A"": true/* Test */}", new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip });
            Assert.True(o.A);
        }

        [Fact]
        public void PreserveReferencesCallbackTest()
        {
            PersonReference p1 = new PersonReference
            {
                Name = "John Smith"
            };
            PersonReference p2 = new PersonReference
            {
                Name = "Mary Sue",
            };

            p1.Spouse = p2;
            p2.Spouse = p1;
            Assert.Throws<JsonException> (() => JsonSerializer.Serialize(p1));
        }
    }

    public class PersonReference
    {
        internal Guid Id { get; set; }
        public string Name { get; set; }
        public PersonReference Spouse { get; set; }
    }

    public class CommentTestObject
    {
        public bool A { get; set; }
    }

    public class ChildClassVirtual : BaseClassVirtual
    {
        public new virtual bool IsTransient { get; set; }
    }

    public class BaseClassVirtual
    {
        internal virtual bool IsTransient { get; set; }
    }

    public class BaseClass
    {
        internal bool IsTransient { get; set; }
    }

    public class ChildClass : BaseClass
    {
        public new bool IsTransient { get; set; }
    }
}
