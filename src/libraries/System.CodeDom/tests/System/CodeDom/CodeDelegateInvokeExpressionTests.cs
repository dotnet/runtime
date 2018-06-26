// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeDelegateInvokeExpressionTests : CodeObjectTestBase<CodeDelegateInvokeExpression>
	{
		[Fact]
		public void Ctor_Default()
		{
			var delegateInvoke = new CodeDelegateInvokeExpression();
			Assert.Null(delegateInvoke.TargetObject);

			Assert.Empty(delegateInvoke.Parameters);
		}

		[Theory]
		[MemberData(nameof(CodeExpression_TestData))]
		public void Ctor_CodeExpression(CodeExpression targetObject)
		{
			var delegateInvoke = new CodeDelegateInvokeExpression(targetObject);
			Assert.Equal(targetObject, delegateInvoke.TargetObject);
			Assert.Empty(delegateInvoke.Parameters);
		}

		public static IEnumerable<object[]> Ctor_CodeExpression_ParamsCodeExpression_TestData()
		{
			yield return new object[] { null, new CodeExpression[0] };
			yield return new object[] { new CodePrimitiveExpression(""), new CodeExpression[] { new CodePrimitiveExpression() } };
			yield return new object[] { new CodePrimitiveExpression("Hello"), new CodeExpression[] { new CodePrimitiveExpression("Value1") } };
		}

		[Theory]
		[MemberData(nameof(Ctor_CodeExpression_ParamsCodeExpression_TestData))]
		public void Ctor_CodeExpression_ParamsCodeExpression(CodeExpression targetObject, CodeExpression[] parameters)
		{
			var delegateInvoke = new CodeDelegateInvokeExpression(targetObject, parameters);
			Assert.Equal(targetObject, delegateInvoke.TargetObject);
			Assert.Equal(parameters, delegateInvoke.Parameters.Cast<CodeExpression>());
		}

		[Fact]
		public void Ctor_NullParameters_ThrowsArgumentNullException()
		{
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeDelegateInvokeExpression(new CodePrimitiveExpression("Hello"), null));
		}

		[Fact]
		public void Ctor_NullObjectInParameters_ThrowsArgumentNullException()
		{
			CodeExpression[] parameters = new CodeExpression[] { null };
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeDelegateInvokeExpression(new CodePrimitiveExpression("Hello"), parameters));
		}

		[Theory]
		[MemberData(nameof(CodeExpression_TestData))]
		public void TargetObject_Set_Get_ReturnsExpected(CodeExpression value)
		{
			var delegateInvoke = new CodeDelegateInvokeExpression();
			delegateInvoke.TargetObject = value;
			Assert.Same(value, delegateInvoke.TargetObject);
		}

		[Fact]
		public void Parameters_AddMultiple_ReturnsExpected()
		{
			var delegateInvoke = new CodeDelegateInvokeExpression();

			CodeParameterDeclarationExpression parameter1 = new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(int)), "name1");
			delegateInvoke.Parameters.Add(parameter1);
			Assert.Equal(new CodeParameterDeclarationExpression[] { parameter1 }, delegateInvoke.Parameters.Cast<CodeParameterDeclarationExpression>());

			CodeParameterDeclarationExpression parameter2 = new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(int)), "name2");
			delegateInvoke.Parameters.Add(parameter2);
			Assert.Equal(new CodeParameterDeclarationExpression[] { parameter1, parameter2 }, delegateInvoke.Parameters.Cast<CodeParameterDeclarationExpression>());
		}
	}
}
