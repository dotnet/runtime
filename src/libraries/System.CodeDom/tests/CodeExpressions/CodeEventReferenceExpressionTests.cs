// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeEventReferenceExpressionTests : CodeObjectTestBase<CodeEventReferenceExpression>
	{
		[Fact]
		public void Ctor_Default()
		{
			var eventReference = new CodeEventReferenceExpression();
			Assert.Null(eventReference.TargetObject);
			Assert.Empty(eventReference.EventName);
		}

		public static IEnumerable<object[]> Ctor_TestData()
		{
			yield return new object[] { null, null };
			yield return new object[] { new CodePrimitiveExpression(""), "" };
			yield return new object[] { new CodePrimitiveExpression("Value"), "EventName" };
		}

		[Theory]
		[MemberData(nameof(Ctor_TestData))]
		public void Ctor(CodeExpression targetObject, string eventName)
		{
			var eventReference = new CodeEventReferenceExpression(targetObject, eventName);
			Assert.Equal(targetObject, eventReference.TargetObject);
			Assert.Equal(eventName ?? string.Empty, eventReference.EventName);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("EventName")]
		public void EventName_Set_Get_ReturnsExpected(string value)
		{
			var eventReference = new CodeEventReferenceExpression();
			eventReference.EventName = value;
			Assert.Equal(value ?? string.Empty, eventReference.EventName);
		}

		[Theory]
		[MemberData(nameof(CodeExpression_TestData))]
		public void TargetObject_Set_Get_ReturnsExpected(CodeExpression value)
		{
			var eventReference = new CodeEventReferenceExpression();
			eventReference.TargetObject = value;
			Assert.Equal(value, eventReference.TargetObject);
		}
	}
}
