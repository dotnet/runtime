// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeLinePragmaTests : CodeDomTestBase
	{
		[Fact]
		public void Ctor_Default()
		{
			var linePragma = new CodeLinePragma();
			Assert.Empty(linePragma.FileName);
			Assert.Equal(0, linePragma.LineNumber);
		}

		public static IEnumerable<object[]> Ctor_String_Int_TestData()
		{
			yield return new object[] { null, 0 };
			yield return new object[] { "", -1 };
			yield return new object[] { "Value1", 1 };
		}

		[Theory]
		[MemberData(nameof(Ctor_String_Int_TestData))]
		public void Ctor_String_Int(string fileName, int lineNumber)
		{
			var linePragma = new CodeLinePragma(fileName, lineNumber);
			Assert.Equal(fileName ?? string.Empty, linePragma.FileName);
			Assert.Equal(lineNumber, linePragma.LineNumber);
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void FileName_Set_Get_ReturnsExpected(string value)
		{
			var linePragma = new CodeLinePragma();
			linePragma.FileName = value;
			Assert.Equal(value ?? string.Empty, linePragma.FileName);
		}

		[Theory]
		[InlineData(-1)]
		[InlineData(0)]
		[InlineData(1)]
		public void LineNumber_Set_Get_ReturnsExpected(int value)
		{
			var linePragma = new CodeLinePragma();
			linePragma.LineNumber = value;
			Assert.Equal(value, linePragma.LineNumber);
		}
	}
}
