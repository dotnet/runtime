// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeCastExpressionTests : CodeObjectTestBase<CodeCastExpression>
	{
		[Fact]
		public void Ctor_Default()
		{
			var cast = new CodeCastExpression();
			Assert.Equal(typeof(void).FullName, cast.TargetType.BaseType);
			Assert.Null(cast.Expression);
		}

		public static IEnumerable<object[]> Ctor_CodeTypeReference_TestData()
		{
			yield return new object[] { null, null };
			yield return new object[] { new CodeTypeReference(), new CodePrimitiveExpression() };
			yield return new object[] { new CodeTypeReference(typeof(void)), new CodePrimitiveExpression("Value") };
		}

		[Theory]
		[MemberData(nameof(Ctor_CodeTypeReference_TestData))]
		public void Ctor_CodeTypeReference_CodeExpression(CodeTypeReference type, CodeExpression expression)
		{
			CodeExpression[] initializers = new CodeExpression[0];
			var cast = new CodeCastExpression(type, expression);
			Assert.Equal((type ?? new CodeTypeReference("")).BaseType, cast.TargetType.BaseType);
			Assert.Equal(expression, cast.Expression);
		}

		public static IEnumerable<object[]> Ctor_TypeString_TestData()
		{
			yield return new object[] { null, null, "System.Void" };
			yield return new object[] { "", new CodePrimitiveExpression(), "System.Void" };
			yield return new object[] { "Int32", new CodePrimitiveExpression("Value"), "Int32" };
		}

		[Theory]
		[MemberData(nameof(Ctor_TypeString_TestData))]
		public void Ctor_String_CodeExpression(string type, CodeExpression expression, string expectedBaseType)
		{
			var cast = new CodeCastExpression(type, expression);
			Assert.Equal(expectedBaseType, cast.TargetType.BaseType);
			Assert.Equal(expression, cast.Expression);
		}

		public static IEnumerable<object[]> Ctor_Type_TestData()
		{
			yield return new object[] { typeof(int), null, "System.Int32" };
			yield return new object[] { typeof(List<>), new CodePrimitiveExpression(), "System.Collections.Generic.List`1" };
			yield return new object[] { typeof(void), new CodePrimitiveExpression("Value"), "System.Void" };
		}

		[Theory]
		[MemberData(nameof(Ctor_Type_TestData))]
		public void Ctor_Type_CodeExpression(Type type, CodeExpression expression, string expectedBaseType)
		{
			var cast = new CodeCastExpression(type, expression);
			Assert.Equal(expectedBaseType, cast.TargetType.BaseType);
			Assert.Equal(expression, cast.Expression);
		}

		[Theory]
		[MemberData(nameof(CodeTypeReference_TestData))]
		public void TargetType_Set_Get_ReturnsExpected(CodeTypeReference value)
		{
			var cast = new CodeCastExpression();
			cast.TargetType = value;
			Assert.Equal((value ?? new CodeTypeReference("")).BaseType, cast.TargetType.BaseType);
		}

		[Theory]
		[MemberData(nameof(CodeExpression_TestData))]
		public void Expression_Set_Get_ReturnsExpected(CodeExpression value)
		{
			var cast = new CodeCastExpression();
			cast.Expression = value;
			Assert.Equal(value, cast.Expression);
		}
	}
}
