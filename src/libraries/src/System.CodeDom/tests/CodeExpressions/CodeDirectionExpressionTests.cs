// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeDirectionExpressionTests : CodeObjectTestBase<CodeDirectionExpression>
	{
		[Fact]
		public void Ctor_Default()
		{
			var direction = new CodeDirectionExpression();
			Assert.Null(direction.Expression);
			Assert.Equal(FieldDirection.In, direction.Direction);
		}

		public static IEnumerable<object[]> Ctor_TestData()
		{
			yield return new object[] { FieldDirection.In, null };
			yield return new object[] { FieldDirection.In - 1, new CodePrimitiveExpression() };
			yield return new object[] { FieldDirection.Ref + 1, new CodePrimitiveExpression("Value") };
		}

		[Theory]
		[MemberData(nameof(Ctor_TestData))]
		public void Ctor(FieldDirection direction, CodeExpression expression)
		{
			var directionExpression = new CodeDirectionExpression(direction, expression);
			Assert.Equal(direction, directionExpression.Direction);
			Assert.Equal(expression, directionExpression.Expression);
		}

		[Theory]
		[MemberData(nameof(CodeExpression_TestData))]
		public void Expression_Set_Get_ReturnsExpected(CodeExpression value)
		{
			var direction = new CodeDirectionExpression();
			direction.Expression = value;
			Assert.Equal(value, direction.Expression);
		}

		[Theory]
		[InlineData(FieldDirection.In - 1)]
		[InlineData(FieldDirection.In)]
		[InlineData(FieldDirection.In | FieldDirection.Out)]
		[InlineData(FieldDirection.Ref + 1)]
		public void Direction_Set_Get_ReturnsExpected(FieldDirection value)
		{
			var direction = new CodeDirectionExpression();
			direction.Direction = value;
			Assert.Equal(value, direction.Direction);
		}
	}
}
