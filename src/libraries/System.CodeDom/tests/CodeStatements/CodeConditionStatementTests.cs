// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeConditionStatementTests : CodeStatementTestBase<CodeConditionStatement>
	{
		[Fact]
		public void Ctor_Default()
		{
			var condition = new CodeConditionStatement();
			Assert.Null(condition.Condition);
			Assert.Empty(condition.TrueStatements);
			Assert.Empty(condition.FalseStatements);
		}

		public static IEnumerable<object[]> Ctor_TestData()
		{
			yield return new object[] { null, new CodeStatement[0], new CodeStatement[0] };
			yield return new object[] { new CodePrimitiveExpression("Value1"), new CodeStatement[] { new CodeCommentStatement("Value2") }, new CodeStatement[0] };
			yield return new object[] { new CodePrimitiveExpression("Value1"), new CodeStatement[] { new CodeCommentStatement("Value2") }, new CodeStatement[] { new CodeCommentStatement("Value3") } };
			yield return new object[] { new CodePrimitiveExpression("Value1"), new CodeStatement[0], new CodeStatement[] { new CodeCommentStatement("Value3") } };
		}

		[Theory]
		[MemberData(nameof(Ctor_TestData))]
		public void Ctor(CodeExpression expression, CodeStatement[] trueStatements, CodeStatement[] falseStatements)
		{
			if (falseStatements.Length == 0)
			{
				if (trueStatements.Length == 0)
				{
					// Use Ctor(CodeExpression)
					var condition1 = new CodeConditionStatement(expression);
					Assert.Equal(expression, condition1.Condition);
					Assert.Empty(condition1.TrueStatements);
					Assert.Empty(condition1.FalseStatements);
				}
				// Use Ctor(CodeExpression, CodeStatement[])
				var condition2 = new CodeConditionStatement(expression, trueStatements);
				Assert.Equal(expression, condition2.Condition);
				Assert.Equal(trueStatements, condition2.TrueStatements.Cast<CodeStatement>());
				Assert.Empty(condition2.FalseStatements);
			}
			// Use Ctor(CodeExpression, CodeStatement[], CodeStatement[])
			var condition3 = new CodeConditionStatement(expression, trueStatements, falseStatements);
			Assert.Equal(expression, condition3.Condition);
			Assert.Equal(trueStatements, condition3.TrueStatements.Cast<CodeStatement>());
			Assert.Equal(falseStatements, condition3.FalseStatements.Cast<CodeStatement>());
		}

		[Theory]
		[MemberData(nameof(CodeExpression_TestData))]
		public void Condition_Set_Get_ReturnsExpected(CodeExpression value)
		{
			var condition = new CodeConditionStatement();
			condition.Condition = value;
			Assert.Equal(value, condition.Condition);
		}

		[Fact]
		public void TrueStatements_AddMultiple_ReturnsExpected()
		{
			var condition = new CodeConditionStatement();

			CodeStatement statement1 = new CodeCommentStatement("Value1");
			condition.TrueStatements.Add(statement1);
			Assert.Equal(new CodeStatement[] { statement1 }, condition.TrueStatements.Cast<CodeStatement>());

			CodeStatement statement2 = new CodeCommentStatement("Value2");
			condition.TrueStatements.Add(statement2);
			Assert.Equal(new CodeStatement[] { statement1, statement2 }, condition.TrueStatements.Cast<CodeStatement>());
		}

		[Fact]
		public void FalseStatements_AddMultiple_ReturnsExpected()
		{
			var condition = new CodeConditionStatement();

			CodeStatement statement1 = new CodeCommentStatement("Value1");
			condition.FalseStatements.Add(statement1);
			Assert.Equal(new CodeStatement[] { statement1 }, condition.FalseStatements.Cast<CodeStatement>());

			CodeStatement statement2 = new CodeCommentStatement("Value2");
			condition.FalseStatements.Add(statement2);
			Assert.Equal(new CodeStatement[] { statement1, statement2 }, condition.FalseStatements.Cast<CodeStatement>());
		}
	}
}
