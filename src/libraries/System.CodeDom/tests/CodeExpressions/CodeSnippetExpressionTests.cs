// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeSnippetExpressionTests : CodeObjectTestBase<CodeSnippetExpression>
	{
		[Fact]
		public void Ctor_Default()
		{
			var snippet = new CodeSnippetExpression();
			Assert.Empty(snippet.Value);
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void Ctor_String(string value)
		{
			var snippet = new CodeSnippetExpression(value);
			Assert.Equal(value ?? string.Empty, snippet.Value);
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void Value_Set_Get_ReturnsExpected(string value)
		{
			var snippet = new CodeSnippetExpression();
			snippet.Value = value;
			Assert.Equal(value ?? string.Empty, snippet.Value);
		}
	}
}
