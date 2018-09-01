// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Reflection.Emit;
using Xunit;

#pragma warning disable CS0618 // Type or member is obsolete

namespace System.Runtime.InteropServices.Tests
{
    public class Int64Tests
    {
        [Theory]
        [InlineData(new long[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, long.MaxValue })]
        public void WriteInt64_Pointer_Roundtrips(long[] values)
        {
            int sizeOfArray = Marshal.SizeOf(values[0]) * values.Length;

            IntPtr ptr = Marshal.AllocCoTaskMem(sizeOfArray);
            try
            {
                Marshal.WriteInt64(ptr, values[0]);

                for (int i = 1; i < values.Length; i++)
                {
                    Marshal.WriteInt64(ptr, i * Marshal.SizeOf(values[0]), values[i]);
                }

                long value = Marshal.ReadInt64(ptr);
                Assert.Equal(values[0], value);

                for (int i = 1; i < values.Length; i++)
                {
                    value = Marshal.ReadInt64(ptr, i * Marshal.SizeOf(values[0]));
                    Assert.Equal(values[i], value);
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(ptr);
            }
        }

        [Fact]
        public void WriteInt64_BlittableObject_Roundtrips()
        {
            int offset1 = Marshal.OffsetOf<BlittableStruct>(nameof(BlittableStruct.value1)).ToInt32();
            int offset2 = Marshal.OffsetOf<BlittableStruct>(nameof(BlittableStruct.value2)).ToInt32();

            object structure = new BlittableStruct
            {
                value1 = 10,
                value2 = 20
            };

            Marshal.WriteInt64(structure, offset1, 11);
            Marshal.WriteInt64(structure, offset2, 21);

            Assert.Equal(11, ((BlittableStruct)structure).value1);
            Assert.Equal(21, ((BlittableStruct)structure).value2);
            Assert.Equal(11, Marshal.ReadInt64(structure, offset1));
            Assert.Equal(21, Marshal.ReadInt64(structure, offset2));
        }

        [Fact]
        public void WriteInt64_StructWithReferenceTypes_ReturnsExpected()
        {
            int pointerOffset = Marshal.OffsetOf<StructWithReferenceTypes>(nameof(StructWithReferenceTypes.pointerValue)).ToInt32();
            int stringOffset = Marshal.OffsetOf<StructWithReferenceTypes>(nameof(StructWithReferenceTypes.stringValue)).ToInt32();
            int arrayOffset = Marshal.OffsetOf<StructWithReferenceTypes>(nameof(StructWithReferenceTypes.byValueArray)).ToInt32();

            object structure = new StructWithReferenceTypes
            {
                pointerValue = (IntPtr)100,
                stringValue = "ABC",
                byValueArray = new long[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
            };

            if (IntPtr.Size == 8)
            {
                Marshal.WriteInt64(structure, pointerOffset, 200);
            }
            Marshal.WriteInt64(structure, arrayOffset + sizeof(long) * 9, 100);

            if (IntPtr.Size == 8)
            {
                Assert.Equal((IntPtr)200, ((StructWithReferenceTypes)structure).pointerValue);
            }
            Assert.Equal("ABC", ((StructWithReferenceTypes)structure).stringValue);
            Assert.Equal(new long[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 100 }, ((StructWithReferenceTypes)structure).byValueArray);
            if (IntPtr.Size == 8)
            {
                Assert.Equal(200, Marshal.ReadInt64(structure, pointerOffset));
            }
            Assert.NotEqual(0, Marshal.ReadInt64(structure, stringOffset));
            Assert.Equal(100, Marshal.ReadInt64(structure, arrayOffset + sizeof(long) * 9));
        }

        [Fact]
        public void ReadInt64_BlittableObject_ReturnsExpected()
        {
            int offset1 = Marshal.OffsetOf<BlittableStruct>(nameof(BlittableStruct.value1)).ToInt32();
            int offset2 = Marshal.OffsetOf<BlittableStruct>(nameof(BlittableStruct.value2)).ToInt32();

            object structure = new BlittableStruct
            {
                value1 = 10,
                value2 = 20
            };

            Assert.Equal(10, Marshal.ReadInt64(structure, offset1));
            Assert.Equal(20, Marshal.ReadInt64(structure, offset2));
        }

        [Fact]
        public void ReadInt64_StructWithReferenceTypes_ReturnsExpected()
        {
            int pointerOffset = Marshal.OffsetOf<StructWithReferenceTypes>(nameof(StructWithReferenceTypes.pointerValue)).ToInt32();
            int stringOffset = Marshal.OffsetOf<StructWithReferenceTypes>(nameof(StructWithReferenceTypes.stringValue)).ToInt32();
            int arrayOffset = Marshal.OffsetOf<StructWithReferenceTypes>(nameof(StructWithReferenceTypes.byValueArray)).ToInt32();

            object structure = new StructWithReferenceTypes
            {
                pointerValue = (IntPtr)100,
                stringValue = "ABC",
                byValueArray = new long[10] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
            };

            if (IntPtr.Size == 8)
            {
                Assert.Equal(100, Marshal.ReadInt64(structure, pointerOffset));
            }
            Assert.NotEqual(0, Marshal.ReadInt64(structure, stringOffset));
            Assert.Equal(3, Marshal.ReadInt64(structure, arrayOffset + sizeof(long) * 2));
        }

        [Fact]
        public void ReadInt64_ZeroPointer_ThrowsException()
        {
            AssertExtensions.ThrowsAny<AccessViolationException, NullReferenceException>(() => Marshal.ReadInt64(IntPtr.Zero));
            AssertExtensions.ThrowsAny<AccessViolationException, NullReferenceException>(() => Marshal.ReadInt64(IntPtr.Zero, 2));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Exception is wrapped in a TargetInvocationException in the .NET Framework.")]
        public void ReadInt64_NullObject_ThrowsAccessViolationException()
        {
            Assert.Throws<AccessViolationException>(() => Marshal.ReadInt64(null, 2));
        }

#if !netstandard // TODO: Enable for netstandard2.1
        [Fact]
        public void ReadInt64_NotReadable_ThrowsArgumentException()
        {
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type");
            Type collectibleType = typeBuilder.CreateType();
            object collectibleObject = Activator.CreateInstance(collectibleType);

            AssertExtensions.Throws<ArgumentException>(null, () => Marshal.ReadInt64(collectibleObject, 0));
        }
#endif

        [Fact]
        public void WriteInt64_ZeroPointer_ThrowsException()
        {
            AssertExtensions.ThrowsAny<AccessViolationException, NullReferenceException>(() => Marshal.WriteInt64(IntPtr.Zero, 0));
            AssertExtensions.ThrowsAny<AccessViolationException, NullReferenceException>(() => Marshal.WriteInt64(IntPtr.Zero, 2, 0));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Exception is wrapped in a TargetInvocationException in the .NET Framework.")]
        public void WriteInt64_NullObject_ThrowsAccessViolationException()
        {
            Assert.Throws<AccessViolationException>(() => Marshal.WriteInt64(null, 2, 0));
        }

#if !netstandard // TODO: Enable for netstandard2.1
        [Fact]
        public void WriteInt64_NotReadable_ThrowsArgumentException()
        {
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type");
            Type collectibleType = typeBuilder.CreateType();
            object collectibleObject = Activator.CreateInstance(collectibleType);

            AssertExtensions.Throws<ArgumentException>(null, () => Marshal.WriteInt64(collectibleObject, 0, 0));
        }
#endif

        public struct BlittableStruct
        {
            public long value1;
            public int padding;
            public long value2;
        }

        public struct StructWithReferenceTypes
        {
            public IntPtr pointerValue;
            public string stringValue;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public long[] byValueArray;
        }
    }
}

#pragma warning restore CS0618 // Type or member is obsolete
