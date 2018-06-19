// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeGotoStatementTests : CodeStatementTestBase<CodeGotoStatement>
	{
		[Fact]
		public void Ctor_Default()
		{
			var gotoStatement = new CodeGotoStatement();
			Assert.Null(gotoStatement.Label);
		}

		[Theory]
		[InlineData("\r n \t")]
		[InlineData("Label")]
		public void Ctor_String(string label)
		{
			var gotoStatement = new CodeGotoStatement(label);
			Assert.Equal(label, gotoStatement.Label);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		public void Ctor_StringNullOrEmpty_ThrowsArgumentNullException(string value)
		{
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeGotoStatement(value));
		}

		[Theory]
		[InlineData("\r \n \t")]
		[InlineData("Label")]
		public void Label_Set_Get_ReturnsExpected(string value)
		{
			var gotoStatement = new CodeGotoStatement();
			gotoStatement.Label = value;
			Assert.Equal(value, gotoStatement.Label);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		public void Label_SetNullOrEmpty_ThrowsArgumentNullException(string value)
		{
			var gotoStatement = new CodeGotoStatement();
			AssertExtensions.Throws<ArgumentNullException>("value", () => gotoStatement.Label = value);
		}
	}
}
