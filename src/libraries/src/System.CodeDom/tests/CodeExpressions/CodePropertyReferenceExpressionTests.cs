// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodePropertyReferenceExpressionTests : CodeObjectTestBase<CodePropertyReferenceExpression>
	{
		[Fact]
		public void Ctor_Default()
		{
			var propertyReference = new CodePropertyReferenceExpression();
			Assert.Null(propertyReference.TargetObject);
			Assert.Empty(propertyReference.PropertyName);
		}

		public static IEnumerable<object[]> Ctor_TestData()
		{
			yield return new object[] { null, null };
			yield return new object[] { new CodePrimitiveExpression(""), "" };
			yield return new object[] { new CodePrimitiveExpression("Value"), "PropertyName" };
		}

		[Theory]
		[MemberData(nameof(Ctor_TestData))]
		public void Ctor(CodeExpression targetObject, string PropertyName)
		{
			var propertyReference = new CodePropertyReferenceExpression(targetObject, PropertyName);
			Assert.Equal(targetObject, propertyReference.TargetObject);
			Assert.Equal(PropertyName ?? string.Empty, propertyReference.PropertyName);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("PropertyName")]
		public void EventName_Set_Get_ReturnsExpected(string value)
		{
			var propertyReference = new CodePropertyReferenceExpression();
			propertyReference.PropertyName = value;
			Assert.Equal(value ?? string.Empty, propertyReference.PropertyName);
		}

		[Theory]
		[MemberData(nameof(CodeExpression_TestData))]
		public void TargetObject_Set_Get_ReturnsExpected(CodeExpression value)
		{
			var propertyReference = new CodePropertyReferenceExpression();
			propertyReference.TargetObject = value;
			Assert.Equal(value, propertyReference.TargetObject);
		}
	}
}
