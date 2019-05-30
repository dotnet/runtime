// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class DestroyStructureTests
    {
        [Fact]
        public void DestroyStructure_Generic_Success()
        {
            var structure = new TestStruct();
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(structure));
            try
            {
                structure.s = null;

                Marshal.StructureToPtr(structure, ptr, fDeleteOld: false);
                Marshal.DestroyStructure<TestStruct>(ptr);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        [Fact]
        public void DestroyStructure_NonGeneric_Succes()
        {
            var structure = new TestStruct();
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(structure));
            try
            {
                structure.s = null;

                Marshal.StructureToPtr(structure, ptr, fDeleteOld: false);
                Marshal.DestroyStructure(ptr, typeof(TestStruct));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        [Fact]
        public void DestroyStructure_Blittable_Success()
        {
            Marshal.DestroyStructure<int>((IntPtr)1);
            Marshal.DestroyStructure((IntPtr)1, typeof(int));
        }

        [Fact]
        public void DestroyStructure_ZeroPointer_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("ptr", () => Marshal.DestroyStructure<TestStruct>(IntPtr.Zero));
            AssertExtensions.Throws<ArgumentNullException>("ptr", () => Marshal.DestroyStructure(IntPtr.Zero, typeof(TestStruct)));
        }

        [Fact]
        public void DestroyStructure_NullStructureType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("structureType", () => Marshal.DestroyStructure((IntPtr)1, null));
        }
        
        public static IEnumerable<object[]> DestroyStructure_InvalidType_TestData()
        {
            yield return new object[] { typeof(int).MakeByRefType() };
            yield return new object[] { typeof(string) };

            yield return new object[] { typeof(NonGenericClass) };
            yield return new object[] { typeof(GenericClass<>) };
            yield return new object[] { typeof(GenericClass<string>) };
            yield return new object[] { typeof(AbstractClass) };

            yield return new object[] { typeof(GenericStruct<>) };
            yield return new object[] { typeof(GenericStruct<string>) };
            yield return new object[] { typeof(GenericInterface<>) };
            yield return new object[] { typeof(GenericInterface<string>) };

            yield return new object[] { typeof(GenericClass<>).GetTypeInfo().GenericTypeParameters[0] };

            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type");
            yield return new object[] { typeBuilder };
        }

        [Theory]
        [MemberData(nameof(DestroyStructure_InvalidType_TestData))]
        public void DestroyStructure_NonRuntimeType_ThrowsArgumentException(Type invalidType)
        {
            AssertExtensions.Throws<ArgumentException>("structureType", () => Marshal.DestroyStructure((IntPtr)1, invalidType));
        }

        [Fact]
        public void DestroyStructure_AutoLayout_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("structureType", () => Marshal.DestroyStructure<AutoLayoutStruct>((IntPtr)1));
            AssertExtensions.Throws<ArgumentException>("structureType", () => Marshal.DestroyStructure((IntPtr)1, typeof(AutoLayoutStruct)));
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TestStruct
        {
            public int i;
            public string s;
        }

        [StructLayout(LayoutKind.Auto)]
        public struct AutoLayoutStruct
        {
            public int i;
        }
    }
}
