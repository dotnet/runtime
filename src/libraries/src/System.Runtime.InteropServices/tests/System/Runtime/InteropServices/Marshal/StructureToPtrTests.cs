// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class StructureToPtrTests
    {
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

        [Fact]
        public void AutoLayoutStructureTest()
        {
            var someTs_Auto = new SomeTestStruct_Auto();
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>("structure", () => Marshal.StructureToPtr(someTs_Auto, new IntPtr(123), true));
        }

        [Fact]
        public void NullParameter()
        {
            IntPtr ip = IntPtr.Zero;
            AssertExtensions.Throws<ArgumentNullException>("ptr", () => Marshal.StructureToPtr<SomeTestStruct_Auto>(new SomeTestStruct_Auto(), ip, true));

            ip = new IntPtr(123);
            AssertExtensions.Throws<ArgumentNullException>("structure", () => Marshal.StructureToPtr<Object>(null, ip, true));
        }

        [Fact]
        public void VerifyByValBoolArray()
        {
            var structure1 = new StructWithBoolArray()
            {
                array = new bool[]
                {
                    true,true,true,true
                }
            };

            int size = Marshal.SizeOf(structure1);
            IntPtr memory = Marshal.AllocHGlobal(size + sizeof(Int32));

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
        public void VerifyByValArrayInStruct()
        {
            // equal
            var structure1 = new StructWithByValArray()
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
            int size = Marshal.SizeOf(structure1);
            IntPtr memory = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(structure1, memory, false);
                Marshal.StructureToPtr(structure1, memory, true);
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }

            // underflow
            var structure2 = new StructWithByValArray()
            {
                array = new StructWithIntField[]
                {
                    new StructWithIntField { value = 1 },
                    new StructWithIntField { value = 2 },
                    new StructWithIntField { value = 3 },
                    new StructWithIntField { value = 4 }
                }
            };
            size = Marshal.SizeOf(structure2);
            memory = Marshal.AllocHGlobal(size);
            try
            {
                Assert.Throws<ArgumentException>(() => Marshal.StructureToPtr(structure2, memory, false));
                Assert.Throws<ArgumentException>(() => Marshal.StructureToPtr(structure2, memory, true));
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }

            // overflow
            var structure3 = new StructWithByValArray()
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

            size = Marshal.SizeOf(structure3);
            memory = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(structure3, memory, false);
                Marshal.StructureToPtr(structure3, memory, true);
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }
        }

        [Fact]
        public void VerfiyByValDateArray()
        {
            var structure1 = new StructWithDateArray()
            {
                array = new DateTime[]
                {
                    DateTime.Now, DateTime.Now , DateTime.Now, DateTime.Now, DateTime.Now, DateTime.Now , DateTime.Now, DateTime.Now
                }
            };

            int size = Marshal.SizeOf(structure1);
            IntPtr memory = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(structure1, memory, false);
                Marshal.StructureToPtr(structure1, memory, true);
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }
        }
    }
}
