// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeIterationStatementTests : CodeStatementTestBase<CodeIterationStatement>
	{
		[Fact]
		public void Ctor_Default()
		{
			var iteration = new CodeIterationStatement();
			Assert.Null(iteration.InitStatement);
			Assert.Null(iteration.TestExpression);
			Assert.Null(iteration.IncrementStatement);

			Assert.Empty(iteration.Statements);
		}

		public static IEnumerable<object[]> Ctor_TestData()
		{
			yield return new object[] { null, null, null, new CodeStatement[0] };
			yield return new object[] { new CodeCommentStatement("1"), new CodePrimitiveExpression("2"), new CodeCommentStatement("3"), new CodeStatement[] { new CodeCommentStatement("4") } };
		}

		[Theory]
		[MemberData(nameof(Ctor_TestData))]
		public void Ctor(CodeStatement initStatement, CodeExpression testExpression, CodeStatement incrementStatement, CodeStatement[] statements)
		{
			var iteration = new CodeIterationStatement(initStatement, testExpression, incrementStatement, statements);
			Assert.Equal(initStatement, iteration.InitStatement);
			Assert.Equal(testExpression, iteration.TestExpression);
			Assert.Equal(incrementStatement, iteration.IncrementStatement);
			Assert.Equal(statements, iteration.Statements.Cast<CodeStatement>());
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		public void Ctor_NullStatements_ThrowsArgumentNullException(string value)
		{
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeIterationStatement(null, null, null, null));
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		public void Ctor_NullObjectInStatements_ThrowsArgumentNullException(string value)
		{
			CodeStatement[] statements = new CodeStatement[] { null };
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeIterationStatement(null, null, null, statements));
		}

		[Theory]
		[MemberData(nameof(CodeStatement_TestData))]
		public void InitStatement_Set_Get_ReturnsExpected(CodeStatement value)
		{
			var iteration = new CodeIterationStatement();
			iteration.InitStatement = value;
			Assert.Equal(value, iteration.InitStatement);
		}

		[Theory]
		[MemberData(nameof(CodeExpression_TestData))]
		public void TestExpression_Set_Get_ReturnsExpected(CodeExpression value)
		{
			var iteration = new CodeIterationStatement();
			iteration.TestExpression = value;
			Assert.Equal(value, iteration.TestExpression);
		}

		[Theory]
		[MemberData(nameof(CodeStatement_TestData))]
		public void IncrementStatement_Set_Get_ReturnsExpected(CodeStatement value)
		{
			var iteration = new CodeIterationStatement();
			iteration.IncrementStatement = value;
			Assert.Equal(value, iteration.IncrementStatement);
		}

		[Fact]
		public void Statements_AddMultiple_ReturnsExpected()
		{
			var iteration = new CodeIterationStatement();

			CodeStatement statement1 = new CodeCommentStatement("Value1");
			iteration.Statements.Add(statement1);
			Assert.Equal(new CodeStatement[] { statement1 }, iteration.Statements.Cast<CodeStatement>());

			CodeStatement statement2 = new CodeCommentStatement("Value2");
			iteration.Statements.Add(statement2);
			Assert.Equal(new CodeStatement[] { statement1, statement2 }, iteration.Statements.Cast<CodeStatement>());
		}
	}
}
