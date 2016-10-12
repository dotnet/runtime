// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeArgumentReferenceExpressionTests : CodeObjectTestBase<CodeArgumentReferenceExpression>
	{
		[Fact]
		public void Ctor_Default()
		{
			var argumentReference = new CodeArgumentReferenceExpression();
			Assert.Empty(argumentReference.ParameterName);
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void Ctor_String(string parameterName)
		{
			var argumentReference = new CodeArgumentReferenceExpression(parameterName);
			Assert.Equal(parameterName ?? string.Empty, argumentReference.ParameterName);
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void ParameterName_Set_Get_ReturnsExpected(string parameterName)
		{
			var argumentReference = new CodeArgumentReferenceExpression();
			argumentReference.ParameterName = parameterName;
			Assert.Equal(parameterName ?? string.Empty, argumentReference.ParameterName);
		}
	}
}
