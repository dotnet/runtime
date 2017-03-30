// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeFieldReferenceExpressionTests : CodeObjectTestBase<CodeFieldReferenceExpression>
	{
		[Fact]
		public void Ctor_Default()
		{
			var fieldReference = new CodeFieldReferenceExpression();
			Assert.Null(fieldReference.TargetObject);
			Assert.Empty(fieldReference.FieldName);
		}

		public static IEnumerable<object[]> Ctor_TestData()
		{
			yield return new object[] { null, null };
			yield return new object[] { new CodePrimitiveExpression(""), "" };
			yield return new object[] { new CodePrimitiveExpression("Value"), "FieldName" };
		}

		[Theory]
		[MemberData(nameof(Ctor_TestData))]
		public void Ctor(CodeExpression targetObject, string fieldName)
		{
			var fieldReference = new CodeFieldReferenceExpression(targetObject, fieldName);
			Assert.Equal(targetObject, fieldReference.TargetObject);
			Assert.Equal(fieldName ?? string.Empty, fieldReference.FieldName);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("FieldName")]
		public void EventName_Set_Get_ReturnsExpected(string value)
		{
			var fieldReference = new CodeFieldReferenceExpression();
			fieldReference.FieldName = value;
			Assert.Equal(value ?? string.Empty, fieldReference.FieldName);
		}

		[Theory]
		[MemberData(nameof(CodeExpression_TestData))]
		public void TargetObject_Set_Get_ReturnsExpected(CodeExpression value)
		{
			var fieldReference = new CodeFieldReferenceExpression();
			fieldReference.TargetObject = value;
			Assert.Equal(value, fieldReference.TargetObject);
		}
	}
}
