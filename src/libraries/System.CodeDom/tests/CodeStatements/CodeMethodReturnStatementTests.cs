// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeMethodReturnStatementTests : CodeStatementTestBase<CodeMethodReturnStatement>
	{
		[Fact]
		public void Ctor_Default()
		{
			var methodReturn = new CodeMethodReturnStatement();
			Assert.Null(methodReturn.Expression);
		}

		[Theory]
		[MemberData(nameof(CodeExpression_TestData))]
		public void Ctor_CodeExpression(CodeExpression expression)
		{
			var methodReturn = new CodeMethodReturnStatement(expression);
			Assert.Equal(expression, methodReturn.Expression);
		}

		[Theory]
		[MemberData(nameof(CodeExpression_TestData))]
		public void Expression_Set_Get_ReturnsExpected(CodeExpression value)
		{
			var methodReturn = new CodeMethodReturnStatement();
			methodReturn.Expression = value;
			Assert.Equal(value, methodReturn.Expression);
		}
	}
}
