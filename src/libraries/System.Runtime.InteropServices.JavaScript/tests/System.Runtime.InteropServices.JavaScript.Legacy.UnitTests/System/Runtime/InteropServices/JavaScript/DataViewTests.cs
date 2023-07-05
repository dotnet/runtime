// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public static class DataViewTests
    {
        [Fact]
        public static void DataViewConstructor()
        {
            // create an ArrayBuffer with a size in bytes
            var buffer = new ArrayBuffer(16);

            // Create a couple of views
            var view1 = new DataView(buffer);
            var view2 = new DataView(buffer, 12, 4); //from byte 12 for the next 4 bytes
            view1.SetInt8(12, 42); // put 42 in slot 12 

            Assert.Equal(42, view2.GetInt8(0));
        }

        public static IEnumerable<object[]> ArrayBuffer_Test_Data()
        {
            yield return new object[] { new ArrayBuffer(12) };
        }

        [Theory]
        [MemberData(nameof(ArrayBuffer_Test_Data))]
        public static void DataViewArrayBuffer(ArrayBuffer buffer)
        {
            var x = new DataView(buffer);
            Assert.True(buffer == x.Buffer);
        }

        [Theory]
        [MemberData(nameof(ArrayBuffer_Test_Data))]
        public static void DataViewByteLength(ArrayBuffer buffer)
        {
            var x = new DataView(buffer, 4, 2);
            Assert.Equal(2, x.ByteLength);
        }

        [Theory]
        [MemberData(nameof(ArrayBuffer_Test_Data))]
        public static void DataViewByteOffset(ArrayBuffer buffer)
        {
            var x = new DataView(buffer, 4, 2);
            Assert.Equal(4, x.ByteOffset);
        }

        public static IEnumerable<object[]> DataView_Test_Data()
        {
            yield return new object[] { new DataView(new ArrayBuffer(12), 0) };
        }

        [Theory]
        [MemberData(nameof(DataView_Test_Data))]

        public static void DataViewGetFloat32(DataView view)
        {
            view.SetFloat32(1, (float)Math.PI);
            Assert.Equal((float)Math.Round(Math.PI, 5), (float)Math.Round(view.GetFloat32(1), 5));
        }

        [Theory]
        [MemberData(nameof(DataView_Test_Data))]
        public static void DataViewGetFloat64(DataView view)
        {
            view.SetFloat64(1, (float)Math.PI);
            Assert.Equal(Math.Round(Math.PI, 5), Math.Round(view.GetFloat64(1), 5));
        }

        [Theory]
        [MemberData(nameof(DataView_Test_Data))]
        public static void DataViewGetInt16(DataView view)
        {
            view.SetInt16(1, 1234);
            Assert.Equal(1234, view.GetInt16(1));

            view.SetInt16(1, -1234);
            Assert.Equal(-1234, view.GetInt16(1));
        }

        [Theory]
        [MemberData(nameof(DataView_Test_Data))]
        public static void DataViewGetInt32(DataView view)
        {
            view.SetInt32(1, 1234);
            Assert.Equal(1234, view.GetInt32(1));

            view.SetInt32(1, -1234);
            Assert.Equal(-1234, view.GetInt32(1));
        }

        [Theory]
        [MemberData(nameof(DataView_Test_Data))]
        public static void DataViewGetInt8(DataView view)
        {
            view.SetInt8(1, 123);
            Assert.Equal(123, view.GetInt8(1));

            view.SetInt8(1, -123);
            Assert.Equal(-123, view.GetInt8(1));
        }

        [Theory]
        [MemberData(nameof(DataView_Test_Data))]
        public static void DataViewGetUint16(DataView view)
        {
            view.SetUint16(1, 1234);
            Assert.Equal(1234, view.GetUint16(1));

        }

        [Theory]
        [MemberData(nameof(DataView_Test_Data))]
        public static void DataViewGetUint32(DataView view)
        {
            view.SetUint32(1, 1234);
            Assert.Equal(1234u, view.GetUint32(1));
        }

        [Theory]
        [MemberData(nameof(DataView_Test_Data))]
        public static void DataViewGetUint8(DataView view)
        {
            view.SetUint8(1, 123);
            Assert.Equal(123u, view.GetUint8(1));
        }
    }
}
