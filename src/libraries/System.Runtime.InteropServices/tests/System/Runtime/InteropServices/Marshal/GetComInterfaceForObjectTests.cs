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
    public partial class GetComInterfaceForObjectTests
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetComInterfaceForObject_GenericWithValidClass_ReturnsExpected()
        {
            var o = new ClassWithInterface();
            IntPtr iUnknown = Marshal.GetComInterfaceForObject<ClassWithInterface, NonGenericInterface>(o);
            try
            {
                Assert.NotEqual(IntPtr.Zero, iUnknown);
            }
            finally
            {
                Marshal.Release(iUnknown);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetComInterfaceForObject_GenericWithValidStruct_ReturnsExpected()
        {
            var o = new StructWithInterface();
            IntPtr iUnknown = Marshal.GetComInterfaceForObject<StructWithInterface, NonGenericInterface>(o);
            try
            {
                Assert.NotEqual(IntPtr.Zero, iUnknown);
            }
            finally
            {
                Marshal.Release(iUnknown);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetComInterfaceForObject_NonGenericWithValidClass_ReturnsExpected()
        {
            var o = new ClassWithInterface();
            IntPtr iUnknown = Marshal.GetComInterfaceForObject(o, typeof(NonGenericInterface));
            try
            {
                Assert.NotEqual(IntPtr.Zero, iUnknown);
            }
            finally
            {
                Marshal.Release(iUnknown);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetComInterfaceForObject_NonGenericWithValidStruct_ReturnsExpected()
        {
            var o = new StructWithInterface();
            IntPtr iUnknown = Marshal.GetComInterfaceForObject(o, typeof(NonGenericInterface));
            try
            {
                Assert.NotEqual(IntPtr.Zero, iUnknown);
            }
            finally
            {
                Marshal.Release(iUnknown);
            }
        }

        [Theory]
        [InlineData(CustomQueryInterfaceMode.Allow)]
        [InlineData(CustomQueryInterfaceMode.Ignore)]
        [InlineData(CustomQueryInterfaceMode.Allow + 1)]
        [InlineData(CustomQueryInterfaceMode.Ignore - 1)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetComInterfaceForObject_NonGenericCustomQueryInterfaceModeWithValidClass_ReturnsExpected(CustomQueryInterfaceMode mode)
        {
            var o = new ClassWithInterface();
            IntPtr iUnknown = Marshal.GetComInterfaceForObject(o, typeof(NonGenericInterface));
            try
            {
                Assert.NotEqual(IntPtr.Zero, iUnknown);
            }
            finally
            {
                Marshal.Release(iUnknown);
            }
        }

        [Theory]
        [InlineData(CustomQueryInterfaceMode.Allow)]
        [InlineData(CustomQueryInterfaceMode.Ignore)]
        [InlineData(CustomQueryInterfaceMode.Allow + 1)]
        [InlineData(CustomQueryInterfaceMode.Ignore - 1)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetComInterfaceForObject_NonGenericCustomQueryInterfaceModeWithValidStruct_ReturnsExpected(CustomQueryInterfaceMode mode)
        {
            var o = new StructWithInterface();
            IntPtr iUnknown = Marshal.GetComInterfaceForObject(o, typeof(NonGenericInterface), mode);
            try
            {
                Assert.NotEqual(IntPtr.Zero, iUnknown);
            }
            finally
            {
                Marshal.Release(iUnknown);
            }
        }

        public class ClassWithInterface : NonGenericInterface { }
        public class StructWithInterface : NonGenericInterface { }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void GetComInterfaceForObject_Unix_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetComInterfaceForObject(null, null));
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetComInterfaceForObject(null, null, CustomQueryInterfaceMode.Allow));
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetComInterfaceForObject<int, int>(1));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetComInterfaceForObject_NullObject_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("o", () => Marshal.GetComInterfaceForObject(null, typeof(NonGenericInterface)));
            AssertExtensions.Throws<ArgumentNullException>("o", () => Marshal.GetComInterfaceForObject(null, typeof(NonGenericInterface), CustomQueryInterfaceMode.Allow));
            AssertExtensions.Throws<ArgumentNullException>("o", () => Marshal.GetComInterfaceForObject<string, string>(null));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetComInterfaceForObject_NullType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("t", () => Marshal.GetComInterfaceForObject(new object(), null));
            AssertExtensions.Throws<ArgumentNullException>("t", () => Marshal.GetComInterfaceForObject(new object(), null, CustomQueryInterfaceMode.Allow));
        }

        public static IEnumerable<object[]> GetComInterfaceForObject_InvalidType_TestData()
        {
            yield return new object[] { typeof(int).MakeByRefType() };
            yield return new object[] { typeof(int).MakePointerType() };
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

            yield return new object[] { typeof(NonComVisibleClass) };
            yield return new object[] { typeof(NonComVisibleStruct) };
            yield return new object[] { typeof(NonComVisibleInterface) };

#if !netstandard // TODO: Enable for netstandard2.1
            AssemblyBuilder collectibleAssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder collectibleModuleBuilder = collectibleAssemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder collectibleTypeBuilder = collectibleModuleBuilder.DefineType("Type", TypeAttributes.Interface | TypeAttributes.Abstract);
            Type collectibleType = collectibleTypeBuilder.CreateType();
            yield return new object[] { collectibleType };
#endif
        }

        [Theory]
        [MemberData(nameof(GetComInterfaceForObject_InvalidType_TestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetComInterfaceForObject_InvalidType_ThrowsArgumentException(Type type)
        {
            AssertExtensions.Throws<ArgumentException>("t", () => Marshal.GetComInterfaceForObject(new object(), type));
            AssertExtensions.Throws<ArgumentException>("t", () => Marshal.GetComInterfaceForObject(new object(), type, CustomQueryInterfaceMode.Allow));
        }

        public static IEnumerable<object[]> GetComInterfaceForObject_InvalidObject_TestData()
        {
            yield return new object[] { new GenericClass<string>() };
            yield return new object[] { new GenericStruct<string>() };
        }

        [Theory]
        [MemberData(nameof(GetComInterfaceForObject_InvalidObject_TestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetComInterfaceForObject_InvalidObject_ThrowsArgumentException(object o)
        {
            AssertExtensions.Throws<ArgumentException>("o", () => Marshal.GetComInterfaceForObject(o, typeof(NonGenericInterface)));
            AssertExtensions.Throws<ArgumentException>("o", () => Marshal.GetComInterfaceForObject(o, typeof(NonGenericInterface), CustomQueryInterfaceMode.Allow));
            AssertExtensions.Throws<ArgumentException>("o", () => Marshal.GetComInterfaceForObject<object, NonGenericInterface>(o));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetTypedObjectForIUnknown_UncastableType_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => Marshal.GetComInterfaceForObject(new object(), typeof(NonGenericInterface)));
            Assert.Throws<InvalidCastException>(() => Marshal.GetComInterfaceForObject(new object(), typeof(NonGenericInterface), CustomQueryInterfaceMode.Allow));
            Assert.Throws<InvalidCastException>(() => Marshal.GetComInterfaceForObject<object, NonGenericInterface>(new object()));
        }
    }
}
