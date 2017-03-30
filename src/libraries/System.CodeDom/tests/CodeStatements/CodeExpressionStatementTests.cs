// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeExpressionStatementTests : CodeStatementTestBase<CodeExpressionStatement>
	{
		[Fact]
		public void Ctor_Default()
		{
			var expressionStatement = new CodeExpressionStatement();
			Assert.Null(expressionStatement.Expression);
		}

		[Theory]
		[MemberData(nameof(CodeExpression_TestData))]
		public void Ctor_CodeExpression(CodeExpression expression)
		{
			var expressionStatement = new CodeExpressionStatement(expression);
			Assert.Equal(expression, expressionStatement.Expression);
		}

		[Theory]
		[MemberData(nameof(CodeExpression_TestData))]
		public void Expression_Set_Get_ReturnsExpected(CodeExpression value)
		{
			var expressionStatement = new CodeExpressionStatement();
			expressionStatement.Expression = value;
			Assert.Equal(value, expressionStatement.Expression);
		}
	}
}
