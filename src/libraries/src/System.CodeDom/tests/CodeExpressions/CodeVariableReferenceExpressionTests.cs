// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeVariableReferenceExpressionTests : CodeObjectTestBase<CodeVariableReferenceExpression>
	{
		[Fact]
		public void Ctor_Default()
		{
			var variableReference = new CodeVariableReferenceExpression();
			Assert.Empty(variableReference.VariableName);
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void Ctor_String(string VariableName)
		{
			var variableReference = new CodeVariableReferenceExpression(VariableName);
			Assert.Equal(VariableName ?? string.Empty, variableReference.VariableName);
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void VariableName_Set_Get_ReturnsExpected(string VariableName)
		{
			var variableReference = new CodeVariableReferenceExpression();
			variableReference.VariableName = VariableName;
			Assert.Equal(VariableName ?? string.Empty, variableReference.VariableName);
		}
	}
}
