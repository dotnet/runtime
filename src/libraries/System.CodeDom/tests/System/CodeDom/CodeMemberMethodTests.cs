// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeMemberMemberTests : CodeTypeMemberTestBase<CodeMemberMethod>
	{
		[Fact]
		public void Ctor_Default()
		{
			var method = new CodeMemberMethod();
			Assert.Equal(typeof(void).FullName, method.ReturnType.BaseType);
			Assert.Empty(method.Statements);
			Assert.Empty(method.Parameters);

			Assert.Empty(method.ImplementationTypes);
			Assert.Null(method.PrivateImplementationType);

			Assert.Empty(method.ReturnTypeCustomAttributes);
			Assert.Empty(method.TypeParameters);
		}

		[Theory]
		[MemberData(nameof(CodeTypeReference_TestData))]
		public void ReturnType_Set_Get_ReturnsExpected(CodeTypeReference type)
		{
			var method = new CodeMemberMethod() { ReturnType = type };
			Assert.Equal((type ?? new CodeTypeReference("")).BaseType, method.ReturnType.BaseType);
		}

		[Fact]
		public void Statements_AddMultiple_ReturnsExpected()
		{
			var method = new CodeMemberMethod();

			CodeStatement statement1 = new CodeCommentStatement("Value1");
			method.Statements.Add(statement1);
			Assert.Equal(new CodeStatement[] { statement1 }, method.Statements.Cast<CodeStatement>());

			CodeStatement statement2 = new CodeCommentStatement("Value2");
			method.Statements.Add(statement2);
			Assert.Equal(new CodeStatement[] { statement1, statement2 }, method.Statements.Cast<CodeStatement>());
		}

		[Fact]
		public void Statements_Get_CallsPopulateStatementsOnce()
		{
			var method = new CodeMemberMethod();
			bool calledPopulateStatements = false;
			method.PopulateStatements += (object sender, EventArgs args) =>
			{
				calledPopulateStatements = true;
				Assert.Same(method, sender);
				Assert.Equal(EventArgs.Empty, args);
			};

			CodeStatement statement = new CodeMethodReturnStatement(new CodePrimitiveExpression("Value2"));
			method.Statements.Add(statement);
			Assert.Equal(new CodeStatement[] { statement }, method.Statements.Cast<CodeStatement>());
			Assert.True(calledPopulateStatements);

			// Only calls PopulateStatements once
			calledPopulateStatements = false;
			method.Statements.Add(statement);
			Assert.False(calledPopulateStatements);
		}

		[Fact]
		public void Parameters_AddMultiple_ReturnsExpected()
		{
			var method = new CodeMemberMethod();

			CodeParameterDeclarationExpression parameter1 = new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(int)), "name1");
			method.Parameters.Add(parameter1);
			Assert.Equal(new CodeParameterDeclarationExpression[] { parameter1 }, method.Parameters.Cast<CodeParameterDeclarationExpression>());

			CodeParameterDeclarationExpression parameter2 = new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(int)), "name2");
			method.Parameters.Add(parameter2);
			Assert.Equal(new CodeParameterDeclarationExpression[] { parameter1, parameter2 }, method.Parameters.Cast<CodeParameterDeclarationExpression>());
		}

		[Fact]
		public void Parameters_Get_CallsPopulateParametersOnce()
		{
			var method = new CodeMemberMethod();
			bool calledPopulateParameters = false;
			method.PopulateParameters += (object sender, EventArgs args) =>
			{
				calledPopulateParameters = true;
				Assert.Same(method, sender);
				Assert.Equal(EventArgs.Empty, args);
			};

			CodeParameterDeclarationExpression parameter = new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(int)), "name1");
			method.Parameters.Add(parameter);
			Assert.Equal(new CodeParameterDeclarationExpression[] { parameter }, method.Parameters.Cast<CodeParameterDeclarationExpression>());
			Assert.True(calledPopulateParameters);

			// Only calls PopulateParameters once
			calledPopulateParameters = false;
			method.Parameters.Add(parameter);
			Assert.False(calledPopulateParameters);
		}

		[Theory]
		[MemberData(nameof(CodeTypeReference_TestData))]
		public void PrivateImplementationType_Set_Get_ReturnsExpected(CodeTypeReference type)
		{
			var method = new CodeMemberMethod() { PrivateImplementationType = type };
			Assert.Equal(type, method.PrivateImplementationType);
		}

		[Fact]
		public void ImplementationTypes_AddMultiple_ReturnsExpected()
		{
			var method = new CodeMemberMethod();

			CodeTypeReference type1 = new CodeTypeReference(typeof(int));
			method.ImplementationTypes.Add(type1);
			Assert.Equal(new CodeTypeReference[] { type1 }, method.ImplementationTypes.Cast<CodeTypeReference>());

			CodeTypeReference type2 = new CodeTypeReference(typeof(int));
			method.ImplementationTypes.Add(type2);
			Assert.Equal(new CodeTypeReference[] { type1, type2 }, method.ImplementationTypes.Cast<CodeTypeReference>());
		}

		[Fact]
		public void ImplementationTypes_Get_CallsPopulateImplementationTypesOnce()
		{
			var method = new CodeMemberMethod();
			bool calledImplementationTypes = false;
			method.PopulateImplementationTypes += (object sender, EventArgs args) =>
			{
				calledImplementationTypes = true;
				Assert.Same(method, sender);
				Assert.Equal(EventArgs.Empty, args);
			};

			CodeTypeReference type = new CodeTypeReference(typeof(int));
			method.ImplementationTypes.Add(type);
			Assert.Equal(new CodeTypeReference[] { type }, method.ImplementationTypes.Cast<CodeTypeReference>());
			Assert.True(calledImplementationTypes);

			// Only calls PopulateImplementationTypes once
			calledImplementationTypes = false;
			method.ImplementationTypes.Add(type);
			Assert.False(calledImplementationTypes);
		}

		[Fact]
		public void ReturnTypeCustomAttributes_AddMultiple_ReturnsExpected()
		{
			var method = new CodeMemberMethod();

			CodeAttributeDeclaration attribute1 = new CodeAttributeDeclaration("Name1");
			method.ReturnTypeCustomAttributes.Add(attribute1);
			Assert.Equal(new CodeAttributeDeclaration[] { attribute1 }, method.ReturnTypeCustomAttributes.Cast<CodeAttributeDeclaration>());

			CodeAttributeDeclaration attribute2 = new CodeAttributeDeclaration("Name2");
			method.ReturnTypeCustomAttributes.Add(attribute2);
			Assert.Equal(new CodeAttributeDeclaration[] { attribute1, attribute2 }, method.ReturnTypeCustomAttributes.Cast<CodeAttributeDeclaration>());
		}

		[Fact]
		public void TypeParameters_AddMultiple_ReturnsExpected()
		{
			var method = new CodeMemberMethod();

			CodeTypeParameter parameter1 = new CodeTypeParameter("Name1");
			method.TypeParameters.Add(parameter1);
			Assert.Equal(new CodeTypeParameter[] { parameter1 }, method.TypeParameters.Cast<CodeTypeParameter>());

			CodeTypeParameter parameter2 = new CodeTypeParameter("Name2");
			method.TypeParameters.Add(parameter2);
			Assert.Equal(new CodeTypeParameter[] { parameter1, parameter2 }, method.TypeParameters.Cast<CodeTypeParameter>());
		}
	}
}
