// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeAttachEventStatementTests : CodeStatementTestBase<CodeAttachEventStatement>
	{
		[Fact]
		public void Ctor_Default()
		{
			var attachEvent = new CodeAttachEventStatement();
			Assert.Empty(attachEvent.Event.EventName);
			Assert.Null(attachEvent.Event.TargetObject);
			Assert.Null(attachEvent.Listener);
		}

		public static IEnumerable<object[]> Ctor_CodeExpression_String_CodeExpression_TestData()
		{
			yield return new object[] { null, null, null };
			yield return new object[] { new CodePrimitiveExpression("Value1"), "EventName", new CodePrimitiveExpression("Value2") };
		}

		[Theory]
		[MemberData(nameof(Ctor_CodeExpression_String_CodeExpression_TestData))]
		public void Ctor_CodeExpression_String_CodeExpression(CodeExpression targetObject, string eventName, CodeExpression listener)
		{
			var attachEvent = new CodeAttachEventStatement(targetObject, eventName, listener);
			Assert.Equal(targetObject, attachEvent.Event.TargetObject);
			Assert.Equal(eventName ?? string.Empty, attachEvent.Event.EventName);
			Assert.Equal(listener, attachEvent.Listener);
		}

		public static IEnumerable<object[]> CodeEventReferenceExpression_CodeExpression_TestData()
		{
			yield return new object[] { null, null };
			yield return new object[] { new CodeEventReferenceExpression(null, null), null };
			yield return new object[] { new CodeEventReferenceExpression(new CodePrimitiveExpression("Value1"), "EventName"), new CodePrimitiveExpression("Value2") };
		}

		[Theory]
		[MemberData(nameof(CodeEventReferenceExpression_CodeExpression_TestData))]
		public void Ctor_CodeEventReferenceExpression_CodeExpression(CodeEventReferenceExpression eventExpresion, CodeExpression listener)
		{
			var attachEvent = new CodeAttachEventStatement(eventExpresion, listener);
			Assert.Equal((eventExpresion ?? new CodeEventReferenceExpression()).TargetObject, attachEvent.Event.TargetObject);
			Assert.Equal((eventExpresion ?? new CodeEventReferenceExpression()).EventName, attachEvent.Event.EventName);
			Assert.Equal(listener, attachEvent.Listener);
		}

		[Theory]
		[MemberData(nameof(CodeEventReferenceExpression_CodeExpression_TestData))]
		public void Event_Set_Get_ReturnsExpected(CodeEventReferenceExpression value, CodeExpression _)
		{
			var attachEvent = new CodeAttachEventStatement();
			attachEvent.Event = value;
			Assert.Equal((value ?? new CodeEventReferenceExpression()).TargetObject, attachEvent.Event.TargetObject);
			Assert.Equal((value ?? new CodeEventReferenceExpression()).EventName, attachEvent.Event.EventName);
		}

		[Theory]
		[MemberData(nameof(CodeExpression_TestData))]
		public void Listener_Set_Get_ReturnsExpected(CodeExpression value)
		{
			var attachEvent = new CodeAttachEventStatement();
			attachEvent.Listener = value;
			Assert.Equal(value, attachEvent.Listener);
		}
	}
}
