// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderMakeGenericType
    {
        public static IEnumerable<object[]> MakeGenericType_TestData()
        {
            string mscorlibFullName = typeof(int).GetTypeInfo().Assembly.FullName;
            yield return new object[] { new string[] { "U", "T" }, new Type[] { typeof(string), typeof(int) }, "TestType[[System.String, " + mscorlibFullName + "],[System.Int32, " + mscorlibFullName + "]]" };

            string thisAssemblyFullName = typeof(TypeBuilderMakeGenericType).GetTypeInfo().Assembly.FullName;
            yield return new object[] { new string[] { "U", "T" }, new Type[] { typeof(MakeGenericTypeClass), typeof(MakeGenericTypeInterface) }, "TestType[[System.Reflection.Emit.Tests.MakeGenericTypeClass, " + thisAssemblyFullName + "],[System.Reflection.Emit.Tests.MakeGenericTypeInterface, " + thisAssemblyFullName + "]]" };

            yield return new object[] { new string[] { "U" }, new Type[] { typeof(string) }, "TestType[[System.String, " + mscorlibFullName + "]]" };
        }

        [Theory]
        [MemberData(nameof(MakeGenericType_TestData))]
        public void MakeGenericType(string[] genericParams, Type[] typeArguments, string expectedFullName)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            GenericTypeParameterBuilder[] typeGenParam = type.DefineGenericParameters(genericParams);
            Type genericType = type.MakeGenericType(typeArguments);
            Assert.Equal(expectedFullName, genericType.FullName);
        }

        [Fact]
        public void MakeGenericType_EmptyTypeArguments_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            Assert.Throws<InvalidOperationException>(() => type.MakeGenericType(new Type[0]));
        }

        [Fact]
        public void MakeGenericType_NullTypeArguments_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            type.DefineGenericParameters("T", "U");
            AssertExtensions.Throws<ArgumentNullException>("typeArguments", () => type.MakeGenericType(null));
        }

        [Fact]
        public void MakeGenericType_NullObjectInTypeArguments_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            type.DefineGenericParameters("T", "U");
            AssertExtensions.Throws<ArgumentNullException>("typeArguments", () => type.MakeGenericType(new Type[] { null, null }));
        }
    }

    public class MakeGenericTypeClass { }
    public interface MakeGenericTypeInterface { }
}
