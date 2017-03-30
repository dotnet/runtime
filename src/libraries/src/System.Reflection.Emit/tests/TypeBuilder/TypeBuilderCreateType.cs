// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderCreateType
    {
        [Theory]
        [InlineData(TypeAttributes.Abstract)]
        [InlineData(TypeAttributes.AnsiClass)]
        [InlineData(TypeAttributes.AutoClass)]
        [InlineData(TypeAttributes.AutoLayout)]
        [InlineData(TypeAttributes.BeforeFieldInit)]
        [InlineData(TypeAttributes.Class)]
        [InlineData(TypeAttributes.ClassSemanticsMask | TypeAttributes.Abstract)]
        [InlineData(TypeAttributes.NotPublic)]
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
            Assert.Equal(type.Name, createdType.Name);

            Assert.Equal(type.CreateTypeInfo(), createdType.GetTypeInfo());
        }

        [Theory]
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
        [InlineData(TypeAttributes.VisibilityMask)]
        public void CreateType_BadAttributes(TypeAttributes attributes)
        {
            try
            {
                TypeBuilder type = Helpers.DynamicType(attributes);
                Type createdType = type.CreateType();
            }
            catch(System.InvalidOperationException)
            {
                Assert.Equal(TypeAttributes.ClassSemanticsMask, attributes);
                return;
            }
            catch(System.ArgumentException)
            {
                return; // All others should fail with this exception
            }

            Assert.True(false, "Type creation should have failed.");
        }

        [Fact]
        public void CreateType_NestedType()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            type.DefineNestedType("NestedType");

            Type createdType = type.CreateType();
            Assert.Equal(type.Name, createdType.Name);
        }

        [Fact]
        public void CreateType_GenericType()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            type.DefineGenericParameters("T");

            Type createdType = type.CreateType();
            Assert.Equal(type.Name, createdType.Name);
        }
    }
}
