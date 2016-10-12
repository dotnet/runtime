// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeCommentTests : CodeObjectTestBase<CodeComment>
	{
		[Fact]
		public void Ctor_Default()
		{
			var comment = new CodeComment();
			Assert.Empty(comment.Text);
			Assert.False(comment.DocComment);
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void Ctor_String(string text)
		{
			var comment = new CodeComment(text);
			Assert.Equal(text ?? string.Empty, comment.Text);
			Assert.False(comment.DocComment);
		}

		public static IEnumerable<object[]> Ctor_String_Bool_TestData()
		{
			yield return new object[] { null, true };
			yield return new object[] { "", false };
			yield return new object[] { "Value", true };
		}

		[Theory]
		[MemberData(nameof(Ctor_String_Bool_TestData))]
		public void Ctor_String_Bool(string text, bool docComment)
		{
			var comment = new CodeComment(text, docComment);
			Assert.Equal(text ?? string.Empty, comment.Text);
			Assert.Equal(docComment, comment.DocComment);
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void Text_Set_Get_ReturnsExpected(string value)
		{
			var comment = new CodeComment();
			comment.Text = value;
			Assert.Equal(value ?? string.Empty, comment.Text);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void DocComment_Set_Get_ReturnsExpected(bool value)
		{
			var comment = new CodeComment();
			comment.DocComment = value;
			Assert.Equal(value, comment.DocComment);
		}
	}
}
