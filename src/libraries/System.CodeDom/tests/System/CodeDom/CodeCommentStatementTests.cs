// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeCommentStatementTests : CodeStatementTestBase<CodeCommentStatement>
	{
		[Fact]
		public void Ctor_Default()
		{
			var comment = new CodeCommentStatement();
			Assert.Null(comment.Comment);
		}

		public static IEnumerable<object[]> Comment_TestData()
		{
			yield return new object[] { null, false };
			yield return new object[] { "", true };
			yield return new object[] { "Text", false };
			yield return new object[] { "text", true };
		}

		[Theory]
		[MemberData(nameof(Comment_TestData))]
		public void Ctor_String(string text, bool docComment)
		{
			CodeCommentStatement comment = new CodeCommentStatement(text);
			Assert.Equal(text ?? string.Empty, comment.Comment.Text);
			Assert.False(comment.Comment.DocComment);
		}

		[Theory]
		[MemberData(nameof(Comment_TestData))]
		public void Ctor_String_Bool(string text, bool docComment)
		{
			CodeCommentStatement comment = new CodeCommentStatement(text, docComment);
			Assert.Equal(text ?? string.Empty, comment.Comment.Text);
			Assert.Equal(docComment, comment.Comment.DocComment);
		}

		[Theory]
		[MemberData(nameof(Comment_TestData))]
		public void Ctor_CodeComment(string text, bool docComment)
		{
			CodeComment codeComment = new CodeComment(text, docComment);
			CodeCommentStatement comment = new CodeCommentStatement(codeComment);
			Assert.Same(codeComment, comment.Comment);
		}

		[Fact]
		public void Ctor_CodeComment_Null()
		{
			CodeCommentStatement comment = new CodeCommentStatement((CodeComment)null);
			Assert.Null(comment.Comment);
		}

		[Theory]
		[MemberData(nameof(Comment_TestData))]
		public void Comment_Set_Get_ReturnsExpected(string text, bool docComment)
		{
			CodeComment codeComment = new CodeComment(text, docComment);
			CodeCommentStatement comment = new CodeCommentStatement();
			comment.Comment = codeComment;
			Assert.Same(codeComment, comment.Comment);
		}

		[Fact]
		public void Ctor_SetNull_Get_ReturnsNull()
		{
			CodeCommentStatement comment = new CodeCommentStatement();
			comment.Comment = null;
			Assert.Null(comment.Comment);
		}
	}
}
