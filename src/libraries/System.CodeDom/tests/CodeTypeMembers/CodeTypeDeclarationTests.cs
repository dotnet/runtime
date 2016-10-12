// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeTypeDeclarationTests : CodeTypeMemberTestBase<CodeTypeDeclaration>
	{
		[Fact]
		public void Ctor_Default()
		{
			var declaration = new CodeTypeDeclaration();
			Assert.Empty(declaration.Name);
			Assert.True(declaration.IsClass);
			Assert.False(declaration.IsStruct);
			Assert.False(declaration.IsEnum);
			Assert.False(declaration.IsInterface);
			Assert.False(declaration.IsPartial);
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void Ctor_String(string name)
		{
			var declaration = new CodeTypeDeclaration(name);
			Assert.Equal(name ?? string.Empty, declaration.Name);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void IsClass_Set_Get_ReturnsTrue(bool value)
		{
			var declaration = new CodeTypeDeclaration();
			declaration.IsClass = value;
			Assert.True(declaration.IsClass);
			Assert.False(declaration.IsStruct);
			Assert.False(declaration.IsEnum);
			Assert.False(declaration.IsInterface);
		}

		[Fact]
		public void IsClass_SetTrue_WasStruct_ReturnsTrue()
		{
			var declaration = new CodeTypeDeclaration() { IsStruct = true };
			declaration.IsClass = true;
			Assert.False(declaration.IsStruct);
			Assert.False(declaration.IsEnum);
			Assert.False(declaration.IsInterface);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void IsStruct_Set_Get_ReturnsExpected(bool value)
		{
			var declaration = new CodeTypeDeclaration();
			declaration.IsStruct = value;
			Assert.Equal(value, declaration.IsStruct);
			Assert.Equal(!value, declaration.IsClass);
			Assert.False(declaration.IsEnum);
			Assert.False(declaration.IsInterface);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void IsEnum_Set_Get_ReturnsExpected(bool value)
		{
			var declaration = new CodeTypeDeclaration();
			declaration.IsEnum = value;
			Assert.Equal(value, declaration.IsEnum);
			Assert.Equal(!value, declaration.IsClass);
			Assert.False(declaration.IsStruct);
			Assert.False(declaration.IsInterface);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void IsInterface_Set_Get_ReturnsExpected(bool value)
		{
			var declaration = new CodeTypeDeclaration();
			declaration.IsInterface = value;
			Assert.Equal(value, declaration.IsInterface);
			Assert.Equal(!value, declaration.IsClass);
			Assert.False(declaration.IsStruct);
			Assert.False(declaration.IsEnum);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		public void IsPartial_Set_Get_ReturnsExpected(bool value)
		{
			var declaration = new CodeTypeDeclaration();
			declaration.IsPartial = value;
			Assert.Equal(value, declaration.IsPartial);
		}

		[Theory]
		[InlineData(TypeAttributes.Class - 1)]
		[InlineData(TypeAttributes.Class)]
		[InlineData(TypeAttributes.NotPublic | TypeAttributes.Public)]
		[InlineData((TypeAttributes)int.MaxValue)]
		public void TypeAttributes_Set_Get_ReturnExpected(TypeAttributes value)
		{
			var declaration = new CodeTypeDeclaration();
			declaration.TypeAttributes = value;
			Assert.Equal(value, declaration.TypeAttributes);
		}

		[Fact]
		public void BaseTypes_AddMultiple_ReturnsExpected()
		{
			var declaration = new CodeTypeDeclaration();

			CodeTypeReference type1 = new CodeTypeReference(typeof(int));
			declaration.BaseTypes.Add(type1);
			Assert.Equal(new CodeTypeReference[] { type1 }, declaration.BaseTypes.Cast<CodeTypeReference>());

			CodeTypeReference type2 = new CodeTypeReference(typeof(char));
			declaration.BaseTypes.Add(type2);
			Assert.Equal(new CodeTypeReference[] { type1, type2 }, declaration.BaseTypes.Cast<CodeTypeReference>());
		}

		[Fact]
		public void BaseTypes_Get_CallsPopulateBaseTypesOnce()
		{
			var declaration = new CodeTypeDeclaration();
			bool calledPopulateBaseTypes = false;
			declaration.PopulateBaseTypes += (object sender, EventArgs args) =>
			{
				calledPopulateBaseTypes = true;
				Assert.Same(declaration, sender);
				Assert.Equal(EventArgs.Empty, args);
			};

			declaration.BaseTypes.Add(new CodeTypeReference(typeof(int)));
			Assert.True(calledPopulateBaseTypes);

			// Only calls PopulateBaseTypes once
			calledPopulateBaseTypes = false;
			declaration.BaseTypes.Add(new CodeTypeReference(typeof(char)));
			Assert.False(calledPopulateBaseTypes);
		}

		[Fact]
		public void Members_AddMultiple_ReturnsExpected()
		{
			var declaration = new CodeTypeDeclaration();

			CodeTypeMember member1 = new CodeMemberField("Type", "Name1");
			declaration.Members.Add(member1);
			Assert.Equal(new CodeTypeMember[] { member1 }, declaration.Members.Cast<CodeTypeMember>());

			CodeTypeMember member2 = new CodeMemberField("Type", "Name1");
			declaration.Members.Add(member2);
			Assert.Equal(new CodeTypeMember[] { member1, member2 }, declaration.Members.Cast<CodeTypeMember>());
		}

		[Fact]
		public void Members_Get_CallsPopulateMembersOnce()
		{
			var declaration = new CodeTypeDeclaration();
			bool calledPopulateMembers = false;
			declaration.PopulateMembers += (object sender, EventArgs args) =>
			{
				calledPopulateMembers = true;
				Assert.Same(declaration, sender);
				Assert.Equal(EventArgs.Empty, args);
			};

			declaration.Members.Add(new CodeMemberField("Type", "Name1"));
			Assert.True(calledPopulateMembers);

			// Only calls PopulateMembers once
			calledPopulateMembers = false;
			declaration.Members.Add(new CodeMemberField("Type", "Name2"));
			Assert.False(calledPopulateMembers);
		}

		[Fact]
		public void TypeParameters_AddMultiple_ReturnsExpected()
		{
			var declaration = new CodeTypeDeclaration();

			CodeTypeParameter parameter1 = new CodeTypeParameter("Name1");
			declaration.TypeParameters.Add(parameter1);
			Assert.Equal(new CodeTypeParameter[] { parameter1 }, declaration.TypeParameters.Cast<CodeTypeParameter>());

			CodeTypeParameter parameter2 = new CodeTypeParameter("Name2");
			declaration.TypeParameters.Add(parameter2);
			Assert.Equal(new CodeTypeParameter[] { parameter1, parameter2 }, declaration.TypeParameters.Cast<CodeTypeParameter>());
		}
	}
}
