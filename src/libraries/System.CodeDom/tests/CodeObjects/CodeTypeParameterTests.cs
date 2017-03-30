// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeTypeParameterTests : CodeObjectTestBase<CodeTypeParameter>
	{
		[Fact]
		public void Ctor_Default()
		{
			var typeParameter = new CodeTypeParameter();
			Assert.Empty(typeParameter.Name);
			Assert.Empty(typeParameter.Constraints);
			Assert.Empty(typeParameter.CustomAttributes);
			Assert.False(typeParameter.HasConstructorConstraint);
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void Ctor_String(string name)
		{
			var typeParameter = new CodeTypeParameter(name);
			Assert.Equal(name ?? string.Empty, typeParameter.Name);
			Assert.Empty(typeParameter.Constraints);
			Assert.Empty(typeParameter.CustomAttributes);
			Assert.False(typeParameter.HasConstructorConstraint);
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void Name_Set_Get_ReturnsExpected(string value)
		{
			var typeParameter = new CodeTypeParameter();
			typeParameter.Name = value;
			Assert.Equal(value ?? string.Empty, typeParameter.Name);
		}

		[Fact]
		public void Constraints_AddMultiple_ReturnsExpected()
		{
			var typeParameter = new CodeTypeParameter();

			CodeTypeReference type1 = new CodeTypeReference(typeof(int));
			typeParameter.Constraints.Add(type1);
			Assert.Equal(new CodeTypeReference[] { type1 }, typeParameter.Constraints.Cast<CodeTypeReference>());

			CodeTypeReference type2 = new CodeTypeReference(typeof(int));
			typeParameter.Constraints.Add(type2);
			Assert.Equal(new CodeTypeReference[] { type1, type2 }, typeParameter.Constraints.Cast<CodeTypeReference>());
		}

		[Fact]
		public void CustomAttributes_AddMultiple_ReturnsExpected()
		{
			var typeParameter = new CodeTypeParameter();

			CodeAttributeDeclaration attribute1 = new CodeAttributeDeclaration("Name1");
			typeParameter.CustomAttributes.Add(attribute1);
			Assert.Equal(new CodeAttributeDeclaration[] { attribute1 }, typeParameter.CustomAttributes.Cast<CodeAttributeDeclaration>());

			CodeAttributeDeclaration attribute2 = new CodeAttributeDeclaration("Name2");
			typeParameter.CustomAttributes.Add(attribute2);
			Assert.Equal(new CodeAttributeDeclaration[] { attribute1, attribute2 }, typeParameter.CustomAttributes.Cast<CodeAttributeDeclaration>());
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void HasConstructorConstraint_Get_Set_ReturnsExpected(bool value)
		{
			var typeParameter = new CodeTypeParameter();
			typeParameter.HasConstructorConstraint = value;
			Assert.Equal(value, typeParameter.HasConstructorConstraint);
		}
	}
}
