// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeMethodReferenceExpressionTests : CodeObjectTestBase<CodeMethodReferenceExpression>
	{
		[Fact]
		public void Ctor_Default()
		{
			var methodReference = new CodeMethodReferenceExpression();
			Assert.Empty(methodReference.MethodName);
			Assert.Null(methodReference.TargetObject);
			Assert.Empty(methodReference.TypeArguments);
		}

		public static IEnumerable<object[]> Ctor_CodeExpression_String_TestData()
		{
			yield return new object[] { null, null };
			yield return new object[] { new CodePrimitiveExpression(), "" };
			yield return new object[] { new CodePrimitiveExpression("Value"), "Length" };
		}

		[Theory]
		[MemberData(nameof(Ctor_CodeExpression_String_TestData))]
		public void Ctor(CodeExpression targetObject, string methodName)
		{
			var methodReference = new CodeMethodReferenceExpression(targetObject, methodName);
			Assert.Equal(targetObject, methodReference.TargetObject);
			Assert.Equal(methodName ?? string.Empty, methodReference.MethodName);
		}

		public static IEnumerable<object[]> Ctor_CodeExpression_String_ParamsCodeExpression_TestData()
		{
			yield return new object[] { null, null, null };
			yield return new object[] { new CodePrimitiveExpression(), "", new CodeTypeReference[0] };
			yield return new object[] { new CodePrimitiveExpression("Value"), "Length", new CodeTypeReference[] { new CodeTypeReference(typeof(void)) } };
		}

		[Theory]
		[MemberData(nameof(Ctor_CodeExpression_String_ParamsCodeExpression_TestData))]
		public void Ctor(CodeExpression targetObject, string methodName, CodeTypeReference[] typeArguments)
		{
			var methodReference = new CodeMethodReferenceExpression(targetObject, methodName, typeArguments);
			Assert.Equal(targetObject, methodReference.TargetObject);
			Assert.Equal(methodName ?? string.Empty, methodReference.MethodName);
			Assert.Equal(typeArguments ?? new CodeTypeReference[0], methodReference.TypeArguments.Cast<CodeTypeReference>());
		}

		[Fact]
		public void Ctor_NullObjectInParameters_ThrowsArgumentNullException()
		{
			CodeTypeReference[] parameters = new CodeTypeReference[] { null };
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeMethodReferenceExpression(new CodePrimitiveExpression(), "", parameters));
		}

		[Theory]
		[MemberData(nameof(CodeExpression_TestData))]
		public void TargetObject_Set_Get_ReturnsExpected(CodeExpression value)
		{
			var methodReference = new CodeMethodReferenceExpression();
			methodReference.TargetObject = value;
			Assert.Equal(value, methodReference.TargetObject);
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void MethodName_Set_Get_ReturnsExpected(string value)
		{
			var methodReference = new CodeMethodReferenceExpression();
			methodReference.MethodName = value;
			Assert.Equal(value ?? string.Empty, methodReference.MethodName);
		}

		[Fact]
		public void TypeArguments_AddMultiple_ReturnsExpected()
		{
			var methodReference = new CodeMethodReferenceExpression();

			CodeTypeReference type1 = new CodeTypeReference(typeof(int));
			methodReference.TypeArguments.Add(type1);
			Assert.Equal(new CodeTypeReference[] { type1 }, methodReference.TypeArguments.Cast<CodeTypeReference>());

			CodeTypeReference type2 = new CodeTypeReference(typeof(char));
			methodReference.TypeArguments.Add(type2);
			Assert.Equal(new CodeTypeReference[] { type1, type2 }, methodReference.TypeArguments.Cast<CodeTypeReference>());
		}
	}
}
