// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeSnippetCompileUnitTests : CodeObjectTestBase<CodeSnippetCompileUnit>
	{
		[Fact]
		public void Ctor_Default()
		{
			var compileUnit = new CodeSnippetCompileUnit();
			Assert.Empty(compileUnit.Value);
			Assert.Null(compileUnit.LinePragma);
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void Ctor_String(string value)
		{
			var compileUnit = new CodeSnippetCompileUnit(value);
			Assert.Equal(value ?? string.Empty, compileUnit.Value);
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void Value_Set_Get_ReturnsExpected(string value)
		{
			var compileUnit = new CodeSnippetCompileUnit();
			compileUnit.Value = value;
			Assert.Equal(value ?? string.Empty, compileUnit.Value);
		}

		[Theory]
		[MemberData(nameof(LinePragma_TestData))]
		public void LinePragma_Set_Get_ReturnsExpected(CodeLinePragma value)
		{
			var compileUnit = new CodeSnippetCompileUnit();
			compileUnit.LinePragma = value;
			Assert.Equal(value, compileUnit.LinePragma);
		}
	}
}
