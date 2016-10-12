// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeNamespaceImportTests : CodeObjectTestBase<CodeNamespaceImport>
	{
		[Fact]
		public void Ctor_Default()
		{
			var import = new CodeNamespaceImport();
			Assert.Empty(import.Namespace);
			Assert.Null(import.LinePragma);
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void Ctor_String(string nameSpace)
		{
			var import = new CodeNamespaceImport(nameSpace);
			Assert.Equal(nameSpace ?? string.Empty, import.Namespace);
			Assert.Null(import.LinePragma);
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void Namespace_Set_Get_ReturnsExpected(string value)
		{
			var import = new CodeNamespaceImport();
			import.Namespace = value;
			Assert.Equal(value ?? string.Empty, import.Namespace);
		}

		[Theory]
		[MemberData(nameof(LinePragma_TestData))]
		public void LinePragma_Set_Get_ReturnsExpected(CodeLinePragma value)
		{
			var import = new CodeNamespaceImport();
			import.LinePragma = value;
			Assert.Equal(value, import.LinePragma);
		}
	}
}
