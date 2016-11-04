// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeRemoveEventStatementTests : CodeStatementTestBase<CodeRemoveEventStatement>
	{
		[Fact]
		public void Ctor_Default()
		{
			var removeEvent = new CodeRemoveEventStatement();
			Assert.Empty(removeEvent.Event.EventName);
			Assert.Null(removeEvent.Event.TargetObject);
			Assert.Null(removeEvent.Listener);
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
			var removeEvent = new CodeRemoveEventStatement(targetObject, eventName, listener);
			Assert.Equal(targetObject, removeEvent.Event.TargetObject);
			Assert.Equal(eventName ?? string.Empty, removeEvent.Event.EventName);
			Assert.Equal(listener, removeEvent.Listener);
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
			var removeEvent = new CodeRemoveEventStatement(eventExpresion, listener);
			Assert.Equal((eventExpresion ?? new CodeEventReferenceExpression()).TargetObject, removeEvent.Event.TargetObject);
			Assert.Equal((eventExpresion ?? new CodeEventReferenceExpression()).EventName, removeEvent.Event.EventName);
			Assert.Equal(listener, removeEvent.Listener);
		}

		[Theory]
		[MemberData(nameof(CodeEventReferenceExpression_CodeExpression_TestData))]
		public void Event_Set_Get_ReturnsExpected(CodeEventReferenceExpression value, CodeExpression _)
		{
			var removeEvent = new CodeRemoveEventStatement();
			removeEvent.Event = value;
			Assert.Equal((value ?? new CodeEventReferenceExpression()).TargetObject, removeEvent.Event.TargetObject);
			Assert.Equal((value ?? new CodeEventReferenceExpression()).EventName, removeEvent.Event.EventName);
		}

		[Theory]
		[MemberData(nameof(CodeExpression_TestData))]
		public void Listener_Set_Get_ReturnsExpected(CodeExpression value)
		{
			var removeEvent = new CodeRemoveEventStatement();
			removeEvent.Listener = value;
			Assert.Equal(value, removeEvent.Listener);
		}
	}
}
