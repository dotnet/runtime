// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class StructureToPtrTests
    {
        [Fact]
        public void StructureToPtr_ByValBoolArray_Success()
        {
            var structure1 = new StructWithBoolArray()
            {
                array = new bool[] { true, true, true, true }
            };

            int size = Marshal.SizeOf(structure1);
            IntPtr memory = Marshal.AllocHGlobal(size + sizeof(int));
            try
            {
                Marshal.WriteInt32(memory, size, 0xFF);
                Marshal.StructureToPtr(structure1, memory, false);
                Marshal.StructureToPtr(structure1, memory, true);
                Assert.Equal(0xFF, Marshal.ReadInt32(memory, size));
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }
        }

        [Fact]
        public void StructureToPtr_ByValArrayInStruct_Success()
        {
            var structure = new StructWithByValArray()
            {
                array = new StructWithIntField[]
                {
                    new StructWithIntField { value = 1 },
                    new StructWithIntField { value = 2 },
                    new StructWithIntField { value = 3 },
                    new StructWithIntField { value = 4 },
                    new StructWithIntField { value = 5 }
                }
            };
            int size = Marshal.SizeOf(structure);
            IntPtr memory = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(structure, memory, false);
                Marshal.StructureToPtr(structure, memory, true);
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }
        }

        [Fact]
        public void StructureToPtr_OverflowByValArrayInStruct_Success()
        {
            var structure = new StructWithByValArray()
            {
                array = new StructWithIntField[]
                {
                    new StructWithIntField { value = 1 },
                    new StructWithIntField { value = 2 },
                    new StructWithIntField { value = 3 },
                    new StructWithIntField { value = 4 },
                    new StructWithIntField { value = 5 },
                    new StructWithIntField { value = 6 }
                }
            };

            int size = Marshal.SizeOf(structure);
            IntPtr memory = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(structure, memory, false);
                Marshal.StructureToPtr(structure, memory, true);
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }
        }

        [Fact]
        public void StructureToPtr_ByValDateArray_Success()
        {
            var structure = new StructWithDateArray()
            {
                array = new DateTime[]
                {
                    DateTime.Now, DateTime.Now , DateTime.Now, DateTime.Now, DateTime.Now, DateTime.Now , DateTime.Now, DateTime.Now
                }
            };

            int size = Marshal.SizeOf(structure);
            IntPtr memory = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(structure, memory, false);
                Marshal.StructureToPtr(structure, memory, true);
            }
            finally
            {
                Marshal.DestroyStructure(memory, structure.GetType());
                Marshal.FreeHGlobal(memory);
            }
        }

        [Fact]
        public void StructureToPtr_NullPtr_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("ptr", () => Marshal.StructureToPtr((object)new SomeTestStruct_Auto(), IntPtr.Zero, fDeleteOld: true));
            AssertExtensions.Throws<ArgumentNullException>("ptr", () => Marshal.StructureToPtr(new SomeTestStruct_Auto(), IntPtr.Zero, fDeleteOld: true));
        }

        [Fact]
        public void StructureToPtr_NullStructure_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("structure", () => Marshal.StructureToPtr(null, (IntPtr)1, fDeleteOld: true));
            AssertExtensions.Throws<ArgumentNullException>("structure", () => Marshal.StructureToPtr<object>(null, (IntPtr)1, fDeleteOld: true));
        }

        public static IEnumerable<object[]> StructureToPtr_GenericClass_TestData()
        {
            yield return new object[] { new GenericClass<string>() };
            yield return new object[] { new GenericStruct<string>() };
        }

        [Theory]
        [MemberData(nameof(StructureToPtr_GenericClass_TestData))]
        public void StructureToPtr_GenericObject_ThrowsArgumentException(object o)
        {
            AssertExtensions.Throws<ArgumentException>("structure", () => Marshal.StructureToPtr(o, (IntPtr)1, fDeleteOld: true));
            AssertExtensions.Throws<ArgumentException>("structure", () => Marshal.StructureToPtr<object>(o, (IntPtr)1, fDeleteOld: true));
        }

        public static IEnumerable<object[]> StructureToPtr_NonBlittableObject_TestData()
        {
            yield return new object[] { new NonGenericClass() };
            yield return new object[] { "string" };
        }

        [Theory]
        [MemberData(nameof(StructureToPtr_NonBlittableObject_TestData))]
        public void StructureToPtr_NonBlittable_ThrowsArgumentException(object o)
        {
            AssertExtensions.Throws<ArgumentException>("structure", () => Marshal.StructureToPtr(o, (IntPtr)1, fDeleteOld: true));
            AssertExtensions.Throws<ArgumentException>("structure", () => Marshal.StructureToPtr<object>(o, (IntPtr)1, fDeleteOld: true));
        }

        [Fact]
        public void StructureToPtr_AutoLayout_ThrowsArgumentException()
        {
            var someTs_Auto = new SomeTestStruct_Auto();
            AssertExtensions.Throws<ArgumentException>("structure", () => Marshal.StructureToPtr((object)someTs_Auto, (IntPtr)1, fDeleteOld: true));
            AssertExtensions.Throws<ArgumentException>("structure", () => Marshal.StructureToPtr(someTs_Auto, (IntPtr)1, fDeleteOld: true));
        }

        [Fact]
        public void StructureToPtr_InvalidLengthByValArrayInStruct_ThrowsArgumentException()
        {
            var structure = new StructWithByValArray
            {
                array = new StructWithIntField[]
                {
                    new StructWithIntField { value = 1 },
                    new StructWithIntField { value = 2 },
                    new StructWithIntField { value = 3 },
                    new StructWithIntField { value = 4 }
                }
            };
            int size = Marshal.SizeOf(structure);
            IntPtr memory = Marshal.AllocHGlobal(size);
            try
            {
                Assert.Throws<ArgumentException>(() => Marshal.StructureToPtr(structure, memory, false));
                Assert.Throws<ArgumentException>(() => Marshal.StructureToPtr(structure, memory, true));
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }
        }

        [Fact]
        public unsafe void StructureToPtr_StructWithBlittableFixedBuffer_In_NonBlittable_Success()
        {
            var str = default(NonBlittableContainingBuffer);

            // Assign values to the bytes.
            byte* ptr = (byte*)&str.bufferStruct;
            for (int i = 0; i < sizeof(HasFixedBuffer); i++)
                ptr[i] = (byte)(0x11 * (i + 1));

            HasFixedBuffer* original = (HasFixedBuffer*)ptr;
            
            // Marshal the parent struct.
            var parentStructIntPtr = Marshal.AllocHGlobal(Marshal.SizeOf<NonBlittableContainingBuffer>());
            Marshal.StructureToPtr(str, parentStructIntPtr, false);
            try
            {
                HasFixedBuffer* bufferStructPtr = (HasFixedBuffer*)parentStructIntPtr.ToPointer();
                Assert.Equal(original->buffer[0], bufferStructPtr->buffer[0]);
                Assert.Equal(original->buffer[1], bufferStructPtr->buffer[1]);
            }
            finally
            {
                Marshal.DestroyStructure<NonBlittableContainingBuffer>(parentStructIntPtr);
                Marshal.FreeHGlobal(parentStructIntPtr);
            }
        }

        [Fact]
        public unsafe void StructureToPtr_NonBlittableStruct_WithBlittableFixedBuffer_Success()
        {
            NonBlittableWithBlittableBuffer x = new NonBlittableWithBlittableBuffer();
            x.f[0] = 1;
            x.f[1] = 2;
            x.f[2] = 3;
            x.s = null;

            int size = Marshal.SizeOf(typeof(NonBlittableWithBlittableBuffer));
            byte* p = stackalloc byte[size];
            Marshal.StructureToPtr(x, (IntPtr)p, false);
            NonBlittableWithBlittableBuffer y = Marshal.PtrToStructure<NonBlittableWithBlittableBuffer>((IntPtr)p);

            Assert.Equal(x.f[0], y.f[0]);
            Assert.Equal(x.f[1], y.f[1]);
            Assert.Equal(x.f[2], y.f[2]);
        }

        [Fact]
        public unsafe void StructureToPtr_OpaqueStruct_In_NonBlittableStructure_Success()
        {
            NonBlittableWithOpaque x = new NonBlittableWithOpaque();
            byte* opaqueData = (byte*)&x.opaque;
            *opaqueData = 1;

            int size = Marshal.SizeOf(typeof(NonBlittableWithOpaque));
            byte* p = stackalloc byte[size];
            Marshal.StructureToPtr(x, (IntPtr)p, false);
            NonBlittableWithOpaque y = Marshal.PtrToStructure<NonBlittableWithOpaque>((IntPtr)p);

            byte* marshaledOpaqueData = (byte*)&y.opaque;

            Assert.Equal(*opaqueData, *marshaledOpaqueData);
        }

        public struct StructWithIntField
        {
            public int value;
        }

        public struct StructWithByValArray
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public StructWithIntField[] array;
        }

        public struct StructWithBoolArray
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public bool[] array;
        }

        public struct StructWithDateArray
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public DateTime[] array;
        }

        [StructLayout(LayoutKind.Auto)]
        public struct SomeTestStruct_Auto
        {
            public int i;
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct HasFixedBuffer
        {
            public short member;
            public fixed byte buffer[2];
            public short member2;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct NonBlittableContainingBuffer
        {
            public HasFixedBuffer bufferStruct;
            public string str;
            public IntPtr intPtr;
        }

        unsafe struct NonBlittableWithBlittableBuffer
        {
            public fixed int f[100];
            public string s;
        }
        
        [StructLayout(LayoutKind.Explicit, Size = 1)]
        public struct OpaqueStruct
        {

        }

        public struct NonBlittableWithOpaque
        {
            public OpaqueStruct opaque;
            public string str;
        }
    }
}
