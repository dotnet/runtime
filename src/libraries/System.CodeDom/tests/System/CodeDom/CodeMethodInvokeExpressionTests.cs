// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeMethodInvokeExpressionTests : CodeObjectTestBase<CodeMethodInvokeExpression>
	{
		[Fact]
		public void Ctor_Default()
		{
			var methodInvoke = new CodeMethodInvokeExpression();
			Assert.Null(methodInvoke.Method.TargetObject);
			Assert.Empty(methodInvoke.Method.MethodName);
			Assert.Empty(methodInvoke.Parameters);
		}

		public static IEnumerable<object[]> Ctor_CodeMethodReferenceExpression_ParamsCodeExpression_TestData()
		{
			yield return new object[] { null, new CodeExpression[0] };
			yield return new object[] { new CodeMethodReferenceExpression(), new CodeExpression[0] };
			yield return new object[] { new CodeMethodReferenceExpression(new CodePrimitiveExpression("Value"), "Length"), new CodeExpression[] { new CodePrimitiveExpression("Value") } };
		}

		[Theory]
		[MemberData(nameof(Ctor_CodeMethodReferenceExpression_ParamsCodeExpression_TestData))]
		public void Ctor(CodeMethodReferenceExpression method, CodeExpression[] parameters)
		{
			var methodInvoke = new CodeMethodInvokeExpression(method, parameters);
			Assert.Equal((method ?? new CodeMethodReferenceExpression()).TargetObject, methodInvoke.Method.TargetObject);
			Assert.Equal((method ?? new CodeMethodReferenceExpression()).MethodName, methodInvoke.Method.MethodName);
			Assert.Equal(parameters, methodInvoke.Parameters.Cast<CodeExpression>());
		}

		public static IEnumerable<object[]> Ctor_CodeExpression_String_ParamsCodeExpression_TestData()
		{
			yield return new object[] { null, null, new CodeExpression[0] };
			yield return new object[] { new CodeExpression(), "", new CodeExpression[0] };
			yield return new object[] { new CodePrimitiveExpression("Value"), "Length", new CodeExpression[] { new CodePrimitiveExpression("Value") } };
		}

		[Theory]
		[MemberData(nameof(Ctor_CodeExpression_String_ParamsCodeExpression_TestData))]
		public void Ctor(CodeExpression targetObject, string methodName, CodeExpression[] parameters)
		{
			var methodInvoke = new CodeMethodInvokeExpression(targetObject, methodName, parameters);
			Assert.Equal(targetObject, methodInvoke.Method.TargetObject);
			Assert.Equal(methodName ?? string.Empty, methodInvoke.Method.MethodName);
			Assert.Equal(parameters, methodInvoke.Parameters.Cast<CodeExpression>());
		}

		[Fact]
		public void Ctor_NullParameters_ThrowsArgumentNullException()
		{
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(), null));
		}

		[Fact]
		public void Ctor_NullObjectInParameters_ThrowsArgumentNullException()
		{
			CodeExpression[] parameters = new CodeExpression[] { null };
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(), parameters));
		}

		public static IEnumerable<object[]> CodeMethodReferenceExpression_TestData()
		{
			yield return new object[] { null };
			yield return new object[] { new CodeMethodReferenceExpression() };
			yield return new object[] { new CodeMethodReferenceExpression(new CodePrimitiveExpression("Value"), "Length") };
		}

		[Theory]
		[MemberData(nameof(CodeMethodReferenceExpression_TestData))]
		public void Method_Set_Get_ReturnsExpected(CodeMethodReferenceExpression value)
		{
			var methodInvoke = new CodeMethodInvokeExpression();
			methodInvoke.Method = value;
			Assert.Equal(value?.MethodName ?? string.Empty, methodInvoke.Method.MethodName);
			Assert.Equal(value?.TargetObject, methodInvoke.Method.TargetObject);
		}

		[Fact]
		public void Parameters_AddMultiple_ReturnsExpected()
		{
			var methodInvoke = new CodeMethodInvokeExpression();

			CodeExpression expression1 = new CodePrimitiveExpression("Value1");
			methodInvoke.Parameters.Add(expression1);
			Assert.Equal(new CodeExpression[] { expression1 }, methodInvoke.Parameters.Cast<CodeExpression>());

			CodeExpression expression2 = new CodePrimitiveExpression("Value2");
			methodInvoke.Parameters.Add(expression2);
			Assert.Equal(new CodeExpression[] { expression1, expression2 }, methodInvoke.Parameters.Cast<CodeExpression>());
		}
	}
}
