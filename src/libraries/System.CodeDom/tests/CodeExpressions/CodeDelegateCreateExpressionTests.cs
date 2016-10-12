// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeDelegateCreateExpressionTests : CodeObjectTestBase<CodeDelegateCreateExpression>
	{
		[Fact]
		public void Ctor_Default()
		{
			var delegateCreate = new CodeDelegateCreateExpression();
			Assert.Equal(typeof(void).FullName, delegateCreate.DelegateType.BaseType);

			Assert.Null(delegateCreate.TargetObject);
			Assert.Empty(delegateCreate.MethodName);
		}

		public static IEnumerable<object[]> Ctor_TestData()
		{
			yield return new object[] { null, null, null };
			yield return new object[] { new CodeTypeReference(""), new CodePrimitiveExpression(""), "" };
			yield return new object[] { new CodeTypeReference(typeof(Delegate)), new CodePrimitiveExpression("Value"), "MethodName" };
		}

		[Theory]
		[MemberData(nameof(Ctor_TestData))]
		public void Ctor(CodeTypeReference delegateType, CodeExpression targetObject, string methodName)
		{
			var delegateCreate = new CodeDelegateCreateExpression(delegateType, targetObject, methodName);
			Assert.Equal((delegateType ?? new CodeTypeReference("")).BaseType, delegateCreate.DelegateType.BaseType);
			Assert.Equal(targetObject, delegateCreate.TargetObject);
			Assert.Equal(methodName ?? string.Empty, delegateCreate.MethodName);
		}

		[Theory]
		[MemberData(nameof(CodeTypeReference_TestData))]
		public void DelegateType_Set_Get_ReturnsExpected(CodeTypeReference value)
		{
			var delegateCreate = new CodeDelegateCreateExpression();
			delegateCreate.DelegateType = value;
			Assert.Equal((value ?? new CodeTypeReference("")).BaseType, delegateCreate.DelegateType.BaseType);
		}

		[Theory]
		[MemberData(nameof(CodeExpression_TestData))]
		public void TargetObject_Set_Get_ReturnsExpected(CodeExpression value)
		{
			var delegateCreate = new CodeDelegateCreateExpression();
			delegateCreate.TargetObject = value;
			Assert.Equal(value, delegateCreate.TargetObject);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("MethodName")]
		public void MethodName_Set_Get_ReturnsExpected(string value)
		{
			var delegateCreate = new CodeDelegateCreateExpression();
			delegateCreate.MethodName = value;
			Assert.Equal(value ?? string.Empty, delegateCreate.MethodName);
		}
	}
}
