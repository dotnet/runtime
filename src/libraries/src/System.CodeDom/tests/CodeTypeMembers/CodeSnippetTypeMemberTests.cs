// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeSnippetTypeMemberTests : CodeTypeMemberTestBase<CodeSnippetTypeMember>
	{
		[Fact]
		public void Ctor_Default()
		{
			var snippet = new CodeSnippetTypeMember();
			Assert.Empty(snippet.Name);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("Text")]
		public void Ctor_String(string value)
		{
			var snippet = new CodeSnippetTypeMember(value);
			Assert.Equal(value ?? string.Empty, snippet.Text);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("Text")]
		public void Text_Set_Get_ReturnsExpected(string value)
		{
			var snippet = new CodeSnippetTypeMember();
			snippet.Text = value;
			Assert.Equal(value ?? string.Empty, snippet.Text);
		}
	}
}
