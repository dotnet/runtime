// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeChecksumPragmaTests : CodeObjectTestBase<CodeChecksumPragma>
	{
		[Fact]
		public void Ctor_Default()
		{
			var pragma = new CodeChecksumPragma();
			Assert.Empty(pragma.FileName);
			Assert.Equal(Guid.Empty, pragma.ChecksumAlgorithmId);
			Assert.Null(pragma.ChecksumData);
		}

		public static IEnumerable<object[]> Ctor_TestData()
		{
			yield return new object[] { null, new Guid(), null };
			yield return new object[] { "", Guid.NewGuid(), new byte[0] };
			yield return new object[] { "Value1", Guid.NewGuid(), new byte[] { 0, 1, 2, 3 } };
		}

		[Theory]
		[MemberData(nameof(Ctor_TestData))]
		public void Ctor(string fileName, Guid algorithmId, byte[] checksumData)
		{
			var pragma = new CodeChecksumPragma(fileName, algorithmId, checksumData);
			Assert.Equal(fileName ?? string.Empty, pragma.FileName);
			Assert.Equal(algorithmId, pragma.ChecksumAlgorithmId);
			Assert.Equal(checksumData, pragma.ChecksumData);
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void FileName_Set_Get_ReturnsExpected(string value)
		{
			var pragma = new CodeChecksumPragma();
			pragma.FileName = value;
			Assert.Equal(value ?? string.Empty, pragma.FileName);
		}

		public static IEnumerable<object[]> Guid_TestData()
		{
			yield return new object[] { new Guid() };
			yield return new object[] { Guid.NewGuid() };
		}

		[Theory]
		[MemberData(nameof(Guid_TestData))]
		public void AlgorithmId_Set_Get_ReturnsExpected(Guid value)
		{
			var pragma = new CodeChecksumPragma();
			pragma.ChecksumAlgorithmId = value;
			Assert.Equal(value, pragma.ChecksumAlgorithmId);
		}

		[Theory]
		[InlineData(null)]
		[InlineData(new byte[0])]
		[InlineData(new byte[] { 0, 1, 2, 3 })]
		public void ChecksumData_Set_Get_ReturnsExpected(byte[] value)
		{
			var pragma = new CodeChecksumPragma();
			pragma.ChecksumData = value;
			Assert.Equal(value, pragma.ChecksumData);
		}
	}
}
