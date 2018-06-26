// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeTryCatchFinallyStatementTests : CodeStatementTestBase<CodeTryCatchFinallyStatement>
	{
		[Fact]
		public void Ctor_Default()
		{
			var tryCatchFinally = new CodeTryCatchFinallyStatement();
			Assert.Empty(tryCatchFinally.TryStatements);
			Assert.Empty(tryCatchFinally.CatchClauses);
			Assert.Empty(tryCatchFinally.FinallyStatements);
		}

		public static IEnumerable<object[]> Ctor_CodeStatementArray_CodeCatchClauseArray_TestData()
		{
			yield return new object[] { new CodeStatement[0], new CodeCatchClause[0] };
			yield return new object[] { new CodeStatement[] { new CodeCommentStatement("Comment") }, new CodeCatchClause[0] };
			yield return new object[] { new CodeStatement[0], new CodeCatchClause[] { new CodeCatchClause("Local") } };
			yield return new object[] { new CodeStatement[] { new CodeCommentStatement("Comment") }, new CodeCatchClause[] { new CodeCatchClause("Local") } };
		}

		[Theory]
		[MemberData(nameof(Ctor_CodeStatementArray_CodeCatchClauseArray_TestData))]
		public void Ctor_CodeStatementArray_CodeCatchClauseArray(CodeStatement[] tryStatements, CodeCatchClause[] catchClauses)
		{
			var tryCatchFinally = new CodeTryCatchFinallyStatement(tryStatements, catchClauses);
			Assert.Equal(tryStatements, tryCatchFinally.TryStatements.Cast<CodeStatement>());
			Assert.Equal(catchClauses, tryCatchFinally.CatchClauses.Cast<CodeCatchClause>());
			Assert.Empty(tryCatchFinally.FinallyStatements);
		}

		public static IEnumerable<object[]> Ctor_CodeStatementArray_CodeCatchClauseArray_CodeStatementArray_TestData()
		{
			yield return new object[] { new CodeStatement[0], new CodeCatchClause[0], new CodeStatement[0] };
			yield return new object[] { new CodeStatement[] { new CodeCommentStatement("Comment") }, new CodeCatchClause[0], new CodeStatement[0] };
			yield return new object[] { new CodeStatement[0], new CodeCatchClause[] { new CodeCatchClause("Local") }, new CodeStatement[0] };
			yield return new object[] { new CodeStatement[0], new CodeCatchClause[0], new CodeStatement[] { new CodeCommentStatement("Comment") } };
			yield return new object[] { new CodeStatement[] { new CodeCommentStatement("Comment") }, new CodeCatchClause[] { new CodeCatchClause("Local") }, new CodeStatement[] { new CodeCommentStatement("Comment") } };
		}

		[Theory]
		[MemberData(nameof(Ctor_CodeStatementArray_CodeCatchClauseArray_CodeStatementArray_TestData))]
		public void Ctor_CodeStatementArray_CodeCatchClauseArray_CodeStatementArray(CodeStatement[] tryStatements, CodeCatchClause[] catchClauses, CodeStatement[] finallyStatements)
		{
			var tryCatchFinally = new CodeTryCatchFinallyStatement(tryStatements, catchClauses, finallyStatements);
			Assert.Equal(tryStatements, tryCatchFinally.TryStatements.Cast<CodeStatement>());
			Assert.Equal(catchClauses, tryCatchFinally.CatchClauses.Cast<CodeCatchClause>());
			Assert.Equal(finallyStatements, tryCatchFinally.FinallyStatements.Cast<CodeStatement>());
		}

		[Fact]
		public void Ctor_NullTryStatements_ThrowsArgumentNullException()
		{
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeTryCatchFinallyStatement(null, new CodeCatchClause[0]));
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeTryCatchFinallyStatement(null, new CodeCatchClause[0], new CodeStatement[0]));
		}

		[Fact]
		public void Ctor_NullObjectInTryStatements_ThrowsArgumentNullException()
		{
			CodeStatement[] tryStatements = new CodeStatement[] { null };
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeTryCatchFinallyStatement(tryStatements, new CodeCatchClause[0]));
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeTryCatchFinallyStatement(tryStatements, new CodeCatchClause[0], new CodeStatement[0]));
		}

		[Fact]
		public void Ctor_NullCodeCatchClauses_ThrowsArgumentNullException()
		{
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeTryCatchFinallyStatement(new CodeStatement[0], null));
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeTryCatchFinallyStatement(new CodeStatement[0], null, new CodeStatement[0]));
		}

		[Fact]
		public void Ctor_NullObjectInCodeCatchClauses_ThrowsArgumentNullException()
		{
			CodeCatchClause[] catchClauses = new CodeCatchClause[] { null };
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeTryCatchFinallyStatement(new CodeStatement[0], catchClauses));
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeTryCatchFinallyStatement(new CodeStatement[0], catchClauses, new CodeStatement[0]));
		}

		[Fact]
		public void Ctor_NullFinallyStatements_ThrowsArgumentNullException()
		{
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeTryCatchFinallyStatement(new CodeStatement[0], new CodeCatchClause[0], null));
		}

		[Fact]
		public void Ctor_NullObjectInFinallyStatements_ThrowsArgumentNullException()
		{
			CodeStatement[] finallyStatements = new CodeStatement[] { null };
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeTryCatchFinallyStatement(new CodeStatement[0], new CodeCatchClause[0], finallyStatements));
		}

		[Fact]
		public void TryStatements_AddMultiple_ReturnsExpected()
		{
			var tryCatchFinally = new CodeTryCatchFinallyStatement();

			CodeStatement statement1 = new CodeCommentStatement("Value1");
			tryCatchFinally.TryStatements.Add(statement1);
			Assert.Equal(new CodeStatement[] { statement1 }, tryCatchFinally.TryStatements.Cast<CodeStatement>());

			CodeStatement statement2 = new CodeCommentStatement("Value1");
			tryCatchFinally.TryStatements.Add(statement2);
			Assert.Equal(new CodeStatement[] { statement1, statement2 }, tryCatchFinally.TryStatements.Cast<CodeStatement>());
		}

		[Fact]
		public void CatchClauses_AddMultiple_ReturnsExpected()
		{
			var tryCatchFinally = new CodeTryCatchFinallyStatement();

			CodeCatchClause catchClause1 = new CodeCatchClause("Local1");
			tryCatchFinally.CatchClauses.Add(catchClause1);
			Assert.Equal(new CodeCatchClause[] { catchClause1 }, tryCatchFinally.CatchClauses.Cast<CodeCatchClause>());

			CodeCatchClause catchClause2 = new CodeCatchClause("Local2");
			tryCatchFinally.CatchClauses.Add(catchClause2);
			Assert.Equal(new CodeCatchClause[] { catchClause1, catchClause2 }, tryCatchFinally.CatchClauses.Cast<CodeCatchClause>());
		}

		[Fact]
		public void FinallyStatements_AddMultiple_ReturnsExpected()
		{
			var tryCatchFinally = new CodeTryCatchFinallyStatement();

			CodeStatement statement1 = new CodeCommentStatement("Value1");
			tryCatchFinally.FinallyStatements.Add(statement1);
			Assert.Equal(new CodeStatement[] { statement1 }, tryCatchFinally.FinallyStatements.Cast<CodeStatement>());

			CodeStatement statement2 = new CodeCommentStatement("Value1");
			tryCatchFinally.FinallyStatements.Add(statement2);
			Assert.Equal(new CodeStatement[] { statement1, statement2 }, tryCatchFinally.FinallyStatements.Cast<CodeStatement>());
		}
	}
}
