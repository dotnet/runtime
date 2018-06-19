// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeTypeOfExpressionTests : CodeObjectTestBase<CodeTypeOfExpression>
	{
		[Fact]
		public void Ctor_Default()
		{
			var typeofExpression = new CodeTypeOfExpression();
			Assert.Equal(typeof(void).FullName, typeofExpression.Type.BaseType);
		}

		[Theory]
		[MemberData(nameof(CodeTypeReference_TestData))]
		public void Ctor_CodeTypeReference_CodeExpression(CodeTypeReference type)
		{
			var typeofExpression = new CodeTypeOfExpression(type);
			Assert.Equal((type ?? new CodeTypeReference("")).BaseType, typeofExpression.Type.BaseType);
		}

		public static IEnumerable<object[]> Ctor_TypeString_TestData()
		{
			yield return new object[] { null, "System.Void" };
			yield return new object[] { "", "System.Void" };
			yield return new object[] { "Int32", "Int32" };
		}

		[Theory]
		[MemberData(nameof(Ctor_TypeString_TestData))]
		public void Ctor_String_CodeExpression(string type, string expectedBaseType)
		{
			var typeofExpression = new CodeTypeOfExpression(type);
			Assert.Equal(expectedBaseType, typeofExpression.Type.BaseType);
		}

		public static IEnumerable<object[]> Ctor_Type_TestData()
		{
			yield return new object[] { typeof(int), "System.Int32" };
			yield return new object[] { typeof(List<>), "System.Collections.Generic.List`1" };
			yield return new object[] { typeof(void), "System.Void" };
		}

		[Theory]
		[MemberData(nameof(Ctor_Type_TestData))]
		public void Ctor_Type_CodeExpression(Type type, string expectedBaseType)
		{
			var typeofExpression = new CodeTypeOfExpression(type);
			Assert.Equal(expectedBaseType, typeofExpression.Type.BaseType);
		}

		[Fact]
		public void Ctor_NullType_ThrowsArgumentNullException()
		{
			AssertExtensions.Throws<ArgumentNullException>("type", () => new CodeTypeOfExpression((Type)null));
		}

		[Theory]
		[MemberData(nameof(CodeTypeReference_TestData))]
		public void Type_Set_Get_ReturnsExpected(CodeTypeReference value)
		{
			var typeofExpression = new CodeTypeOfExpression();
			typeofExpression.Type = value;
			Assert.Equal((value ?? new CodeTypeReference("")).BaseType, typeofExpression.Type.BaseType);
		}
	}
}
