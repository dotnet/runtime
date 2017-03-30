// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeMemberPropertyTests : CodeTypeMemberTestBase<CodeMemberProperty>
	{
		[Fact]
		public void Ctor_Default()
		{
			var property = new CodeMemberProperty();
			Assert.Equal(typeof(void).FullName, property.Type.BaseType);
			Assert.Empty(property.ImplementationTypes);
			Assert.Null(property.PrivateImplementationType);

			Assert.False(property.HasGet);
			Assert.Empty(property.GetStatements);

			Assert.False(property.HasSet);
			Assert.Empty(property.SetStatements);

			Assert.Empty(property.Parameters);
		}

		[Theory]
		[MemberData(nameof(CodeTypeReference_TestData))]
		public void Type_Set_Get_ReturnsExpected(CodeTypeReference type)
		{
			var property = new CodeMemberProperty() { Type = type };
			Assert.Equal((type ?? new CodeTypeReference("")).BaseType, property.Type.BaseType);
		}

		[Theory]
		[MemberData(nameof(CodeTypeReference_TestData))]
		public void PrivateImplementationType_Set_Get_ReturnsExpected(CodeTypeReference type)
		{
			var property = new CodeMemberProperty() { PrivateImplementationType = type };
			Assert.Equal(type, property.PrivateImplementationType);
		}

		[Fact]
		public void HasGet_SetTrue_ReturnsTrue()
		{
			var property = new CodeMemberProperty() { HasGet = true };
			Assert.True(property.HasGet);
		}

		[Fact]
		public void HasGet_AddedGetStatements_ReturnsTrue()
		{
			var property = new CodeMemberProperty();

			CodeStatement statement1 = new CodeMethodReturnStatement(new CodePrimitiveExpression("value1"));
			property.GetStatements.Add(statement1);
			Assert.True(property.HasGet);
			Assert.Equal(new CodeStatement[] { statement1 }, property.GetStatements.Cast<CodeStatement>());

			CodeStatement statement2 = new CodeMethodReturnStatement(new CodePrimitiveExpression("value2"));
			property.GetStatements.Add(statement2);
			Assert.True(property.HasGet);

			Assert.Equal(new CodeStatement[] { statement1, statement2 }, property.GetStatements.Cast<CodeStatement>());
		}

		[Fact]
		public void HasGet_SetFalse_RemovesAllGetStatements()
		{
			var property = new CodeMemberProperty();
			property.HasGet = false;
			Assert.False(property.HasGet);

			property.GetStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression("value")));

			property.HasGet = false;
			Assert.False(property.HasGet);
			Assert.Empty(property.GetStatements);
		}

		[Fact]
		public void HasSet_SetTrue_ReturnsTrue()
		{
			var property = new CodeMemberProperty() { HasSet = true };
			Assert.True(property.HasSet);
		}

		[Fact]
		public void HasSet_AddedSetStatements_ReturnsTrue()
		{
			var property = new CodeMemberProperty();

			CodeStatement statement1 = new CodeMethodReturnStatement(new CodePrimitiveExpression("value1"));
			property.SetStatements.Add(statement1);
			Assert.True(property.HasSet);
			Assert.Equal(new CodeStatement[] { statement1 }, property.SetStatements.Cast<CodeStatement>());

			CodeStatement statement2 = new CodeMethodReturnStatement(new CodePrimitiveExpression("value2"));
			property.SetStatements.Add(statement2);
			Assert.True(property.HasSet);

			Assert.Equal(new CodeStatement[] { statement1, statement2 }, property.SetStatements.Cast<CodeStatement>());
		}

		[Fact]
		public void HasSet_SetFalse_RemovesAllSetStatements()
		{
			var property = new CodeMemberProperty();
			property.HasSet = false;
			Assert.False(property.HasSet);

			property.SetStatements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression("value")));
			property.HasSet = false;
			Assert.False(property.HasSet);
			Assert.Empty(property.SetStatements);
		}

		[Fact]
		public void Parameters_AddMultiple_ReturnsExpected()
		{
			var property = new CodeMemberProperty();

			CodeParameterDeclarationExpression parameter1 = new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(int)), "name1");
			property.Parameters.Add(parameter1);
			Assert.Equal(new CodeParameterDeclarationExpression[] { parameter1 }, property.Parameters.Cast<CodeParameterDeclarationExpression>());

			CodeParameterDeclarationExpression parameter2 = new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(int)), "name2");
			property.Parameters.Add(parameter2);
			Assert.Equal(new CodeParameterDeclarationExpression[] { parameter1, parameter2 }, property.Parameters.Cast<CodeParameterDeclarationExpression>());
		}

		[Fact]
		public void ImplementationTypes_AddMultiple_ReturnsExpected()
		{
			var property = new CodeMemberProperty();

			CodeTypeReference type1 = new CodeTypeReference(typeof(int));
			property.ImplementationTypes.Add(type1);
			Assert.Equal(new CodeTypeReference[] { type1 }, property.ImplementationTypes.Cast<CodeTypeReference>());

			CodeTypeReference type2 = new CodeTypeReference(typeof(int));
			property.ImplementationTypes.Add(type2);
			Assert.Equal(new CodeTypeReference[] { type1, type2 }, property.ImplementationTypes.Cast<CodeTypeReference>());
		}
	}
}
