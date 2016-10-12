// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeParameterDeclarationExpressionTests : CodeObjectTestBase<CodeParameterDeclarationExpression>
	{
		[Fact]
		public void Ctor_Default()
		{
			var parameter = new CodeParameterDeclarationExpression();
			Assert.Empty(parameter.CustomAttributes);
			Assert.Equal(FieldDirection.In, parameter.Direction);
			Assert.Equal(typeof(void).FullName, parameter.Type.BaseType);
			Assert.Empty(parameter.Name);
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
			var parameter = new CodeParameterDeclarationExpression(type, name);
			Assert.Empty(parameter.CustomAttributes);
			Assert.Equal(FieldDirection.In, parameter.Direction);
			Assert.Equal((type ?? new CodeTypeReference("")).BaseType, parameter.Type.BaseType);
			Assert.Equal(name ?? string.Empty, parameter.Name);
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
			var parameter = new CodeParameterDeclarationExpression(type, name);
			Assert.Empty(parameter.CustomAttributes);
			Assert.Equal(FieldDirection.In, parameter.Direction);
			Assert.Equal(expectedBaseType, parameter.Type.BaseType);
			Assert.Equal(name ?? string.Empty, parameter.Name);
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
			var parameter = new CodeParameterDeclarationExpression(type, name);
			Assert.Empty(parameter.CustomAttributes);
			Assert.Equal(FieldDirection.In, parameter.Direction);
			Assert.Equal(expectedBaseType, parameter.Type.BaseType);
			Assert.Equal(name ?? string.Empty, parameter.Name);
		}

		[Fact]
		public void CustomAttributes_SetNull_Get_ReturnsEmpty()
		{
			var parameter = new CodeParameterDeclarationExpression() { CustomAttributes = null };
			Assert.Empty(parameter.CustomAttributes);
		}

		[Fact]
		public void CustomAttributes_SetNonNull_Get_ReturnsExpected()
		{
			var parameter = new CodeParameterDeclarationExpression() { CustomAttributes = null };
			CodeAttributeDeclarationCollection value = new CodeAttributeDeclarationCollection();
			value.Add(new CodeAttributeDeclaration("Name1"));
			value.Add(new CodeAttributeDeclaration("Name2"));

			parameter.CustomAttributes = value;
			Assert.Equal(value, parameter.CustomAttributes);
		}

		[Fact]
		public void CustomAttributes_AddMultiple_ReturnsExpected()
		{
			var parameter = new CodeParameterDeclarationExpression();

			CodeAttributeDeclaration attribute1 = new CodeAttributeDeclaration("Name1");
			parameter.CustomAttributes.Add(attribute1);
			Assert.Equal(new CodeAttributeDeclaration[] { attribute1 }, parameter.CustomAttributes.Cast<CodeAttributeDeclaration>());

			CodeAttributeDeclaration attribute2 = new CodeAttributeDeclaration("Name2");
			parameter.CustomAttributes.Add(attribute2);
			Assert.Equal(new CodeAttributeDeclaration[] { attribute1, attribute2 }, parameter.CustomAttributes.Cast<CodeAttributeDeclaration>());
		}

		[Theory]
		[InlineData(FieldDirection.In - 1)]
		[InlineData(FieldDirection.In)]
		[InlineData(FieldDirection.In | FieldDirection.Out)]
		[InlineData(FieldDirection.Ref + 1)]
		public void Direction_Set_Get_ReturnsExpected(FieldDirection value)
		{
			var parameter = new CodeParameterDeclarationExpression();
			parameter.Direction = value;
			Assert.Equal(value, parameter.Direction);
		}

		[Theory]
		[MemberData(nameof(CodeTypeReference_TestData))]
		public void Type_Set_Get_ReturnsExpected(CodeTypeReference value)
		{
			var parameter = new CodeParameterDeclarationExpression();
			parameter.Type = value;
			Assert.Equal((value ?? new CodeTypeReference("")).BaseType, parameter.Type.BaseType);
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void Name_Set_Get_ReturnsExpected(string value)
		{
			var parameter = new CodeParameterDeclarationExpression();
			parameter.Name = value;
			Assert.Equal(value ?? string.Empty, parameter.Name);
		}
	}
}
