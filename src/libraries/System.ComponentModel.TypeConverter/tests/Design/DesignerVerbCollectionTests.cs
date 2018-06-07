// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.ComponentModel.Design.Tests
{
    public class DesignerVerbCollectionTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var collection = new DesignerVerbCollection();
            Assert.Equal(0, collection.Count);
            Assert.Empty(collection);
        }

        [Fact]
        public void Ctor_Value()
        {
            var value = new DesignerVerb[] { new DesignerVerb("Text", null), new DesignerVerb("Text", null) };
            var collection = new DesignerVerbCollection(value);
            Assert.Equal(2, collection.Count);
            Assert.Equal(value, collection.Cast<DesignerVerb>());
        }

        [Fact]
        public void Ctor_NullValue_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => new DesignerVerbCollection(null));
        }

        [Fact]
        public void Add_ValidValue_Success()
        {
            var verb = new DesignerVerb("Text", null);
            var collection = new DesignerVerbCollection { verb, null };
            Assert.Equal(2, collection.Count);
            Assert.Same(verb, collection[0]);
            Assert.Null(collection[1]);
        }

        [Fact]
        public void Insert_ValidValue_Success()
        {
            var verb = new DesignerVerb("Text", null);
            var collection = new DesignerVerbCollection { new DesignerVerb("Text", null) };
            collection.Insert(0, verb);
            Assert.Equal(2, collection.Count);
            Assert.Same(verb, collection[0]);

            collection.Insert(0, null);
            Assert.Equal(3, collection.Count);
            Assert.Null(collection[0]);
        }

        [Fact]
        public void Remove_ValidValue_Success()
        {
            var verb = new DesignerVerb("Text", null);
            var collection = new DesignerVerbCollection { verb };
            collection.Remove(verb);
            Assert.Empty(collection);

            collection.Add(null);
            collection.Remove(null);
            Assert.Empty(collection);
        }

        [Fact]
        public void Item_SetValidValue_Success()
        {
            var verb = new DesignerVerb("Text", null);
            var collection = new DesignerVerbCollection { new DesignerVerb("Text", null) };
            collection[0] = verb;

            Assert.Equal(1, collection.Count);
            Assert.Same(verb, collection[0]);

            collection[0] = null;
            Assert.Null(collection[0]);
        }

        [Fact]
        public void AddRange_DesignerVerbArray_Success()
        {
            var value = new DesignerVerb[] { new DesignerVerb("Text", null), new DesignerVerb("Text", null) };
            var collection = new DesignerVerbCollection();
            collection.AddRange(value);

            Assert.Equal(2, collection.Count);
            Assert.Equal(value, collection.Cast<DesignerVerb>());
        }

        [Fact]
        public void AddRange_DesignerVerbCollection_Success()
        {
            var value = new DesignerVerb[] { new DesignerVerb("Text", null), new DesignerVerb("Text", null) };
            var collection = new DesignerVerbCollection();
            collection.AddRange(new DesignerVerbCollection(value));

            Assert.Equal(2, collection.Count);
            Assert.Equal(value, collection.Cast<DesignerVerb>());
        }

        [Fact]
        public void AddRange_ThisDesignerVerbCollection_Success()
        {
            var value = new DesignerVerb[] { new DesignerVerb("Text", null), new DesignerVerb("Text", null) };
            var collection = new DesignerVerbCollection(value);
            collection.AddRange(collection);

            Assert.Equal(4, collection.Count);
            Assert.Equal(value.Concat(value), collection.Cast<DesignerVerb>());
        }

        [Fact]
        public void AddRange_NullValue_ThrowsArgumentNullException()
        {
            var collection = new DesignerVerbCollection();
            AssertExtensions.Throws<ArgumentNullException>("value", () => collection.AddRange((DesignerVerb[])null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => collection.AddRange((DesignerVerbCollection)null));
        }

        [Fact]
        public void Contains_Value_ReturnsExpected()
        {
            var verb = new DesignerVerb("Text", null);
            var collection = new DesignerVerbCollection { verb };

            Assert.True(collection.Contains(verb));
            Assert.False(collection.Contains(new DesignerVerb("Text", null)));
            Assert.False(collection.Contains(null));
        }

        [Fact]
        public void IndexOf_Value_ReturnsExpected()
        {
            var verb = new DesignerVerb("Text", null);
            var collection = new DesignerVerbCollection { verb };

            Assert.Equal(0, collection.IndexOf(verb));
            Assert.Equal(-1, collection.IndexOf(new DesignerVerb("Text", null)));
            Assert.Equal(-1, collection.IndexOf(null));
        }

        [Fact]
        public void CopyTo_ValidDestination_Success()
        {
            var verb = new DesignerVerb("Text", null);
            var collection = new DesignerVerbCollection { verb, verb };

            var destination = new DesignerVerb[3];
            collection.CopyTo(destination, 1);
            Assert.Equal(new DesignerVerb[] { null, verb, verb }, destination);
        }
    }
}
