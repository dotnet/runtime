// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices.Tests.Common;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public partial class GenerateGuidForTypeTests
    {
        public static IEnumerable<object[]> GenerateGuidForType_Valid_TestData()
        {
            yield return new object[] { typeof(int) };
            yield return new object[] { typeof(int).MakePointerType() };
            yield return new object[] { typeof(int).MakeByRefType() };
            yield return new object[] { typeof(string) };
            yield return new object[] { typeof(string[]) };

            yield return new object[] { typeof(NonGenericClass) };
            yield return new object[] { typeof(GenericClass<string>) };
            yield return new object[] { typeof(AbstractClass) };

            yield return new object[] { typeof(NonGenericStruct) };
            yield return new object[] { typeof(GenericStruct<string>) };

            yield return new object[] { typeof(INonGenericInterface) };
            yield return new object[] { typeof(IGenericInterface<string>) };

            yield return new object[] { typeof(GenericClass<>) };
            yield return new object[] { typeof(GenericClass<>).GetTypeInfo().GenericTypeParameters[0] };

            yield return new object[] { typeof(ClassWithGuidAttribute) };

            if (PlatformDetection.IsReflectionEmitSupported)
            {
                AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.RunAndCollect);
                ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
                TypeBuilder typeBuilder = moduleBuilder.DefineType("Type");
                Type collectibleType = typeBuilder.CreateType();
                yield return new object[] { collectibleType };
            }
        }

        [Theory]
        [MemberData(nameof(GenerateGuidForType_Valid_TestData))]
        public void GenerateGuidForType_ValidType_ReturnsExpected(Type type)
        {
            if (type.HasElementType)
            {
                Assert.Equal(Guid.Empty, type.GUID);
                Assert.Equal(type.GUID, Marshal.GenerateGuidForType(type));
            }
            else
            {
                Assert.Equal(type.GUID, Marshal.GenerateGuidForType(type));
            }
        }

        [Guid("12345678-0939-11d1-8be1-00c04fd8d503")]
        public class ClassWithGuidAttribute { }

        [Fact]
        public void GenerateGuidForType_NullType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => Marshal.GenerateGuidForType(null));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsReflectionEmitSupported))]
        public void GenerateGuidForType_NotRuntimeType_ThrowsArgumentException()
        {
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Assembly"), AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Type");
            AssertExtensions.Throws<ArgumentException>("type", () => Marshal.GenerateGuidForType(typeBuilder));
        }
    }
}
