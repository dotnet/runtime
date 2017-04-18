// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeCatchClauseTests : CodeDomTestBase
	{
		[Fact]
		public void Ctor_Default()
		{
			var catchClause = new CodeCatchClause();
			Assert.Empty(catchClause.LocalName);
			Assert.Empty(catchClause.Statements);

			Assert.Equal(new CodeTypeReference(typeof(Exception)).BaseType, catchClause.CatchExceptionType.BaseType);
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void Ctor_String(string localName)
		{
			var catchClause = new CodeCatchClause(localName);
			Assert.Equal(localName ?? string.Empty, catchClause.LocalName);
			Assert.Equal(typeof(Exception).ToString(), catchClause.CatchExceptionType.BaseType);
			Assert.Empty(catchClause.Statements);
		}

		public static IEnumerable<object[]> Ctor_String_CodeTypeReference_TestData()
		{
			yield return new object[] { null, null };
			yield return new object[] { "", new CodeTypeReference() };
			yield return new object[] { "Value1", new CodeTypeReference(typeof(ArgumentException)) };
		}

		[Theory]
		[MemberData(nameof(Ctor_String_CodeTypeReference_TestData))]
		public void Ctor_String_CodeTypeReference(string localName, CodeTypeReference catchExceptionType)
		{
			var catchClause = new CodeCatchClause(localName, catchExceptionType);
			Assert.Equal(localName ?? string.Empty, catchClause.LocalName);
			Assert.Equal((catchExceptionType ?? new CodeTypeReference(typeof(Exception))).BaseType, catchClause.CatchExceptionType.BaseType);
			Assert.Empty(catchClause.Statements);
		}

		public static IEnumerable<object[]> Ctor_String_CodeTypeReference_ParamsCodeStatement_TestData()
		{
			yield return new object[] { null, null, new CodeStatement[0] };
			yield return new object[] { "", new CodeTypeReference(), new CodeStatement[] { new CodeCommentStatement("") } };
			yield return new object[] { "Value1", new CodeTypeReference(typeof(ArgumentException)), new CodeStatement[] { new CodeCommentStatement("") } };
		}

		[Theory]
		[MemberData(nameof(Ctor_String_CodeTypeReference_ParamsCodeStatement_TestData))]
		public void Ctor_String_CodeTypeReference_ParamsCodeStatement(string localName, CodeTypeReference catchExceptionType, CodeStatement[] statements)
		{
			var catchClause = new CodeCatchClause(localName, catchExceptionType, statements);
			Assert.Equal(localName ?? string.Empty, catchClause.LocalName);
			Assert.Equal((catchExceptionType ?? new CodeTypeReference(typeof(Exception))).BaseType, catchClause.CatchExceptionType.BaseType);
			Assert.Equal(statements, catchClause.Statements.Cast<CodeStatement>());
		}

		[Fact]
		public void Ctor_NullStatements_ThrowsArgumentNullException()
		{
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeCatchClause("name", new CodeTypeReference(typeof(void)), null));
		}

		[Fact]
		public void Ctor_NullObjectInStatements_ThrowsArgumentNullException()
		{
			CodeStatement[] statements = new CodeStatement[] { null };
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeCatchClause("name", new CodeTypeReference(typeof(void)), statements));
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void LocalName_Set_Get_ReturnsExpected(string value)
		{
			var catchClause = new CodeCatchClause();
			catchClause.LocalName = value;
			Assert.Equal(value ?? string.Empty, catchClause.LocalName);
		}

		[Theory]
		[MemberData(nameof(CodeTypeReference_TestData))]
		public void CatchExceptionType_Set_Get_ReturnsExpected(CodeTypeReference value)
		{
			var catchClause = new CodeCatchClause();
			catchClause.CatchExceptionType = value;
			Assert.Equal((value ?? new CodeTypeReference(typeof(Exception))).BaseType, catchClause.CatchExceptionType.BaseType);
		}

		[Fact]
		public void Statements_AddMultiple_ReturnsExpected()
		{
			var catchClause = new CodeCatchClause();

			CodeStatement statement1 = new CodeCommentStatement("Value1");
			catchClause.Statements.Add(statement1);
			Assert.Equal(new CodeStatement[] { statement1 }, catchClause.Statements.Cast<CodeStatement>());

			CodeStatement statement2 = new CodeCommentStatement("Value1");
			catchClause.Statements.Add(statement2);
			Assert.Equal(new CodeStatement[] { statement1, statement2 }, catchClause.Statements.Cast<CodeStatement>());
		}
	}
}
