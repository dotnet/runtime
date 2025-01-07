// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderCreateType
    {
        [Theory]
        [InlineData(TypeAttributes.Abstract)]
        [InlineData(TypeAttributes.AnsiClass)]
        [InlineData(TypeAttributes.AutoClass)]
        [InlineData(TypeAttributes.BeforeFieldInit)]
        [InlineData(TypeAttributes.ClassSemanticsMask | TypeAttributes.Abstract)]
        [InlineData(TypeAttributes.Public)]
        [InlineData(TypeAttributes.Sealed)]
        [InlineData(TypeAttributes.SequentialLayout)]
        [InlineData(TypeAttributes.Serializable)]
        [InlineData(TypeAttributes.SpecialName)]
        [InlineData(TypeAttributes.StringFormatMask)]
        [InlineData(TypeAttributes.UnicodeClass)]
        public void CreateType(TypeAttributes attributes)
        {
            TypeBuilder type = Helpers.DynamicType(attributes);
            Type createdType = type.CreateType();
            Assert.NotNull(createdType);
            Assert.Equal(type.Name, createdType.Name);

            TypeInfo typeInfo = type.CreateTypeInfo();
            Assert.Equal(typeInfo, createdType.GetTypeInfo());

            // Verify MetadataToken
            Assert.Equal(type.MetadataToken, typeInfo.MetadataToken);
            TypeInfo typeFromToken = (TypeInfo)type.Module.ResolveType(typeInfo.MetadataToken);
            Assert.Equal(createdType, typeFromToken);
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        [InlineData(TypeAttributes.ClassSemanticsMask)]
        [InlineData(TypeAttributes.HasSecurity)]
        [InlineData(TypeAttributes.LayoutMask)]
        [InlineData(TypeAttributes.NestedAssembly)]
        [InlineData(TypeAttributes.NestedFamANDAssem)]
        [InlineData(TypeAttributes.NestedFamily)]
        [InlineData(TypeAttributes.NestedFamORAssem)]
        [InlineData(TypeAttributes.NestedPrivate)]
        [InlineData(TypeAttributes.NestedPublic)]
        [InlineData(TypeAttributes.ReservedMask)]
        [InlineData(TypeAttributes.RTSpecialName)]
        public void CreateType_BadAttributes(TypeAttributes attributes)
        {
            try
            {
                TypeBuilder type = Helpers.DynamicType(attributes);
                Type createdType = type.CreateType();
            }
            catch (System.InvalidOperationException)
            {
                Assert.Equal(TypeAttributes.ClassSemanticsMask, attributes);
                return;
            }
            catch (System.ArgumentException)
            {
                return; // All others should fail with this exception
            }

            Assert.Fail("Type creation should have failed.");
        }

        [Fact]
        public void CreateType_NestedType()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            type.DefineNestedType("NestedType");

            Type createdType = type.CreateType();
            Assert.NotNull(createdType);
            Assert.Equal(type.Name, createdType.Name);
        }

        [Fact]
        public void CreateType_GenericType()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            type.DefineGenericParameters("T");

            Type createdType = type.CreateType();
            Assert.NotNull(createdType);
            Assert.Equal(type.Name, createdType.Name);
        }
    }
}
