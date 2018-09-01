// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

#pragma warning disable CS0618 // Type or member is obsolete

namespace System.Runtime.InteropServices.Tests
{
    public class IntPtrTests
    {
        public static IEnumerable<object[]> ReadWrite_TestData()
        {
            yield return new object[] { Enumerable.Range(0, 10).Select(i => (IntPtr)i).ToArray() };
        }

        [Theory]
        [MemberData(nameof(ReadWrite_TestData))]
        public void WriteIntPtr_Pointer_Roundtrips(IntPtr[] values)
        {
            int sizeOfArray = Marshal.SizeOf(values[0]) * values.Length;

            IntPtr ptr = Marshal.AllocCoTaskMem(sizeOfArray);
            try
            {
                Marshal.WriteIntPtr(ptr, values[0]);

                for (int i = 1; i < values.Length; i++)
                {
                    Marshal.WriteIntPtr(ptr, i * Marshal.SizeOf(values[0]), values[i]);
                }

                IntPtr value = Marshal.ReadIntPtr(ptr);
                Assert.Equal(values[0], value);

                for (int i = 1; i < values.Length; i++)
                {
                    value = Marshal.ReadIntPtr(ptr, i * Marshal.SizeOf(values[0]));
                    Assert.Equal(values[i], value);
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(ptr);
            }
        }

        [Fact]
        public void WriteIntPtr_BlittableObject_Roundtrips()
        {
            int offset1 = Marshal.OffsetOf<BlittableStruct>(nameof(BlittableStruct.value1)).ToInt32();
            int offset2 = Marshal.OffsetOf<BlittableStruct>(nameof(BlittableStruct.value2)).ToInt32();

            object structure = new BlittableStruct
            {
                value1 = (IntPtr)10,
                value2 = (IntPtr)20
            };

            Marshal.WriteIntPtr(structure, offset1, (IntPtr)11);
            Marshal.WriteIntPtr(structure, offset2, (IntPtr)21);

            Assert.Equal((IntPtr)11, ((BlittableStruct)structure).value1);
            Assert.Equal((IntPtr)21, ((BlittableStruct)structure).value2);
            Assert.Equal((IntPtr)11, Marshal.ReadIntPtr(structure, offset1));
            Assert.Equal((IntPtr)21, Marshal.ReadIntPtr(structure, offset2));
        }

        [Fact]
        public void WriteIntPtr_StructWithReferenceTypes_ReturnsExpected()
        {
            int pointerOffset = Marshal.OffsetOf<StructWithReferenceTypes>(nameof(StructWithReferenceTypes.pointerValue)).ToInt32();
            int stringOffset = Marshal.OffsetOf<StructWithReferenceTypes>(nameof(StructWithReferenceTypes.stringValue)).ToInt32();
            int arrayOffset = Marshal.OffsetOf<StructWithReferenceTypes>(nameof(StructWithReferenceTypes.byValueArray)).ToInt32();

            object structure = new StructWithReferenceTypes
            {
                pointerValue = (IntPtr)100,
                stringValue = "ABC",
                byValueArray = new IntPtr[10] { (IntPtr)1, (IntPtr)2, (IntPtr)3, (IntPtr)4, (IntPtr)5, (IntPtr)6, (IntPtr)7, (IntPtr)8, (IntPtr)9, (IntPtr)10 }
            };

            Marshal.WriteIntPtr(structure, pointerOffset, (IntPtr)200);
            Marshal.WriteIntPtr(structure, arrayOffset + IntPtr.Size * 9, (IntPtr)100);

            Assert.Equal((IntPtr)200, ((StructWithReferenceTypes)structure).pointerValue);
            Assert.Equal("ABC", ((StructWithReferenceTypes)structure).stringValue);
            Assert.Equal(new IntPtr[10] { (IntPtr)1, (IntPtr)2, (IntPtr)3, (IntPtr)4, (IntPtr)5, (IntPtr)6, (IntPtr)7, (IntPtr)8, (IntPtr)9, (IntPtr)100 }, ((StructWithReferenceTypes)structure).byValueArray);
            Assert.Equal((IntPtr)200, Marshal.ReadIntPtr(structure, pointerOffset));
            Assert.NotEqual(IntPtr.Zero, Marshal.ReadIntPtr(structure, stringOffset));
            Assert.Equal((IntPtr)100, Marshal.ReadIntPtr(structure, arrayOffset + IntPtr.Size * 9));
        }

        [Fact]
        public void ReadIntPtr_BlittableObject_ReturnsExpected()
        {
            int offset1 = Marshal.OffsetOf<BlittableStruct>(nameof(BlittableStruct.value1)).ToInt32();
            int offset2 = Marshal.OffsetOf<BlittableStruct>(nameof(BlittableStruct.value2)).ToInt32();

            object structure = new BlittableStruct
            {
                value1 = (IntPtr)10,
                value2 = (IntPtr)20
            };

            Assert.Equal((IntPtr)10, Marshal.ReadIntPtr(structure, offset1));
            Assert.Equal((IntPtr)20, Marshal.ReadIntPtr(structure, offset2));
        }

        [Fact]
        public void ReadIntPtr_StructWithReferenceTypes_ReturnsExpected()
        {
            int pointerOffset = Marshal.OffsetOf<StructWithReferenceTypes>(nameof(StructWithReferenceTypes.pointerValue)).ToInt32();
            int stringOffset = Marshal.OffsetOf<StructWithReferenceTypes>(nameof(StructWithReferenceTypes.stringValue)).ToInt32();
            int arrayOffset = Marshal.OffsetOf<StructWithReferenceTypes>(nameof(StructWithReferenceTypes.byValueArray)).ToInt32();

            object structure = new StructWithReferenceTypes
            {
                pointerValue = (IntPtr)100,
                stringValue = "ABC",
                byValueArray = new IntPtr[10] { (IntPtr)1, (IntPtr)2, (IntPtr)3, (IntPtr)4, (IntPtr)5, (IntPtr)6, (IntPtr)7, (IntPtr)8, (IntPtr)9, (IntPtr)10 }
            };

            Assert.Equal((IntPtr)100, Marshal.ReadIntPtr(structure, pointerOffset));
            Assert.NotEqual(IntPtr.Zero, Marshal.ReadIntPtr(structure, stringOffset));
            Assert.Equal((IntPtr)3, Marshal.ReadIntPtr(structure, arrayOffset + IntPtr.Size * 2));
        }

        [Fact]
        public void ReadIntPtr_ZeroPointer_ThrowsException()
        {
            AssertExtensions.ThrowsAny<AccessViolationException, NullReferenceException>(() => Marshal.ReadIntPtr(IntPtr.Zero));
            AssertExtensions.ThrowsAny<AccessViolationException, NullReferenceException>(() => Marshal.ReadIntPtr(IntPtr.Zero, 2));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Exception is wrapped in a TargetInvocationException in the .NET Framework.")]
        public void ReadIntPtr_NullObject_ThrowsAccessViolationException()
        {
            Assert.Throws<AccessViolationException>(() => Marshal.ReadIntPtr(null, 2));
        }

#if !netstandard // TODO: Enable for netstandard2.1
        [Fact]
        public void ReadIntPtr_NotReadable_ThrowsArgumentException()
        {
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type");
            Type collectibleType = typeBuilder.CreateType();
            object collectibleObject = Activator.CreateInstance(collectibleType);

            AssertExtensions.Throws<ArgumentException>(null, () => Marshal.ReadIntPtr(collectibleObject, 0));
        }
#endif

        [Fact]
        public void WriteIntPtr_ZeroPointer_ThrowsException()
        {
            AssertExtensions.ThrowsAny<AccessViolationException, NullReferenceException>(() => Marshal.WriteIntPtr(IntPtr.Zero, (IntPtr)0));
            AssertExtensions.ThrowsAny<AccessViolationException, NullReferenceException>(() => Marshal.WriteIntPtr(IntPtr.Zero, 2, (IntPtr)0));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Exception is wrapped in a TargetInvocationException in the .NET Framework.")]
        public void WriteIntPtr_NullObject_ThrowsAccessViolationException()
        {
            Assert.Throws<AccessViolationException>(() => Marshal.WriteIntPtr(null, 2, (IntPtr)0));
        }

#if !netstandard // TODO: Enable for netstandard2.1
        [Fact]
        public void WriteIntPtr_NotReadable_ThrowsArgumentException()
        {
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type");
            Type collectibleType = typeBuilder.CreateType();
            object collectibleObject = Activator.CreateInstance(collectibleType);

            AssertExtensions.Throws<ArgumentException>(null, () => Marshal.WriteIntPtr(collectibleObject, 0, IntPtr.Zero));
        }
#endif

        public struct BlittableStruct
        {
            public IntPtr value1;
            public int padding;
            public IntPtr value2;
        }

        public struct StructWithReferenceTypes
        {
            public IntPtr pointerValue;
            public string stringValue;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public IntPtr[] byValueArray;
        }
    }
}

#pragma warning restore CS0618 // Type or member is obsolete
