// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Emit;
using Xunit;

#pragma warning disable CS0618 // Type or member is obsolete

namespace System.Runtime.InteropServices.Tests
{
    public class Int16Tests
    {
        [Theory]
        [InlineData(new short[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, short.MaxValue })]
        public void WriteInt16_Pointer_Roundtrips(short[] values)
        {
            int sizeOfArray = Marshal.SizeOf(values[0]) * values.Length;

            IntPtr ptr = Marshal.AllocCoTaskMem(sizeOfArray);
            try
            {
                Marshal.WriteInt16(ptr, values[0]);

                for (int i = 1; i < values.Length; i++)
                {
                    Marshal.WriteInt16(ptr, i * Marshal.SizeOf(values[0]), values[i]);
                }

                short value = Marshal.ReadInt16(ptr);
                Assert.Equal(values[0], value);

                for (int i = 1; i < values.Length; i++)
                {
                    value = Marshal.ReadInt16(ptr, i * Marshal.SizeOf(values[0]));
                    Assert.Equal(values[i], value);
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(ptr);
            }
        }

        [Fact]
        [SkipOnMono("Marshal.WriteInt16 will not be implemented in Mono, see https://github.com/mono/mono/issues/15085.")]
        public void WriteInt16_BlittableObject_Roundtrips()
        {
            int offset1 = Marshal.OffsetOf<BlittableStruct>(nameof(BlittableStruct.value1)).ToInt32();
            int offset2 = Marshal.OffsetOf<BlittableStruct>(nameof(BlittableStruct.value2)).ToInt32();

            object structure = new BlittableStruct
            {
                value1 = 10,
                value2 = 20
            };

            Marshal.WriteInt16(structure, offset1, 11);
            Marshal.WriteInt16(structure, offset2, 21);

            Assert.Equal(11, ((BlittableStruct)structure).value1);
            Assert.Equal(21, ((BlittableStruct)structure).value2);
            Assert.Equal(11, Marshal.ReadInt16(structure, offset1));
            Assert.Equal(21, Marshal.ReadInt16(structure, offset2));
        }

        [Fact]
        [SkipOnMono("Marshal.WriteInt16 will not be implemented in Mono, see https://github.com/mono/mono/issues/15085.")]
        public void WriteInt16_StructWithReferenceTypes_ReturnsExpected()
        {
            int pointerOffset = Marshal.OffsetOf<StructWithReferenceTypes>(nameof(StructWithReferenceTypes.pointerValue)).ToInt32();
            int stringOffset = Marshal.OffsetOf<StructWithReferenceTypes>(nameof(StructWithReferenceTypes.stringValue)).ToInt32();
            int arrayOffset = Marshal.OffsetOf<StructWithReferenceTypes>(nameof(StructWithReferenceTypes.byValueArray)).ToInt32();

            object structure = new StructWithReferenceTypes
            {
                pointerValue = (IntPtr)100,
                stringValue = "ABC",
                byValueArray = new short[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
            };

            Marshal.WriteInt16(structure, pointerOffset, 200);
            Marshal.WriteInt16(structure, arrayOffset + sizeof(short) * 9, 100);

            Assert.Equal((IntPtr)200, ((StructWithReferenceTypes)structure).pointerValue);
            Assert.Equal("ABC", ((StructWithReferenceTypes)structure).stringValue);
            Assert.Equal(new short[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 100 }, ((StructWithReferenceTypes)structure).byValueArray);
            Assert.Equal(200, Marshal.ReadInt16(structure, pointerOffset));
            Assert.Equal(100, Marshal.ReadInt16(structure, arrayOffset + sizeof(short) * 9));
        }

        [Fact]
        [SkipOnMono("Marshal.ReadInt16 will not be implemented in Mono, see https://github.com/mono/mono/issues/15085.")]
        public void ReadInt16_BlittableObject_ReturnsExpected()
        {
            int offset1 = Marshal.OffsetOf<BlittableStruct>(nameof(BlittableStruct.value1)).ToInt32();
            int offset2 = Marshal.OffsetOf<BlittableStruct>(nameof(BlittableStruct.value2)).ToInt32();

            object structure = new BlittableStruct
            {
                value1 = 10,
                value2 = 20
            };

            Assert.Equal(10, Marshal.ReadInt16(structure, offset1));
            Assert.Equal(20, Marshal.ReadInt16(structure, offset2));
        }

        [Fact]
        [SkipOnMono("Marshal.ReadInt16 will not be implemented in Mono, see https://github.com/mono/mono/issues/15085.")]
        public void ReadInt16_StructWithReferenceTypes_ReturnsExpected()
        {
            int pointerOffset = Marshal.OffsetOf<StructWithReferenceTypes>(nameof(StructWithReferenceTypes.pointerValue)).ToInt32();
            int stringOffset = Marshal.OffsetOf<StructWithReferenceTypes>(nameof(StructWithReferenceTypes.stringValue)).ToInt32();
            int arrayOffset = Marshal.OffsetOf<StructWithReferenceTypes>(nameof(StructWithReferenceTypes.byValueArray)).ToInt32();

            object structure = new StructWithReferenceTypes
            {
                pointerValue = (IntPtr)100,
                stringValue = "ABC",
                byValueArray = new short[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
            };

            Assert.Equal(100, Marshal.ReadInt16(structure, pointerOffset));
            Assert.Equal(3, Marshal.ReadInt16(structure, arrayOffset + sizeof(short) * 2));
        }

        [Fact]
        [SkipOnMono("Marshal.ReadByte will not be implemented in Mono, see https://github.com/mono/mono/issues/15085.")]
        public void ReadInt16_ZeroPointer_ThrowsException()
        {
            AssertExtensions.ThrowsAny<AccessViolationException, NullReferenceException>(() => Marshal.ReadInt16(IntPtr.Zero));
            AssertExtensions.ThrowsAny<AccessViolationException, NullReferenceException>(() => Marshal.ReadInt16(IntPtr.Zero, 2));
        }

        [Fact]
        [SkipOnMono("Marshal.ReadInt16 will not be implemented in Mono, see https://github.com/mono/mono/issues/15085.")]
        public void ReadInt16_NullObject_ThrowsAccessViolationException()
        {
            Assert.Throws<AccessViolationException>(() => Marshal.ReadInt16(null, 2));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        [SkipOnMono("Marshal.ReadInt16 will not be implemented in Mono, see https://github.com/mono/mono/issues/15085.")]
        public void ReadInt16_NotReadable_ThrowsArgumentException()
        {
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type");
            Type collectibleType = typeBuilder.CreateType();
            object collectibleObject = Activator.CreateInstance(collectibleType);

            AssertExtensions.Throws<ArgumentException>(null, () => Marshal.ReadInt16(collectibleObject, 0));
        }

        [Fact]
        [SkipOnMono("Marshal.ReadByte will not be implemented in Mono, see https://github.com/mono/mono/issues/15085.")]
        public void WriteInt16_ZeroPointer_ThrowsException()
        {
            AssertExtensions.ThrowsAny<AccessViolationException, NullReferenceException>(() => Marshal.WriteInt16(IntPtr.Zero, 0));
            AssertExtensions.ThrowsAny<AccessViolationException, NullReferenceException>(() => Marshal.WriteInt16(IntPtr.Zero, 2, 0));
        }

        [Fact]
        [SkipOnMono("Marshal.WriteInt16 will not be implemented in Mono, see https://github.com/mono/mono/issues/15085.")]
        public void WriteInt16_NullObject_ThrowsAccessViolationException()
        {
            Assert.Throws<AccessViolationException>(() => Marshal.WriteInt16(null, 2, 0));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        [SkipOnMono("Marshal.WriteInt16 will not be implemented in Mono, see https://github.com/mono/mono/issues/15085.")]
        public void WriteInt16_NotReadable_ThrowsArgumentException()
        {
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type");
            Type collectibleType = typeBuilder.CreateType();
            object collectibleObject = Activator.CreateInstance(collectibleType);

            AssertExtensions.Throws<ArgumentException>(null, () => Marshal.WriteInt16(collectibleObject, 0, 0));
        }

        public struct BlittableStruct
        {
            public short value1;
            public int padding;
            public short value2;
        }

        public struct StructWithReferenceTypes
        {
            public IntPtr pointerValue;
            public string stringValue;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public short[] byValueArray;
        }
    }
}

#pragma warning restore CS0618 // Type or member is obsolete
