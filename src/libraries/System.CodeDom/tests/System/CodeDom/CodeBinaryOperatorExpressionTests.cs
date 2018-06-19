// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeBinaryOperatorExpressionTests : CodeObjectTestBase<CodeBinaryOperatorExpression>
	{
		[Fact]
		public void Ctor_HasNullLeftRightAndOperator()
		{
			var binaryOperator = new CodeBinaryOperatorExpression();
			Assert.Null(binaryOperator.Left);
			Assert.Null(binaryOperator.Right);
			Assert.Equal(CodeBinaryOperatorType.Add, binaryOperator.Operator);
		}

		public static IEnumerable<object[]> Ctor_TestData()
		{
			yield return new object[] { null, (CodeBinaryOperatorType)(-1), null };
			yield return new object[] { new CodePrimitiveExpression("Value1"), CodeBinaryOperatorType.Add, null };
			yield return new object[] { null, CodeBinaryOperatorType.Add | CodeBinaryOperatorType.BitwiseAnd, new CodePrimitiveExpression("Value2") };
			yield return new object[] { new CodePrimitiveExpression("Value1"), (CodeBinaryOperatorType)int.MaxValue, new CodePrimitiveExpression("Value2") };
		}

		[Theory]
		[MemberData(nameof(Ctor_TestData))]
		public void Ctor_CodeExpression_CodeBinaryOperatorType_CodeExpression(CodeExpression left, CodeBinaryOperatorType operatorType, CodeExpression right)
		{
			var binaryOperator = new CodeBinaryOperatorExpression(left, operatorType, right);
			Assert.Equal(left, binaryOperator.Left);
			Assert.Equal(operatorType, binaryOperator.Operator);
			Assert.Equal(right, binaryOperator.Right);
		}

		[Theory]
		[MemberData(nameof(CodeExpression_TestData))]
		public void Left_Set_Get_ReturnsExpected(CodeExpression value)
		{
			var binaryOperator = new CodeBinaryOperatorExpression();
			binaryOperator.Left = value;
			Assert.Equal(value, binaryOperator.Left);
		}

		[Theory]
		[InlineData((CodeBinaryOperatorType)(-1))]
		[InlineData(CodeBinaryOperatorType.Add)]
		[InlineData(CodeBinaryOperatorType.Add | CodeBinaryOperatorType.BitwiseAnd)]
		[InlineData((CodeBinaryOperatorType)int.MaxValue)]
		public void Operator_Set_Get_ReturnsExpected(CodeBinaryOperatorType value)
		{
			var binaryOperator = new CodeBinaryOperatorExpression();
			binaryOperator.Operator = value;
			Assert.Equal(value, binaryOperator.Operator);
		}

		[Theory]
		[MemberData(nameof(CodeExpression_TestData))]
		public void Right_Set_Get_ReturnsExpected(CodeExpression value)
		{
			var binaryOperator = new CodeBinaryOperatorExpression();
			binaryOperator.Right = value;
			Assert.Equal(value, binaryOperator.Right);
		}
	}
}
