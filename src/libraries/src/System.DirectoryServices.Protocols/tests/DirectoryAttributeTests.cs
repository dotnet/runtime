// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public class DirectoryAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new DirectoryAttribute();
            Assert.Empty(attribute.Name);
            Assert.Equal(0, attribute.Count);
            Assert.Equal(0, attribute.Capacity);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("name", "value")]
        public void Ctor_Name_StringValue(string name, string value)
        {
            var attribute = new DirectoryAttribute(name, value);
            Assert.Equal(name, attribute.Name);
            Assert.Equal(value, Assert.Single(attribute));

            Assert.Equal(1, attribute.Count);
            Assert.Equal(4, attribute.Capacity);
        }


        [Theory]
        [InlineData("", new byte[] { 1, 2, 3 })]
        [InlineData("name", new byte[] { 1, 2, 3 })]
        public void Ctor_Name_ByteArrayValue(string name, byte[] value)
        {
            var attribute = new DirectoryAttribute(name, value);
            Assert.Equal(name, attribute.Name);
            Assert.Equal(value, Assert.Single(attribute));

            Assert.Equal(1, attribute.Count);
            Assert.Equal(4, attribute.Capacity);
        }

        [Fact]
        public void Ctor_Name_UriValue()
        {
            var uri = new Uri("http://microsoft.com");
            var attribute = new DirectoryAttribute("Name", uri);
            Assert.Equal("Name", attribute.Name);
            Assert.Equal(uri, Assert.Single(attribute));

            Assert.Equal(1, attribute.Count);
            Assert.Equal(4, attribute.Capacity);
        }

        [Fact]
        public void Ctor_NullName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("name", () => new DirectoryAttribute(null, ""));
            AssertExtensions.Throws<ArgumentNullException>("name", () => new DirectoryAttribute(null, new byte[0]));
            AssertExtensions.Throws<ArgumentNullException>("name", () => new DirectoryAttribute(null, new Uri("http://microsoft.com")));
            AssertExtensions.Throws<ArgumentNullException>("name", () => new DirectoryAttribute(null, new object[0]));
        }

        [Fact]
        public void Ctor_NullValue_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => new DirectoryAttribute("Name", (string)null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => new DirectoryAttribute("Name", (byte[])null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => new DirectoryAttribute("Name", (Uri)null));
        }

        [Fact]
        public void Ctor_Name_Values()
        {
            object[] values = new object[] { "value", new byte[] { 1, 2, 3 }, new Uri("http://microsoft.com") };

            var attribute = new DirectoryAttribute("Name", values);
            Assert.Equal("Name", attribute.Name);
            Assert.Equal(3, attribute.Count);
            Assert.Equal(values, attribute.Cast<object>());
        }

        [Fact]
        public void Ctor_NullValues_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("values", () => new DirectoryAttribute("Name", (object[])null));
        }

        [Fact]
        public void Ctor_NullObjectValues_ThrowsArgumentNullException()
        {
            string[] values = new string[] { "value1", null, "value2" };
            AssertExtensions.Throws<ArgumentNullException>("value", () => new DirectoryAttribute("Name", values));
        }

        [Fact]
        public void Ctor_InvalidObjectInValues_ThrowsArgumentException()
        {
            object[] values = new object[] { 1 };
            AssertExtensions.Throws<ArgumentException>("value", () => new DirectoryAttribute("Name", values));
        }

        [Fact]
        public void Name_Set_GetReturnsExpected()
        {
            var attribute = new DirectoryAttribute { Name = "Name" };
            Assert.Equal("Name", attribute.Name);
        }

        [Fact]
        public void Name_SetNull_ThrowsArgumentNullException()
        {
            var attribute = new DirectoryAttribute();
            AssertExtensions.Throws<ArgumentNullException>("value", () => attribute.Name = null);
        }

        [Fact]
        public void GetValues_Mixed_Success()
        {
            var attribute = new DirectoryAttribute { "abc", new byte[] { 100, 101, 102 } };
            Assert.Equal(new byte[][] { new byte[] { 97, 98, 99 }, new byte[] { 100, 101, 102 } }, attribute.GetValues(typeof(byte[])));
            Assert.Equal(new string[] { "abc", "def" }, attribute.GetValues(typeof(string)));
        }

        [Fact]
        public void GetValues_ContainsUri_ThrowsNotSupportedException()
        {
            var attribute = new DirectoryAttribute { "abc", new byte[] { 100, 101, 102 }, new Uri("http://microsoft.com") };
            Assert.Throws<NotSupportedException>(() => attribute.GetValues(typeof(byte[])));
            Assert.Throws<NotSupportedException>(() => attribute.GetValues(typeof(string)));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(typeof(Uri))]
        [InlineData(typeof(int))]
        public void GetValues_InvalidType_ThrowsArgumentException(Type valuesType)
        {
            var attribute = new DirectoryAttribute();
            AssertExtensions.Throws<ArgumentException>("valuesType", () => attribute.GetValues(valuesType));
        }

        [Fact]
        public void Indexer_SetMultipleType_GetReturnsExpected()
        {
            var attribute = new DirectoryAttribute { "value" };

            attribute[0] = new Uri("http://microsoft.com");
            Assert.Equal(new Uri("http://microsoft.com"), attribute[0]);

            attribute[0] = "value";
            Assert.Equal("value", attribute[0]);

            attribute[0] = new byte[] { 1, 2, 3 };
            Assert.Equal(new byte[] { 1, 2, 3 }, attribute[0]);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(1)]
        public void Indexer_InvalidIndex_ThrowsArgumentOutOfRangeException(int index)
        {
            var attribute = new DirectoryAttribute { "value" };
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => attribute[index]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => attribute[index] = "value");
        }

        [Fact]
        public void Indexer_SetNull_ThrowsArgumentNullException()
        {
            var attribute = new DirectoryAttribute();
            AssertExtensions.Throws<ArgumentNullException>("value", () => attribute[0] = null);
        }

        [Fact]
        public void Indexer_SetInvalidType_ThrowsArgumentException()
        {
            var attribute = new DirectoryAttribute();
            AssertExtensions.Throws<ArgumentException>("value", () => attribute[0] = 1);
        }

        [Fact]
        public void Add_MultipleTypes_Success()
        {
            var attribute = new DirectoryAttribute { "value", new byte[] { 1, 2, 3 }, new Uri("http://microsoft.com") };
            Assert.Equal(3, attribute.Count);
            Assert.Equal(new object[] { "value", new byte[] { 1, 2, 3 }, new Uri("http://microsoft.com") }, attribute.Cast<object>());
        }

        [Fact]
        public void Add_NullValue_ThrowsArgumentNullException()
        {
            var attribute = new DirectoryAttribute();
            AssertExtensions.Throws<ArgumentNullException>("value", () => attribute.Add((string)null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => attribute.Add((byte[])null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => attribute.Add((Uri)null));
        }

        [Fact]
        public void AddRange_Strings_Success()
        {
            string[] values = new string[] { "value1", "value2" };
            var attribute = new DirectoryAttribute();
            attribute.AddRange(values);
            Assert.Equal(2, attribute.Count);
            Assert.Equal(values, attribute.Cast<string>());
        }

        [Fact]
        public void AddRange_ByteArrays_Success()
        {
            byte[][] values = new byte[][] { new byte[] { 1, 2, 3 }, new byte[0] };
            var attribute = new DirectoryAttribute();
            attribute.AddRange(values);
            Assert.Equal(2, attribute.Count);
            Assert.Equal(values, attribute.Cast<byte[]>());
        }

        [Fact]
        public void AddRange_Uris_Success()
        {
            Uri[] values = new Uri[] { new Uri("http://microsoft.com") };
            var attribute = new DirectoryAttribute();
            attribute.AddRange(values);
            Assert.Equal(1, attribute.Count);
            Assert.Equal(values, attribute.Cast<Uri>());
        }

        [Fact]
        public void AddRange_NullValues_ThrowsArgumentNullException()
        {
            var attribute = new DirectoryAttribute();
            AssertExtensions.Throws<ArgumentNullException>("values", () => attribute.AddRange(null));
        }

        [Fact]
        public void AddRange_InvalidArray_ThrowsArgumentExceptionn()
        {
            var attribute = new DirectoryAttribute();
            AssertExtensions.Throws<ArgumentException>("values", () => attribute.AddRange(new object[0]));
        }

        [Fact]
        public void AddRange_NullObjectInValues_ThrowsArgumentException()
        {
            string[] objects = new string[] { "value1", null, "value2" };
            var attribute = new DirectoryAttribute();

            AssertExtensions.Throws<ArgumentException>("values", () => attribute.AddRange(objects));
            Assert.Equal(0, attribute.Count);
        }

        [Fact]
        public void Contains_Valid_ReturnsExpected()
        {
            var attribute = new DirectoryAttribute { "value" };
            Assert.True(attribute.Contains("value"));
            Assert.False(attribute.Contains("vaLue"));
            Assert.False(attribute.Contains(null));
        }

        [Fact]
        public void CopyTo_MultipleTypes_Success()
        {
            object[] array = new object[5];
            var attribute = new DirectoryAttribute { "value", new byte[] { 1, 2, 3 }, new Uri("http://microsoft.com") };
            attribute.CopyTo(array, 1);
            Assert.Equal(new object[] { null, "value", new byte[] { 1, 2, 3 }, new Uri("http://microsoft.com"), null }, array);
        }

        [Fact]
        public void Insert_MultipleTypes_Success()
        {
            var attribute = new DirectoryAttribute { "value2" };
            attribute.Insert(0, "value1");
            attribute.Insert(1, new byte[] { 1, 2, 3 });
            attribute.Insert(3, new Uri("http://microsoft.com"));

            Assert.Equal(new object[] { "value1", new byte[] { 1, 2, 3 }, "value2", new Uri("http://microsoft.com") }, attribute.Cast<object>());
        }

        [Fact]
        public void Insert_NullValue_ThrowsArgumentNullException()
        {
            var attribute = new DirectoryAttribute();
            AssertExtensions.Throws<ArgumentNullException>("value", () => attribute.Insert(0, (string)null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => attribute.Insert(0, (byte[])null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => attribute.Insert(0, (Uri)null));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        public void Insert_InvalidIndex_ThrowsArgumentOutOfRangeException(int index)
        {
            var attribute = new DirectoryAttribute { "value2" };
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => attribute.Insert(index, "value"));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => attribute.Insert(index, new byte[] { 1, 2, 3 }));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => attribute.Insert(index, new Uri("http://microsoft.com")));
        }

        [Fact]
        public void IndexOf_Valid_ReturnsExpected()
        {
            var attribute = new DirectoryAttribute { "value" };
            Assert.Equal(0, attribute.IndexOf("value"));
            Assert.Equal(-1, attribute.IndexOf("vaLue"));
            Assert.Equal(-1, attribute.IndexOf(null));
        }

        [Fact]
        public void Remove_Valid_Success()
        {
            var attribute = new DirectoryAttribute { "value" };
            attribute.Remove("value");
            Assert.Empty(attribute);
        }

        [Fact]
        public void Remove_NullValue_ThrowsArgumentNullException()
        {
            var attribute = new DirectoryAttribute();
            AssertExtensions.Throws<ArgumentNullException>("value", () => attribute.Remove(null));
        }

        [Theory]
        [InlineData("vaLue", null)]
        [InlineData(1, "value")]
        public void Remove_InvalidValue_ThrowsArgumentException(object value, string paramName)
        {
            var attribute = new DirectoryAttribute { "value" };
            AssertExtensions.Throws<ArgumentException>(paramName, () => attribute.Remove(value));
        }
    }
}
