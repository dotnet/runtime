// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeVariableDeclarationStatementTests : CodeObjectTestBase<CodeVariableDeclarationStatement>
	{
		[Fact]
		public void Ctor_Default()
		{
			var variableDeclaration = new CodeVariableDeclarationStatement();
			Assert.Null(variableDeclaration.InitExpression);
			Assert.Empty(variableDeclaration.Name);
			Assert.Equal(typeof(void).FullName, variableDeclaration.Type.BaseType);
		}

		public static IEnumerable<object[]> Ctor_CodeTypeReference_String_TestData()
		{
			yield return new object[] { null, null };
			yield return new object[] { new CodeTypeReference(), "" };
			yield return new object[] { new CodeTypeReference(typeof(int)), "Name" };
		}

		[Theory]
		[MemberData(nameof(Ctor_CodeTypeReference_String_TestData))]
		public void Ctor_CodeTypeReference_String(CodeTypeReference type, string name)
		{
			var variableDeclaration = new CodeVariableDeclarationStatement(type, name);
			Assert.Equal((type ?? new CodeTypeReference("")).BaseType, variableDeclaration.Type.BaseType);
			Assert.Equal(name ?? string.Empty, variableDeclaration.Name);
			Assert.Null(variableDeclaration.InitExpression);
		}

		public static IEnumerable<object[]> Ctor_String_String_TestData()
		{
			yield return new object[] { null, null, "System.Void" };
			yield return new object[] { "", "", "System.Void" };
			yield return new object[] { "Int32", "Name", "Int32" };
		}

		[Theory]
		[MemberData(nameof(Ctor_String_String_TestData))]
		public void Ctor_String_String(string type, string name, string expectedBaseType)
		{
			var variableDeclaration = new CodeVariableDeclarationStatement(type, name);
			Assert.Equal(expectedBaseType, variableDeclaration.Type.BaseType);
			Assert.Equal(name ?? string.Empty, variableDeclaration.Name);
			Assert.Null(variableDeclaration.InitExpression);
		}

		public static IEnumerable<object[]> Ctor_Type_String_TestData()
		{
			yield return new object[] { typeof(int), null, "System.Int32" };
			yield return new object[] { typeof(List<>), "", "System.Collections.Generic.List`1" };
			yield return new object[] { typeof(void), "Name", "System.Void" };
		}

		[Theory]
		[MemberData(nameof(Ctor_Type_String_TestData))]
		public void Ctor_Type_String(Type type, string name, string expectedBaseType)
		{
			var variableDeclaration = new CodeVariableDeclarationStatement(type, name);
			Assert.Equal(expectedBaseType, variableDeclaration.Type.BaseType);
			Assert.Equal(name ?? string.Empty, variableDeclaration.Name);
			Assert.Null(variableDeclaration.InitExpression);
		}

		public static IEnumerable<object[]> Ctor_CodeTypeReference_String_CodeExpression_TestData()
		{
			yield return new object[] { null, null, null };
			yield return new object[] { new CodeTypeReference(), "", new CodeExpression() };
			yield return new object[] { new CodeTypeReference(typeof(int)), "Name", new CodePrimitiveExpression("Value") };
		}

		[Theory]
		[MemberData(nameof(Ctor_CodeTypeReference_String_CodeExpression_TestData))]
		public void Ctor_CodeTypeReference_String_CodeExpression(CodeTypeReference type, string name, CodeExpression initExpression)
		{
			var variableDeclaration = new CodeVariableDeclarationStatement(type, name, initExpression);
			Assert.Equal((type ?? new CodeTypeReference("")).BaseType, variableDeclaration.Type.BaseType);
			Assert.Equal(name ?? string.Empty, variableDeclaration.Name);
			Assert.Equal(initExpression, variableDeclaration.InitExpression);
		}

		public static IEnumerable<object[]> Ctor_String_String_CodeExpression_TestData()
		{
			yield return new object[] { null, null, null, "System.Void" };
			yield return new object[] { "", "", new CodeExpression(), "System.Void" };
			yield return new object[] { "Int32", "Name", new CodePrimitiveExpression("Value"), "Int32" };
		}

		[Theory]
		[MemberData(nameof(Ctor_String_String_CodeExpression_TestData))]
		public void Ctor_String_String_CodeExpression(string type, string name, CodeExpression initExpression, string expectedBaseType)
		{
			var variableDeclaration = new CodeVariableDeclarationStatement(type, name, initExpression);
			Assert.Equal(expectedBaseType, variableDeclaration.Type.BaseType);
			Assert.Equal(name ?? string.Empty, variableDeclaration.Name);
			Assert.Equal(initExpression, variableDeclaration.InitExpression);
		}

		public static IEnumerable<object[]> Ctor_Type_String_CodeExpression_TestData()
		{
			yield return new object[] { typeof(int), null, null, "System.Int32" };
			yield return new object[] { typeof(List<>), "", new CodeExpression(), "System.Collections.Generic.List`1" };
			yield return new object[] { typeof(void), "Name", new CodePrimitiveExpression("Value"), "System.Void" };
		}

		[Theory]
		[MemberData(nameof(Ctor_Type_String_CodeExpression_TestData))]
		public void Ctor_Type_String_CodeExpression(Type type, string name, CodeExpression initExpression, string expectedBaseType)
		{
			var variableDeclaration = new CodeVariableDeclarationStatement(type, name, initExpression);
			Assert.Equal(expectedBaseType, variableDeclaration.Type.BaseType);
			Assert.Equal(name ?? string.Empty, variableDeclaration.Name);
			Assert.Equal(initExpression, variableDeclaration.InitExpression);
		}

		[Fact]
		public void Ctor_NullType_ThrowsArgumentNullException()
		{
			AssertExtensions.Throws<ArgumentNullException>("type", () => new CodeVariableDeclarationStatement((Type)null, "Name"));
			AssertExtensions.Throws<ArgumentNullException>("type", () => new CodeVariableDeclarationStatement((Type)null, "Name", new CodePrimitiveExpression("Value")));
		}

		[Theory]
		[MemberData(nameof(CodeExpression_TestData))]
		public void InitExpression_Set_Get_ReturnsExpected(CodeExpression value)
		{
			var variableDeclaration = new CodeVariableDeclarationStatement();
			variableDeclaration.InitExpression = value;
			Assert.Equal(value, variableDeclaration.InitExpression);
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void Name_Set_Get_ReturnsExpected(string value)
		{
			var variableDeclaration = new CodeVariableDeclarationStatement();
			variableDeclaration.Name = value;
			Assert.Equal(value ?? string.Empty, variableDeclaration.Name);
		}

		[Theory]
		[MemberData(nameof(CodeTypeReference_TestData))]
		public void Type_Set_Get_ReturnsExpected(CodeTypeReference value)
		{
			var variableDeclaration = new CodeVariableDeclarationStatement();
			variableDeclaration.Type = value;
			Assert.Equal((value ?? new CodeTypeReference("")).BaseType, variableDeclaration.Type.BaseType);
		}
	}
}
