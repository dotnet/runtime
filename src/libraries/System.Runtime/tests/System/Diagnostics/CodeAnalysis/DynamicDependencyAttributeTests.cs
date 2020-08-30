// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Diagnostics.CodeAnalysis.Tests
{
    public class DynamicDependencyAttributeTests
    {
        [Theory]
        [InlineData("Foo()")]
        [InlineData(null)]
        [InlineData("")]
        public void TestConstructorSignature(string memberSignature)
        {
            var dda = new DynamicDependencyAttribute(memberSignature);

            Assert.Equal(memberSignature, dda.MemberSignature);
            Assert.Equal(DynamicallyAccessedMemberTypes.None, dda.MemberTypes);
            Assert.Null(dda.Type);
            Assert.Null(dda.TypeName);
            Assert.Null(dda.AssemblyName);
            Assert.Null(dda.Condition);
        }

        [Theory]
        [InlineData("Foo()", typeof(string))]
        [InlineData(null, null)]
        [InlineData("", typeof(void))]
        public void TestConstructorSignatureType(string memberSignature, Type type)
        {
            var dda = new DynamicDependencyAttribute(memberSignature, type);

            Assert.Equal(memberSignature, dda.MemberSignature);
            Assert.Equal(DynamicallyAccessedMemberTypes.None, dda.MemberTypes);
            Assert.Equal(type, dda.Type);
            Assert.Null(dda.TypeName);
            Assert.Null(dda.AssemblyName);
            Assert.Null(dda.Condition);
        }

        [Theory]
        [InlineData("Foo()", "System.String", "System.Runtime")]
        [InlineData(null, null, null)]
        [InlineData("", "", "")]
        public void TestConstructorSignatureTypeNameAssemblyName(string memberSignature, string typeName, string assemblyName)
        {
            var dda = new DynamicDependencyAttribute(memberSignature, typeName, assemblyName);

            Assert.Equal(memberSignature, dda.MemberSignature);
            Assert.Equal(DynamicallyAccessedMemberTypes.None, dda.MemberTypes);
            Assert.Null(dda.Type);
            Assert.Equal(typeName, dda.TypeName);
            Assert.Equal(assemblyName, dda.AssemblyName);
            Assert.Null(dda.Condition);
        }

        [Theory]
        [InlineData(DynamicallyAccessedMemberTypes.PublicMethods, typeof(string))]
        [InlineData(DynamicallyAccessedMemberTypes.None, null)]
        [InlineData(DynamicallyAccessedMemberTypes.All, typeof(void))]
        public void TestConstructorMemberTypes(DynamicallyAccessedMemberTypes memberTypes, Type type)
        {
            var dda = new DynamicDependencyAttribute(memberTypes, type);

            Assert.Null(dda.MemberSignature);
            Assert.Equal(memberTypes, dda.MemberTypes);
            Assert.Equal(type, dda.Type);
            Assert.Null(dda.TypeName);
            Assert.Null(dda.AssemblyName);
            Assert.Null(dda.Condition);
        }

        [Theory]
        [InlineData(DynamicallyAccessedMemberTypes.PublicMethods, "System.String", "System.Runtime")]
        [InlineData(DynamicallyAccessedMemberTypes.None, null, null)]
        [InlineData(DynamicallyAccessedMemberTypes.All, "", "")]
        public void TestConstructorMemberTypesTypeNameAssemblyName(DynamicallyAccessedMemberTypes memberTypes, string typeName, string assemblyName)
        {
            var dda = new DynamicDependencyAttribute(memberTypes, typeName, assemblyName);

            Assert.Null(dda.MemberSignature);
            Assert.Equal(memberTypes, dda.MemberTypes);
            Assert.Null(dda.Type);
            Assert.Equal(typeName, dda.TypeName);
            Assert.Equal(assemblyName, dda.AssemblyName);
            Assert.Null(dda.Condition);
        }

        [Fact]
        public void TestCondition()
        {
            var dda = new DynamicDependencyAttribute("Foo()");
            Assert.Null(dda.Condition);

            dda.Condition = "DEBUG";
            Assert.Equal("DEBUG", dda.Condition);

            dda.Condition = null;
            Assert.Null(dda.Condition);
        }
    }
}
