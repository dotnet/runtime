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
    }
}
