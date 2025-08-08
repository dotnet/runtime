// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.Reflection.Tests
{
    public static unsafe partial class TypeTests
    {
        [Theory]
        [InlineData(typeof(int), "System")]
        [InlineData(typeof(string), "System")]
        [InlineData(typeof(object), "System")]
        [InlineData(typeof(DateTime), "System")]
        [InlineData(typeof(decimal), "System")]
        [InlineData(typeof(Exception), "System")]
        [InlineData(typeof(Guid), "System")]
        [InlineData(typeof(Type), "System")]
        [InlineData(typeof(Reflection.Assembly), "System.Reflection")]
        [InlineData(typeof(Collections.Generic.Dictionary<,>), "System.Collections.Generic")]
        [InlineData(typeof(Collections.Generic.List<>), "System.Collections.Generic")]
        [InlineData(typeof(Threading.Tasks.Task), "System.Threading.Tasks")]
        [InlineData(typeof(IO.FileStream), "System.IO")]
        [InlineData(typeof(TypeTests), "System.Reflection.Tests")]
        [InlineData(typeof(System.Reflection.Tests.Attr), "System.Reflection.Tests")]
        [InlineData(typeof(System.Reflection.Tests.PublicEnum), "System.Reflection.Tests")]
        public static void Namespace_ValidTypes_ReturnsExpected(Type type, string expectedNamespace)
        {
            Assert.Equal(expectedNamespace, type.Namespace);
        }

        [Theory]
        [InlineData(typeof(int[]), "System")]
        [InlineData(typeof(int[,]), "System")]
        [InlineData(typeof(int[][]), "System")]
        [InlineData(typeof(string[]), "System")]
        [InlineData(typeof(Collections.Generic.List<int>[]), "System.Collections.Generic")]
        public static void Namespace_Arrays_ReturnsElementTypeNamespace(Type type, string expectedNamespace)
        {
            Assert.Equal(expectedNamespace, type.Namespace);
        }

        [Theory]
        [InlineData(typeof(int*))]
        [InlineData(typeof(string*))]
        [InlineData(typeof(void*))]
        [InlineData(typeof(char**))]
        [InlineData(typeof(TypeTests*))]
        public static void Namespace_Pointers_ReturnsElementTypeNamespace(Type type)
        {
            Type elementType = type.GetElementType();
            Assert.Equal(elementType.Namespace, type.Namespace);
        }

        [Fact]
        public static void Namespace_FunctionPointers_ReturnsNull()
        {
            Assert.Null(typeof(delegate*<void>).Namespace);
            Assert.Null(typeof(delegate*<void*>).Namespace);
        }

        [Fact]
        public static void Namespace_ByRefTypes_ReturnsElementTypeNamespace()
        {
            Type byRefInt = typeof(int).MakeByRefType();
            Type byRefString = typeof(string).MakeByRefType();
            Type byRefCustom = typeof(TypeTests).MakeByRefType();

            Assert.Equal("System", byRefInt.Namespace);
            Assert.Equal("System", byRefString.Namespace);
            Assert.Equal("System.Reflection.Tests", byRefCustom.Namespace);
        }

        [Theory]
        [InlineData(typeof(Collections.Generic.Dictionary<int, string>), "System.Collections.Generic")]
        [InlineData(typeof(Collections.Generic.List<int>), "System.Collections.Generic")]
        [InlineData(typeof(Collections.Generic.KeyValuePair<string, int>), "System.Collections.Generic")]
        [InlineData(typeof(Nullable<int>), "System")]
        [InlineData(typeof(Func<int, string>), "System")]
        [InlineData(typeof(Action<int>), "System")]
        public static void Namespace_ConstructedGenericTypes_ReturnsGenericTypeDefinitionNamespace(Type type, string expectedNamespace)
        {
            Assert.Equal(expectedNamespace, type.Namespace);
        }

        [Fact]
        public static void Namespace_NestedTypes_ReturnsDeclaringTypeNamespace()
        {
            // Use existing nested types from the test assembly
            Assert.Equal("System.Reflection.Tests", typeof(TI_BaseClass.PublicNestedClass1).Namespace);
            Assert.Equal("System.Reflection.Tests", typeof(TI_BaseClass.PublicNestedClass2).Namespace);
            Assert.Equal("System.Reflection.Tests", typeof(TI_SubClass.PublicNestedClass1).Namespace);
            Assert.Equal("System.Reflection.Tests", typeof(MultipleNestedClass.Nest1).Namespace);
            Assert.Equal("System.Reflection.Tests", typeof(MultipleNestedClass.Nest1.Nest2).Namespace);
            Assert.Equal("System.Reflection.Tests", typeof(MultipleNestedClass.Nest1.Nest2.Nest3).Namespace);
        }

        [Fact]
        public static void Namespace_TopLevelTypeWithNoNamespace_ReturnsNull()
        {
            // Create a simple dynamic assembly with a type that has no namespace
            var assemblyBuilder = System.Reflection.Emit.AssemblyBuilder.DefineDynamicAssembly(
                new System.Reflection.AssemblyName("TestAssembly"),
                System.Reflection.Emit.AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("TestModule");
            var typeBuilder = moduleBuilder.DefineType("TopLevelTypeWithoutNamespace",
                System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class);

            Type createdType = typeBuilder.CreateType();
            Assert.Null(createdType.Namespace);
        }

        // FullName Tests
        [Theory]
        [InlineData(typeof(int), "System.Int32")]
        [InlineData(typeof(string), "System.String")]
        [InlineData(typeof(object), "System.Object")]
        [InlineData(typeof(DateTime), "System.DateTime")]
        [InlineData(typeof(decimal), "System.Decimal")]
        [InlineData(typeof(Exception), "System.Exception")]
        [InlineData(typeof(Guid), "System.Guid")]
        [InlineData(typeof(Type), "System.Type")]
        [InlineData(typeof(Reflection.Assembly), "System.Reflection.Assembly")]
        [InlineData(typeof(Collections.Generic.Dictionary<,>), "System.Collections.Generic.Dictionary`2")]
        [InlineData(typeof(Collections.Generic.List<>), "System.Collections.Generic.List`1")]
        [InlineData(typeof(Threading.Tasks.Task), "System.Threading.Tasks.Task")]
        [InlineData(typeof(IO.FileStream), "System.IO.FileStream")]
        [InlineData(typeof(TypeTests), "System.Reflection.Tests.TypeTests")]
        [InlineData(typeof(System.Reflection.Tests.Attr), "System.Reflection.Tests.Attr")]
        [InlineData(typeof(System.Reflection.Tests.PublicEnum), "System.Reflection.Tests.PublicEnum")]
        public static void FullName_ValidTypes_ReturnsExpected(Type type, string expectedFullName)
        {
            Assert.Equal(expectedFullName, type.FullName);
        }

        [Theory]
        [InlineData(typeof(int[]), "System.Int32[]")]
        [InlineData(typeof(int[,]), "System.Int32[,]")]
        [InlineData(typeof(int[,,]), "System.Int32[,,]")]
        [InlineData(typeof(int[][]), "System.Int32[][]")]
        [InlineData(typeof(int[,][]), "System.Int32[][,]")] // Metadata representation vs C# representation
        [InlineData(typeof(string[]), "System.String[]")]
        public static void FullName_Arrays_ReturnsExpected(Type type, string expectedFullName)
        {
            Assert.Equal(expectedFullName, type.FullName);
        }

        [Theory]
        [InlineData(typeof(int*), "System.Int32*")]
        [InlineData(typeof(char*), "System.Char*")]
        [InlineData(typeof(void*), "System.Void*")]
        [InlineData(typeof(byte**), "System.Byte**")]
        [InlineData(typeof(TypeTests*), "System.Reflection.Tests.TypeTests*")]
        public static void FullName_Pointers_ReturnsExpected(Type type, string expectedFullName)
        {
            Assert.Equal(expectedFullName, type.FullName);
        }

        [Fact]
        public static void FullName_FunctionPointers_ReturnsExpected()
        {
            Assert.Null(typeof(delegate*<void>).FullName);
            Assert.Equal("System.Void()*", typeof(delegate*<void>*).FullName);
        }

        [Fact]
        public static void FullName_ByRefTypes_ReturnsExpected()
        {
            Type byRefInt = typeof(int).MakeByRefType();
            Type byRefString = typeof(string).MakeByRefType();
            Type byRefCustom = typeof(TypeTests).MakeByRefType();

            Assert.Equal("System.Int32&", byRefInt.FullName);
            Assert.Equal("System.String&", byRefString.FullName);
            Assert.Equal("System.Reflection.Tests.TypeTests&", byRefCustom.FullName);
        }

        [Fact]
        public static void FullName_GenericTypeParameters_ReturnsNull()
        {
            Type[] genericParameters = typeof(Collections.Generic.Dictionary<,>).GetGenericArguments();
            foreach (Type parameter in genericParameters)
            {
                Assert.Null(parameter.FullName);
            }
        }

        [Fact]
        public static void FullName_NestedTypes_ReturnsExpected()
        {
            Assert.Equal("System.Reflection.Tests.TI_BaseClass+PublicNestedClass1", typeof(TI_BaseClass.PublicNestedClass1).FullName);
            Assert.Equal("System.Reflection.Tests.TI_BaseClass+PublicNestedClass2", typeof(TI_BaseClass.PublicNestedClass2).FullName);
            Assert.Equal("System.Reflection.Tests.TI_SubClass+PublicNestedClass1", typeof(TI_SubClass.PublicNestedClass1).FullName);
            Assert.Equal("System.Reflection.Tests.MultipleNestedClass+Nest1", typeof(MultipleNestedClass.Nest1).FullName);
            Assert.Equal("System.Reflection.Tests.MultipleNestedClass+Nest1+Nest2", typeof(MultipleNestedClass.Nest1.Nest2).FullName);
            Assert.Equal("System.Reflection.Tests.MultipleNestedClass+Nest1+Nest2+Nest3", typeof(MultipleNestedClass.Nest1.Nest2.Nest3).FullName);
        }

        [Fact]
        public static void FullName_TopLevelTypeWithNoNamespace_ReturnsOnlyName()
        {
            // Create a dynamic type with no namespace
            var assemblyBuilder = System.Reflection.Emit.AssemblyBuilder.DefineDynamicAssembly(
                new System.Reflection.AssemblyName("TestAssembly"),
                System.Reflection.Emit.AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("TestModule");
            var typeBuilder = moduleBuilder.DefineType("TopLevelTypeWithoutNamespace",
                System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Class);

            Type createdType = typeBuilder.CreateType();
            Assert.Equal("TopLevelTypeWithoutNamespace", createdType.FullName);
        }

        [Fact]
        public static void FullName_ArrayOfGenericTypeParameters_ReturnsNull()
        {
            Type genericParam = typeof(Collections.Generic.List<>).GetGenericArguments()[0];
            Type arrayOfGenericParam = genericParam.MakeArrayType();

            Assert.Null(arrayOfGenericParam.FullName);
        }

        [Fact]
        public static void FullName_PointerToGenericTypeParameter_ReturnsNull()
        {
            Type genericParam = typeof(Collections.Generic.List<>).GetGenericArguments()[0];
            Type pointerToGenericParam = genericParam.MakePointerType();

            Assert.Null(pointerToGenericParam.FullName);
        }

        [Fact]
        public static void FullName_ByRefGenericTypeParameter_ReturnsNull()
        {
            Type genericParam = typeof(Collections.Generic.List<>).GetGenericArguments()[0];
            Type byRefGenericParam = genericParam.MakeByRefType();

            Assert.Null(byRefGenericParam.FullName);
        }
    }

    // Helper types for testing nested type scenarios
    public class MultipleNestedClass
    {
        public class Nest1
        {
            public class Nest2
            {
                public class Nest3
                {
                }
            }
        }
    }
}
