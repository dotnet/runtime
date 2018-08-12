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
    public class GetTypedObjectForIUnknownTests
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetTypedObjectForIUnknown_GenericTypeParameter_ReturnsExpected()
        {
            Type type = typeof(GenericClass<>).GetTypeInfo().GenericTypeParameters[0];
            IntPtr iUnknown = Marshal.GetIUnknownForObject(new object());
            try
            {
                object typedObject = Marshal.GetTypedObjectForIUnknown(iUnknown, type);
                Assert.IsType<object>(typedObject);
            }
            finally
            {
                Marshal.Release(iUnknown);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void GetTypedObjectForIUnknown_Unix_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Marshal.GetTypedObjectForIUnknown(IntPtr.Zero, typeof(int)));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetTypedObjectForIUnknown_ZeroUnknown_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("pUnk", () => Marshal.GetTypedObjectForIUnknown(IntPtr.Zero, typeof(int)));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetTypedObjectForIUnknown_NullType_ThrowsArgumentNullException()
        {
            IntPtr iUnknown = Marshal.GetIUnknownForObject(new object());
            try
            {
                AssertExtensions.Throws<ArgumentNullException>("t", () => Marshal.GetTypedObjectForIUnknown(iUnknown, null));
            }
            finally
            {
                Marshal.Release(iUnknown);
            }
        }

        public static IEnumerable<object[]> GetTypedObjectForIUnknown_Invalid_TestData()
        {
            yield return new object[] { typeof(GenericClass<string>) };
            yield return new object[] { typeof(GenericStruct<string>) };
            yield return new object[] { typeof(GenericInterface<string>) };

            yield return new object[] { typeof(GenericClass<>) };

            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type");
            yield return new object[] { typeBuilder };
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [MemberData(nameof(GetTypedObjectForIUnknown_Invalid_TestData))]
        public void GetTypedObjectForIUnknown_InvalidType_ThrowsArgumentException(Type type)
        {
            IntPtr iUnknown = Marshal.GetIUnknownForObject(new object());
            try
            {
                AssertExtensions.Throws<ArgumentException>("t", () => Marshal.GetTypedObjectForIUnknown(iUnknown, type));
            }
            finally
            {
                Marshal.Release(iUnknown);
            }
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData(typeof(AbstractClass))]
        [InlineData(typeof(NonGenericClass))]
        [InlineData(typeof(NonGenericStruct))]
        [InlineData(typeof(NonGenericInterface))]
        public void GetTypedObjectForIUnknown_UncastableType_ThrowsInvalidCastException(Type type)
        {
            IntPtr iUnknown = Marshal.GetIUnknownForObject(new object());
            try
            {
                Assert.Throws<InvalidCastException>(() => Marshal.GetTypedObjectForIUnknown(iUnknown, type));
            }
            finally
            {
                Marshal.Release(iUnknown);
            }
        }
    }
}
