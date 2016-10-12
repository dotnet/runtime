// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeDefaultValueExpressionTests : CodeObjectTestBase<CodeDefaultValueExpression>
	{
		[Fact]
		public void Ctor_Default()
		{
			var defaultValue = new CodeDefaultValueExpression();
			Assert.Equal(typeof(void).FullName, defaultValue.Type.BaseType);
		}

		[Theory]
		[MemberData(nameof(CodeTypeReference_TestData))]
		public void Ctor_CodeTypeReference(CodeTypeReference type)
		{
			var defaultValue = new CodeDefaultValueExpression(type);
			Assert.Equal((type ?? new CodeTypeReference("")).BaseType, defaultValue.Type.BaseType);
		}

		[Theory]
		[MemberData(nameof(CodeTypeReference_TestData))]
		public void Type_Set_Get_ReturnsExpected(CodeTypeReference value)
		{
			var defaultValue = new CodeDefaultValueExpression();
			defaultValue.Type = value;
			Assert.Equal((value ?? new CodeTypeReference("")).BaseType, defaultValue.Type.BaseType);
		}
	}
}
