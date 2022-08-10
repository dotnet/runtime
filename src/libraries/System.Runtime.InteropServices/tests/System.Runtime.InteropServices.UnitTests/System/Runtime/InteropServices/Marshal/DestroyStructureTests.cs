// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public void DestroyStructure_NonGeneric_Success()
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
            yield return new object[] { typeof(IGenericInterface<>) };
            yield return new object[] { typeof(IGenericInterface<string>) };

            yield return new object[] { typeof(GenericClass<>).GetTypeInfo().GenericTypeParameters[0] };

            if (PlatformDetection.IsReflectionEmitSupported)
            {
                AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.Run);
                ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
                TypeBuilder typeBuilder = moduleBuilder.DefineType("Type");
                yield return new object[] { typeBuilder };
            }
        }

        [Theory]
        [ActiveIssue("https://github.com/mono/mono/issues/15087", TestRuntimes.Mono)]
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

        [Fact]
        public void DestroyStructure_NestedNonBlittableStruct_Success()
        {
            WINTRUST_BLOB_INFO wbi = new WINTRUST_BLOB_INFO();
            byte[] contentBytes = System.Text.Encoding.Unicode.GetBytes("foo");

            wbi.gSubject.Data1 = 0x603bcc1f;
            wbi.gSubject.Data2 = 0x4b59;
            wbi.gSubject.Data3 = 0x4e08;
            wbi.gSubject.Data4 = new byte[] { 0xb7, 0x24, 0xd2, 0xc6, 0x29, 0x7e, 0xf3, 0x51 };

            wbi.cbStruct = (uint)Marshal.SizeOf(wbi);
            wbi.pcwszDisplayName = "bar";

            IntPtr pBlob = Marshal.AllocCoTaskMem(Marshal.SizeOf(wbi));
            Marshal.StructureToPtr(wbi, pBlob, false);

            Marshal.DestroyStructure<WINTRUST_BLOB_INFO>(pBlob);

            Marshal.FreeCoTaskMem(pBlob);
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

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        internal struct GUID
        {
            internal uint Data1;
            internal ushort Data2;
            internal ushort Data3;

            /// unsigned char[8]
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            internal byte[] Data4;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct WINTRUST_BLOB_INFO
        {
            internal uint cbStruct;

            /// GUID->_GUID
            internal GUID gSubject;
            //[MarshalAs(UnmanagedType.Struct)]
            //internal Guid gSubject;

            [MarshalAsAttribute(UnmanagedType.LPWStr)]
            internal string pcwszDisplayName;
        }
    }
}
