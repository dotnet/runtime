// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class SizeOfTests
    {
        public struct TestStructWithEnumArray
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public TestEnum[] ArrayOfEnum;
        }

        public enum TestEnum
        {
            red,
            green,
            blue
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SomeTestStruct
        {
            public int i;
            public String s;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TestStructWithFxdLPSTRSAFld
        {
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.LPStr, SizeConst = 0)]
            public String[] Arr;
        }

        [Fact]
        public void SizeOfStructWithEnumArray()
        {
            TestStructWithEnumArray s = new TestStructWithEnumArray();
            s.ArrayOfEnum = new TestEnum[3];
            s.ArrayOfEnum[0] = TestEnum.red;
            s.ArrayOfEnum[1] = TestEnum.green;
            s.ArrayOfEnum[2] = TestEnum.blue;
            int retsize = Marshal.SizeOf(s.GetType());
            Assert.Equal(12, retsize);

            retsize = 0;
            retsize = Marshal.SizeOf(typeof(TestStructWithEnumArray));
            int genericRetsize = Marshal.SizeOf<TestStructWithEnumArray>();
            Assert.Equal(12, retsize);
            Assert.Equal(retsize, genericRetsize);
        }

        [Fact]
        public void SizeOfStructTest()
        {
            SomeTestStruct someTestStruct = new SomeTestStruct();
            AssertExtensions.Throws<ArgumentNullException>("t", () => Marshal.SizeOf(null));
            Assert.Throws<ArgumentException>(() => Marshal.SizeOf(typeof(TestStructWithFxdLPSTRSAFld)));
            Marshal.SizeOf(someTestStruct.GetType());
        }
    }
}
