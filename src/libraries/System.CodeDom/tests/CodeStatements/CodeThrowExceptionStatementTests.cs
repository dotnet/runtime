// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeThrowExceptionStatementTests : CodeStatementTestBase<CodeThrowExceptionStatement>
	{
		[Fact]
		public void Ctor_Default()
		{
			var throwException = new CodeThrowExceptionStatement();
			Assert.Null(throwException.ToThrow);
		}

		[Theory]
		[MemberData(nameof(CodeExpression_TestData))]
		public void Ctor_CodeExpression(CodeExpression expression)
		{
			var throwException = new CodeThrowExceptionStatement(expression);
			Assert.Equal(expression, throwException.ToThrow);
		}

		[Theory]
		[MemberData(nameof(CodeExpression_TestData))]
		public void Expression_Set_Get_ReturnsExpected(CodeExpression value)
		{
			var throwException = new CodeThrowExceptionStatement();
			throwException.ToThrow = value;
			Assert.Equal(value, throwException.ToThrow);
		}
	}
}
