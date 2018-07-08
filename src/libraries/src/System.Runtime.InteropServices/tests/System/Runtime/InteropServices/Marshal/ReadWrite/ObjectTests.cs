// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    internal struct BlittableStruct
    {
        internal int _a;
        internal int _b;
        internal byte _c;
        internal short _d;
        internal IntPtr _p;
    }

    internal struct StructWithReferenceTypes
    {
        internal IntPtr _ptr;
        internal string _str;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        internal int[] _byValArr;
    }
#pragma warning disable 618
    public class ObjectTests
    {
        [Fact]
        [ActiveIssue(30830, TargetFrameworkMonikers.NetFramework)]
        public void NullValueArguments_ThrowsArgumentNullException()
        {
            Assert.Throws<AccessViolationException>(() => { Marshal.WriteByte(null, 0, 0); });
            Assert.Throws<AccessViolationException>(() => { Marshal.WriteInt16(null, 0, 0); });
            Assert.Throws<AccessViolationException>(() => { Marshal.WriteInt32(null, 0, 0); });
            Assert.Throws<AccessViolationException>(() => { Marshal.WriteInt64(null, 0, 0); });
            Assert.Throws<AccessViolationException>(() => { Marshal.WriteIntPtr(null, 0, IntPtr.Zero); });
            Assert.Throws<AccessViolationException>(() => { Marshal.ReadByte(null, 0); });
            Assert.Throws<AccessViolationException>(() => { Marshal.ReadInt16(null, 0); });
            Assert.Throws<AccessViolationException>(() => { Marshal.ReadInt32(null, 0); });
            Assert.Throws<AccessViolationException>(() => { Marshal.ReadIntPtr(null, 0); });
        }

        [Fact]
        public void TestBlittableStruct()
        {
            BlittableStruct blittableStruct = new BlittableStruct();
            blittableStruct._a = 200;
            blittableStruct._b = 300;
            blittableStruct._c = 10;
            blittableStruct._d = 123;
            blittableStruct._p = new IntPtr(100);

            object boxedBlittableStruct = (object)blittableStruct;

            int offsetOfB = Marshal.OffsetOf<BlittableStruct>("_b").ToInt32();
            int offsetOfC = Marshal.OffsetOf<BlittableStruct>("_c").ToInt32();
            int offsetOfD = Marshal.OffsetOf<BlittableStruct>("_d").ToInt32();
            int offsetOfP = Marshal.OffsetOf<BlittableStruct>("_p").ToInt32();

            Assert.Equal(200, Marshal.ReadInt32(boxedBlittableStruct, 0));
            Assert.Equal(300, Marshal.ReadInt32(boxedBlittableStruct, offsetOfB));
            Assert.Equal(10, Marshal.ReadByte(boxedBlittableStruct, offsetOfC));
            Assert.Equal(123, Marshal.ReadInt16(boxedBlittableStruct, offsetOfD));
            Assert.Equal(new IntPtr(100), Marshal.ReadIntPtr(boxedBlittableStruct, offsetOfP));

            Marshal.WriteInt32(boxedBlittableStruct, 0, 300);
            Marshal.WriteInt32(boxedBlittableStruct, offsetOfB, 400);
            Marshal.WriteByte(boxedBlittableStruct, offsetOfC, 20);
            Marshal.WriteInt16(boxedBlittableStruct, offsetOfD, 144);

            Marshal.WriteIntPtr(boxedBlittableStruct, offsetOfP, new IntPtr(500));

            Assert.Equal(300, ((BlittableStruct)boxedBlittableStruct)._a);
            Assert.Equal(400, ((BlittableStruct)boxedBlittableStruct)._b);
            Assert.Equal(20, ((BlittableStruct)boxedBlittableStruct)._c);
            Assert.Equal(144, ((BlittableStruct)boxedBlittableStruct)._d);
            Assert.Equal(new IntPtr(500), ((BlittableStruct)boxedBlittableStruct)._p);
        }

        [Fact]
        public void TestStructWithReferenceType()
        {
            StructWithReferenceTypes structWithReferenceTypes = new StructWithReferenceTypes();
            structWithReferenceTypes._ptr = new IntPtr(100);
            structWithReferenceTypes._str = "ABC";
            structWithReferenceTypes._byValArr = new int[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            object boxedStruct = (object)structWithReferenceTypes;

            int offsetOfStr = Marshal.OffsetOf<StructWithReferenceTypes>("_str").ToInt32();
            int offsetOfByValArr = Marshal.OffsetOf<StructWithReferenceTypes>("_byValArr").ToInt32();

            Assert.Equal(100, Marshal.ReadInt32(boxedStruct, 0));
            Assert.NotEqual(IntPtr.Zero, Marshal.ReadIntPtr(boxedStruct, offsetOfStr));
            Assert.Equal(3, Marshal.ReadInt32(boxedStruct, offsetOfByValArr + sizeof(int) * 2));

            Marshal.WriteInt32(boxedStruct, 0, 200);
            Marshal.WriteInt32(boxedStruct, offsetOfByValArr + sizeof(int) * 9, 100);

            Assert.Equal(new IntPtr(200), ((StructWithReferenceTypes)boxedStruct)._ptr);
            Assert.Equal(100, ((StructWithReferenceTypes)boxedStruct)._byValArr[9]);
            Assert.Equal(9, ((StructWithReferenceTypes)boxedStruct)._byValArr[8]);
            Assert.Equal("ABC", ((StructWithReferenceTypes)boxedStruct)._str);
        }
    }
#pragma warning restore 618
}
