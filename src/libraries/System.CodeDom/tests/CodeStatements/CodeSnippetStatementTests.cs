// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeSnippetStatementTests : CodeStatementTestBase<CodeSnippetStatement>
	{
		[Fact]
		public void Ctor_Default()
		{
			var snippet = new CodeSnippetStatement();
			Assert.Empty(snippet.Value);
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void Ctor_String(string value)
		{
			var snippet = new CodeSnippetStatement(value);
			Assert.Equal(value ?? string.Empty, snippet.Value);
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void Value_Set_Get_ReturnsExpected(string value)
		{
			var snippet = new CodeSnippetStatement();
			snippet.Value = value;
			Assert.Equal(value ?? string.Empty, snippet.Value);
		}
	}
}
