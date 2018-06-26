// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeAssignStatementTests : CodeStatementTestBase<CodeAssignStatement>
	{
		[Fact]
		public void Ctor_Default()
		{
			var assign = new CodeAssignStatement();
			Assert.Null(assign.Left);
			Assert.Null(assign.Right);
		}

		public static IEnumerable<object[]> Ctor_TestData()
		{
			yield return new object[] { null, null };
			yield return new object[] { new CodePrimitiveExpression("Value1"), null };
			yield return new object[] { null, new CodePrimitiveExpression("Value2") };
			yield return new object[] { new CodePrimitiveExpression("Value1"), new CodePrimitiveExpression("Value2") };
		}

		[Theory]
		[MemberData(nameof(Ctor_TestData))]
		public void Ctor_CodeExpression_CodeExpression(CodeExpression left, CodeExpression right)
		{
			var assign = new CodeAssignStatement(left, right);
			Assert.Equal(left, assign.Left);
			Assert.Equal(right, assign.Right);
		}

		[Theory]
		[MemberData(nameof(CodeExpression_TestData))]
		public void Left_Set_Get_ReturnsExpected(CodeExpression value)
		{
			var assign = new CodeAssignStatement();
			assign.Left = value;
			Assert.Equal(value, assign.Left);
		}

		[Theory]
		[MemberData(nameof(CodeExpression_TestData))]
		public void Right_Set_Get_ReturnsExpected(CodeExpression value)
		{
			var assign = new CodeAssignStatement();
			assign.Right = value;
			Assert.Equal(value, assign.Right);
		}
	}
}
