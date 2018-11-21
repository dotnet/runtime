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
    public partial class GetTypedObjectForIUnknownTests
    {
        public static IEnumerable<object> GetTypedObjectForIUnknown_RoundtrippableType_TestData()
        {
            yield return new object();
            yield return 10;
            yield return "string";

            yield return new NonGenericClass();
            yield return new NonGenericStruct();
            yield return Int32Enum.Value1;

            MethodInfo method = typeof(GetTypedObjectForIUnknownTests).GetMethod(nameof(NonGenericMethod));
            Delegate d = method.CreateDelegate(typeof(NonGenericDelegate));
            yield return d;
        }

        public static IEnumerable<object[]> GetTypedObjectForIUnknown_TestData()
        {
            foreach (object o in GetTypedObjectForIUnknown_RoundtrippableType_TestData())
            {
                yield return new object[] { o, o.GetType() };
                yield return new object[] { o, typeof(GenericClass<>).GetTypeInfo().GenericTypeParameters[0] };
                yield return new object[] { o, typeof(int).MakeByRefType() };

                Type baseType = o.GetType().BaseType;
                while (baseType != null)
                {
                    yield return new object[] { o, baseType };
                    baseType = baseType.BaseType;
                }
            }

            yield return new object[] { new ClassWithInterface(), typeof(NonGenericInterface) };
            yield return new object[] { new StructWithInterface(), typeof(NonGenericInterface) };

            yield return new object[] { new GenericClass<string>(), typeof(object) };
            yield return new object[] { new Dictionary<string, int>(), typeof(object) };
            yield return new object[] { new GenericStruct<string>(), typeof(object) };
            yield return new object[] { new GenericStruct<string>(), typeof(ValueType) };

            yield return new object[] { new int[] { 10 }, typeof(object) };
            yield return new object[] { new int[] { 10 }, typeof(Array) };

            yield return new object[] { new int[][] { new int[] { 10 } }, typeof(object) };
            yield return new object[] { new int[][] { new int[] { 10 } }, typeof(Array) };

            yield return new object[] { new int[,] { { 10 } }, typeof(object) };
            yield return new object[] { new int[,] { { 10 } }, typeof(Array) };

            yield return new object[] { new KeyValuePair<string, int>("key", 10), typeof(object) };
            yield return new object[] { new KeyValuePair<string, int>("key", 10), typeof(ValueType) };
        }

        [Theory]
        [MemberData(nameof(GetTypedObjectForIUnknown_TestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetTypedObjectForIUnknown_ValidPointer_ReturnsExpected(object o, Type type)
        {
            IntPtr ptr = Marshal.GetIUnknownForObject(o);
            try
            {
                Assert.Equal(o, Marshal.GetTypedObjectForIUnknown(ptr, type));
            }
            finally
            {
                Marshal.Release(ptr);
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

#if !netstandard // TODO: Enable for netstandard2.1
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type");
            yield return new object[] { typeBuilder };
#endif
        }

        [Theory]
        [MemberData(nameof(GetTypedObjectForIUnknown_Invalid_TestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetTypedObjectForIUnknown_InvalidType_ThrowsArgumentException(Type type)
        {
            IntPtr ptr = Marshal.GetIUnknownForObject(new object());
            try
            {
                AssertExtensions.Throws<ArgumentException>("t", () => Marshal.GetTypedObjectForIUnknown(ptr, type));
            }
            finally
            {
                Marshal.Release(ptr);
            }
        }

        public static IEnumerable<object[]> GetTypedObjectForIUnknownType_UncastableObject_TestData()
        {
            yield return new object[] { new object(), typeof(AbstractClass) };
            yield return new object[] { new object(), typeof(NonGenericClass) };
            yield return new object[] { new object(), typeof(NonGenericStruct) };
            yield return new object[] { new object(), typeof(NonGenericStruct) };
            yield return new object[] { new object(), typeof(NonGenericInterface) };

            yield return new object[] { new NonGenericClass(), typeof(IFormattable) };
            yield return new object[] { new ClassWithInterface(), typeof(IFormattable) };

            yield return new object[] { new object(), typeof(int).MakePointerType() };

#if !netstandard // TODO: Enable for netstandard2.1
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type");
            Type collectibleType = typeBuilder.CreateType();
            yield return new object[] { new object(), collectibleType };
#endif
        }

        [Theory]
        [MemberData(nameof(GetTypedObjectForIUnknownType_UncastableObject_TestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetTypedObjectForIUnknown_UncastableObject_ThrowsInvalidCastException(object o, Type type)
        {
            IntPtr ptr = Marshal.GetIUnknownForObject(o);
            try
            {
                Assert.Throws<InvalidCastException>(() => Marshal.GetTypedObjectForIUnknown(ptr, type));
            }
            finally
            {
                Marshal.Release(ptr);
            }
        }

        public static IEnumerable<object[]> GetTypedObjectForIUnknown_ArrayObjects_TestData()
        {
            yield return new object[] { new int[] { 10 } };
            yield return new object[] { new int[][] { new int[] { 10 } } };
            yield return new object[] { new int[,] { { 10 } } };
        }

        [Theory]
        [MemberData(nameof(GetTypedObjectForIUnknown_ArrayObjects_TestData))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetTypedObjectForIUnknown_ArrayType_ThrowsBadImageFormatException(object o)
        {
            IntPtr ptr = Marshal.GetIUnknownForObject(o);
            try
            {
                Assert.Throws<BadImageFormatException>(() => Marshal.GetTypedObjectForIUnknown(ptr, o.GetType()));
            }
            finally
            {
                Marshal.Release(ptr);
            }
        }

        public class ClassWithInterface : NonGenericInterface { }
        public struct StructWithInterface : NonGenericInterface { }

        public static void NonGenericMethod(int i) { }
        public delegate void NonGenericDelegate(int i);

        public enum Int32Enum : int { Value1, Value2 }
    }
}
