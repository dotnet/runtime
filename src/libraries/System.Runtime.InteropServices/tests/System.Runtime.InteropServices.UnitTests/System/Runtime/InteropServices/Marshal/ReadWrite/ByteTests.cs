// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Emit;
using Xunit;

#pragma warning disable CS0618 // Type or member is obsolete

namespace System.Runtime.InteropServices.Tests
{
    public class ByteTests
    {
        [Theory]
        [InlineData(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, byte.MaxValue })]
        public void WriteByte_Pointer_Roundtrips(byte[] values)
        {
            int sizeOfArray = Marshal.SizeOf(values[0]) * values.Length;

            IntPtr ptr = Marshal.AllocCoTaskMem(sizeOfArray);
            try
            {
                Marshal.WriteByte(ptr, values[0]);

                for (int i = 1; i < values.Length; i++)
                {
                    Marshal.WriteByte(ptr, i * Marshal.SizeOf(values[0]), values[i]);
                }

                byte value = Marshal.ReadByte(ptr);
                Assert.Equal(values[0], value);

                for (int i = 1; i < values.Length; i++)
                {
                    value = Marshal.ReadByte(ptr, i * Marshal.SizeOf(values[0]));
                    Assert.Equal(values[i], value);
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(ptr);
            }
        }

        [Fact]
        [SkipOnMono("Marshal.WriteByte will not be implemented in Mono, see https://github.com/mono/mono/issues/15085.")]
        public void WriteByte_BlittableObject_Roundtrips()
        {
            int offset1 = Marshal.OffsetOf<BlittableStruct>(nameof(BlittableStruct.value1)).ToInt32();
            int offset2 = Marshal.OffsetOf<BlittableStruct>(nameof(BlittableStruct.value2)).ToInt32();

            object structure = new BlittableStruct
            {
                value1 = 10,
                value2 = 20
            };

            Marshal.WriteByte(structure, offset1, 11);
            Marshal.WriteByte(structure, offset2, 21);

            Assert.Equal(11, ((BlittableStruct)structure).value1);
            Assert.Equal(21, ((BlittableStruct)structure).value2);
            Assert.Equal(11, Marshal.ReadByte(structure, offset1));
            Assert.Equal(21, Marshal.ReadByte(structure, offset2));
        }

        [Fact]
        [SkipOnMono("Marshal.WriteByte will not be implemented in Mono, see https://github.com/mono/mono/issues/15085.")]
        public void WriteByte_StructWithReferenceTypes_ReturnsExpected()
        {
            int pointerOffset = Marshal.OffsetOf<StructWithReferenceTypes>(nameof(StructWithReferenceTypes.pointerValue)).ToInt32();
            int stringOffset = Marshal.OffsetOf<StructWithReferenceTypes>(nameof(StructWithReferenceTypes.stringValue)).ToInt32();
            int arrayOffset = Marshal.OffsetOf<StructWithReferenceTypes>(nameof(StructWithReferenceTypes.byValueArray)).ToInt32();

            object structure = new StructWithReferenceTypes
            {
                pointerValue = (IntPtr)100,
                stringValue = "ABC",
                byValueArray = new byte[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
            };

            Marshal.WriteByte(structure, pointerOffset, 200);
            Marshal.WriteByte(structure, arrayOffset + sizeof(byte) * 9, 100);

            Assert.Equal((IntPtr)200, ((StructWithReferenceTypes)structure).pointerValue);
            Assert.Equal("ABC", ((StructWithReferenceTypes)structure).stringValue);
            Assert.Equal(new byte[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 100 }, ((StructWithReferenceTypes)structure).byValueArray);
            Assert.Equal(200, Marshal.ReadByte(structure, pointerOffset));
            Assert.Equal(100, Marshal.ReadByte(structure, arrayOffset + sizeof(byte) * 9));
        }

        [Fact]
        [SkipOnMono("Marshal.ReadByte will not be implemented in Mono, see https://github.com/mono/mono/issues/15085.")]
        public void ReadByte_BlittableObject_ReturnsExpected()
        {
            int offset1 = Marshal.OffsetOf<BlittableStruct>(nameof(BlittableStruct.value1)).ToInt32();
            int offset2 = Marshal.OffsetOf<BlittableStruct>(nameof(BlittableStruct.value2)).ToInt32();

            object structure = new BlittableStruct
            {
                value1 = 10,
                value2 = 20
            };

            Assert.Equal(10, Marshal.ReadByte(structure, offset1));
            Assert.Equal(20, Marshal.ReadByte(structure, offset2));
        }

        [Fact]
        [SkipOnMono("Marshal.ReadByte will not be implemented in Mono, see https://github.com/mono/mono/issues/15085.")]
        public void ReadByte_StructWithReferenceTypes_ReturnsExpected()
        {
            int pointerOffset = Marshal.OffsetOf<StructWithReferenceTypes>(nameof(StructWithReferenceTypes.pointerValue)).ToInt32();
            int stringOffset = Marshal.OffsetOf<StructWithReferenceTypes>(nameof(StructWithReferenceTypes.stringValue)).ToInt32();
            int arrayOffset = Marshal.OffsetOf<StructWithReferenceTypes>(nameof(StructWithReferenceTypes.byValueArray)).ToInt32();

            object structure = new StructWithReferenceTypes
            {
                pointerValue = (IntPtr)100,
                stringValue = "ABC",
                byValueArray = new byte[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
            };

            Assert.Equal(100, Marshal.ReadByte(structure, pointerOffset));
            // Unlike the Int16/32/64 tests that mirror this one, we aren't going to do any asserts on the value at stringOffset.
            // The value at stringOffset is a pointer, so it is entirely possible that the first byte is 0.
            // We tried keeping this test by summing 20 calls, but even that still resulted in 0 too often. As a result, we've
            // decided to not try to validate this case instead of just incrementally increasing the number of iterations
            // to avoid getting a 0 value.
            Assert.Equal(3, Marshal.ReadByte(structure, arrayOffset + sizeof(byte) * 2));
        }

        [Fact]
        [SkipOnMono("Marshal.ReadByte will not be implemented in Mono, see https://github.com/mono/mono/issues/15085.")]
        public void ReadByte_ZeroPointer_ThrowsException()
        {
            AssertExtensions.ThrowsAny<AccessViolationException, NullReferenceException>(() => Marshal.ReadByte(IntPtr.Zero));
            AssertExtensions.ThrowsAny<AccessViolationException, NullReferenceException>(() => Marshal.ReadByte(IntPtr.Zero, 2));
        }

        [Fact]
        [SkipOnMono("Marshal.ReadByte will not be implemented in Mono, see https://github.com/mono/mono/issues/15085.")]
        public void ReadByte_NullObject_ThrowsAccessViolationException()
        {
            Assert.Throws<AccessViolationException>(() => Marshal.ReadByte(null, 2));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        [SkipOnMono("Marshal.ReadByte will not be implemented in Mono, see https://github.com/mono/mono/issues/15085.")]
        public void ReadByte_NotReadable_ThrowsArgumentException()
        {
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type");
            Type collectibleType = typeBuilder.CreateType();
            object collectibleObject = Activator.CreateInstance(collectibleType);

            AssertExtensions.Throws<ArgumentException>(null, () => Marshal.ReadByte(collectibleObject, 0));
        }

        [Fact]
        [SkipOnMono("Marshal.ReadByte will not be implemented in Mono, see https://github.com/mono/mono/issues/15085.")]
        public void WriteByte_ZeroPointer_ThrowsException()
        {
            AssertExtensions.ThrowsAny<AccessViolationException, NullReferenceException>(() => Marshal.WriteByte(IntPtr.Zero, 0));
            AssertExtensions.ThrowsAny<AccessViolationException, NullReferenceException>(() => Marshal.WriteByte(IntPtr.Zero, 2, 0));
        }

        [Fact]
        [SkipOnMono("Marshal.WriteByte will not be implemented in Mono, see https://github.com/mono/mono/issues/15085.")]
        public void WriteByte_NullObject_ThrowsAccessViolationException()
        {
            Assert.Throws<AccessViolationException>(() => Marshal.WriteByte(null, 2, 0));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        [SkipOnMono("Marshal.WriteByte will not be implemented in Mono, see https://github.com/mono/mono/issues/15085.")]
        public void WriteByte_NotReadable_ThrowsArgumentException()
        {
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type");
            Type collectibleType = typeBuilder.CreateType();
            object collectibleObject = Activator.CreateInstance(collectibleType);

            AssertExtensions.Throws<ArgumentException>(null, () => Marshal.WriteByte(collectibleObject, 0, 0));
        }

        public struct BlittableStruct
        {
            public byte value1;
            public int padding;
            public byte value2;
        }

        public struct StructWithReferenceTypes
        {
            public IntPtr pointerValue;
            public string stringValue;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public byte[] byValueArray;
        }
    }
}

#pragma warning restore CS0618 // Type or member is obsolete
