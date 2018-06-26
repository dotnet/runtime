// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeLabeledStatementTests : CodeStatementTestBase<CodeLabeledStatement>
	{
		[Fact]
		public void Ctor_Default()
		{
			var labeled = new CodeLabeledStatement();
			Assert.Empty(labeled.Label);
			Assert.Null(labeled.Statement);
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void Ctor_String(string label)
		{
			var labeled = new CodeLabeledStatement(label);
			Assert.Equal(label ?? string.Empty, labeled.Label);
			Assert.Null(labeled.Statement);
		}

		public static IEnumerable<object[]> Ctor_String_CodeStatement_TestData()
		{
			yield return new object[] { null, null };
			yield return new object[] { "", new CodeStatement() };
			yield return new object[] { "Label", new CodeCommentStatement("Comment") };
		}

		[Theory]
		[MemberData(nameof(Ctor_String_CodeStatement_TestData))]
		public void Ctor_String_CodeStatement(string label, CodeStatement statement)
		{
			var labeled = new CodeLabeledStatement(label, statement);
			Assert.Equal(label ?? string.Empty, labeled.Label);
			Assert.Equal(statement, labeled.Statement);
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void Label_Set_Get_ReturnsExpected(string value)
		{
			var labeled = new CodeLabeledStatement();
			labeled.Label = value;
			Assert.Equal(value ?? string.Empty, labeled.Label);
		}

		[Theory]
		[MemberData(nameof(CodeStatement_TestData))]
		public void Statement_Set_Get_ReturnsExpected(CodeStatement value)
		{
			var labeled = new CodeLabeledStatement();
			labeled.Statement = value;
			Assert.Equal(value, labeled.Statement);
		}
	}
}
