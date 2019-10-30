// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public class DirectoryControlCollectionTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var collection = new DirectoryControlCollection();
            Assert.Equal(0, collection.Count);
            Assert.Equal(0, collection.Capacity);
        }

        [Fact]
        public void Indexer_Set_GetReturnsExpected()
        {
            var control = new AsqRequestControl("name");
            var collection = new DirectoryControlCollection { new AsqRequestControl() };
            collection[0] = control;
            Assert.Equal(control, collection[0]);
        }

        [Fact]
        public void Indexer_SetNull_ThrowsArgumentException()
        {
            var collection = new DirectoryControlCollection();
            AssertExtensions.Throws<ArgumentNullException>("value", () => collection[0] = null);
        }

        [Fact]
        public void Add_ValidControl_AppendsToList()
        {
            var control1 = new AsqRequestControl("name1");
            var control2 = new AsqRequestControl("name2");
            var collection = new DirectoryControlCollection { control1, control2 };
            Assert.Equal(2, collection.Count);
            Assert.Equal(new DirectoryControl[] { control1, control2 }, collection.Cast<DirectoryControl>());
        }

        [Fact]
        public void Add_NullControl_ThrowsArgumentNullException()
        {
            var collection = new DirectoryControlCollection();
            AssertExtensions.Throws<ArgumentNullException>("control", () => collection.Add(null));
        }

        [Fact]
        public void AddRange_ValidControls_AddsToCollection()
        {
            DirectoryControl[] controls = new DirectoryControl[] { new AsqRequestControl(), new AsqRequestControl() };

            var collection = new DirectoryControlCollection();
            collection.AddRange(controls);

            Assert.Equal(controls, collection.Cast<DirectoryControl>());
        }

        [Fact]
        public void AddRange_NullControls_ThrowsArgumentNullException()
        {
            var collection = new DirectoryControlCollection();
            AssertExtensions.Throws<ArgumentNullException>("controls", () => collection.AddRange((DirectoryControl[])null));
        }

        [Fact]
        public void AddRange_NullObjectInValues_ThrowsArgumentException()
        {
            DirectoryControl[] controls = new DirectoryControl[] { new AsqRequestControl(), null, new AsqRequestControl() };
            var collection = new DirectoryControlCollection();

            AssertExtensions.Throws<ArgumentException>("controls", () => collection.AddRange(controls));
            Assert.Equal(0, collection.Count);
        }

        [Fact]
        public void AddRange_ValidControlCollection_AddsToCollection()
        {
            DirectoryControl[] controls = new DirectoryControl[] { new AsqRequestControl(), new AsqRequestControl() };
            var attributeCollection = new DirectoryControlCollection();
            attributeCollection.AddRange(controls);

            var collection = new DirectoryControlCollection();
            collection.AddRange(attributeCollection);

            Assert.Equal(controls, collection.Cast<DirectoryControl>());
        }

        [Fact]
        public void AddRange_NullControlCollection_ThrowsArgumentNullException()
        {
            var collection = new DirectoryControlCollection();
            AssertExtensions.Throws<ArgumentNullException>("controlCollection", () => collection.AddRange((DirectoryControlCollection)null));
        }

        [Fact]
        public void Contains_Valid_ReturnsExpected()
        {
            var control = new AsqRequestControl("name");
            var collection = new DirectoryControlCollection { control };
            Assert.True(collection.Contains(control));
            Assert.False(collection.Contains(new AsqRequestControl()));
            Assert.False(collection.Contains(null));
        }

        [Fact]
        public void CopyTo_MultipleTypes_Success()
        {
            DirectoryControl[] array = new DirectoryControl[3];
            var control = new AsqRequestControl("name");

            var collection = new DirectoryControlCollection { control };
            collection.CopyTo(array, 1);
            Assert.Equal(new DirectoryControl[] { null, control, null }, array);
        }

        [Fact]
        public void Insert_ValidDirectoryAttribute_Success()
        {
            var control1 = new AsqRequestControl("name1");
            var control2 = new AsqRequestControl("name1");
            var collection = new DirectoryControlCollection();
            collection.Insert(0, control1);
            collection.Insert(1, control2);

            Assert.Equal(new DirectoryControl[] { control1, control2 }, collection.Cast<DirectoryControl>());
        }

        [Fact]
        public void Insert_NullValue_ThrowsArgumentNullException()
        {
            var collection = new DirectoryControlCollection();
            AssertExtensions.Throws<ArgumentNullException>("value", () => collection.Insert(0, null));
        }

        [Fact]
        public void IndexOf_Valid_ReturnsExpected()
        {
            var control = new AsqRequestControl("name");
            var collection = new DirectoryControlCollection { control };
            Assert.Equal(0, collection.IndexOf(control));
            Assert.Equal(-1, collection.IndexOf(new AsqRequestControl()));
            Assert.Equal(-1, collection.IndexOf(null));
        }

        [Fact]
        public void Remove_Valid_Success()
        {
            var control = new AsqRequestControl("name");
            var collection = new DirectoryControlCollection { control };
            collection.Remove(control);
            Assert.Empty(collection);
        }

        [Fact]
        public void Remove_NullValue_ThrowsArgumentNullException()
        {
            var collection = new DirectoryControlCollection { new AsqRequestControl() };
            AssertExtensions.Throws<ArgumentNullException>("value", () => collection.Remove(null));
        }

        public static IEnumerable<object[]> Remove_InvalidValue_TestData()
        {
            yield return new object[] { new AsqRequestControl(), null };
            yield return new object[] { 1, "value" };
        }

        [Theory]
        [MemberData(nameof(Remove_InvalidValue_TestData))]
        public void Remove_InvalidValue_ThrowsArgumentException(object value, string paramName)
        {
            IList collection = new DirectoryControlCollection { new AsqRequestControl() };
            AssertExtensions.Throws<ArgumentException>(paramName, () => collection.Remove(value));
        }
    }
}
