// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeAttributeDeclarationTests : CodeDomTestBase
	{
		[Fact]
		public void Ctor_Default()
		{
			var declaration = new CodeAttributeDeclaration();
			Assert.Empty(declaration.Name);
			Assert.Empty(declaration.Arguments);
			Assert.Null(declaration.AttributeType);
		}

		public static IEnumerable<object[]> Ctor_String_TestData()
		{
			yield return new object[] { null, new CodePrimitiveExpression[0] };
			yield return new object[] { "", new CodePrimitiveExpression[0] };
			yield return new object[] { "Value1", new CodeAttributeArgument[0] };
			yield return new object[] { "Value1", new CodeAttributeArgument[] { new CodeAttributeArgument("Name", new CodePrimitiveExpression("Value")) } };
			yield return new object[] { "Value1", new CodeAttributeArgument[] { new CodeAttributeArgument() } };
		}

		[Theory]
		[MemberData(nameof(Ctor_String_TestData))]
		public void Ctor_String(string name, CodeAttributeArgument[] arguments)
		{
			if (arguments.Length == 0)
			{
				var declaration1 = new CodeAttributeDeclaration(name);
				Assert.Equal(name ?? string.Empty, declaration1.Name);
				Assert.Empty(declaration1.Arguments);
				Assert.Equal(new CodeTypeReference(name).BaseType, declaration1.AttributeType.BaseType);
			}
			var declaration2 = new CodeAttributeDeclaration(name, arguments);
			Assert.Equal(name ?? string.Empty, declaration2.Name);
			Assert.Equal(arguments, declaration2.Arguments.Cast<CodeAttributeArgument>());
			Assert.Equal(new CodeTypeReference(name).BaseType, declaration2.AttributeType.BaseType);
		}

		public static IEnumerable<object[]> Ctor_CodeTypeReference_TestData()
		{
			yield return new object[] { null, null };
			yield return new object[] { new CodeTypeReference(typeof(int)), new CodeAttributeArgument[0] };
			yield return new object[] { new CodeTypeReference("System.Void"), new CodeAttributeArgument[] { new CodeAttributeArgument("Name", new CodePrimitiveExpression("Value")) } };
			yield return new object[] { new CodeTypeReference("System.Void"), new CodeAttributeArgument[] { new CodeAttributeArgument() } };
		}

		[Theory]
		[MemberData(nameof(Ctor_CodeTypeReference_TestData))]
		public void Ctor_CodeTypeReference(CodeTypeReference attributeType, CodeAttributeArgument[] arguments)
		{
			if (arguments == null || arguments.Length == 0)
			{
				var declaration1 = new CodeAttributeDeclaration(attributeType);
				Assert.Equal(attributeType?.BaseType ?? string.Empty, declaration1.Name);
				Assert.Equal(attributeType, declaration1.AttributeType);
				Assert.Empty(declaration1.Arguments);
			}
			var declaration2 = new CodeAttributeDeclaration(attributeType, arguments);
			Assert.Equal(attributeType?.BaseType ?? string.Empty, declaration2.Name);
			Assert.Equal(attributeType, declaration2.AttributeType);
			Assert.Equal(arguments ?? new CodeAttributeArgument[0], declaration2.Arguments.Cast<CodeAttributeArgument>());
		}

		[Fact]
		public void Ctor_NullArguments_ThrowsArgumentNullException()
		{
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeAttributeDeclaration("name", null));
		}

		[Fact]
		public void Ctor_NullObjectInArguments_ThrowsArgumentNullException()
		{
			CodeAttributeArgument[] arguments = new CodeAttributeArgument[] { null };
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeAttributeDeclaration("name", arguments));
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeAttributeDeclaration(new CodeTypeReference(), arguments));
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("Name")]
		public void Name_Set_Get_ReturnsExpected(string value)
		{
			var declaration = new CodeAttributeDeclaration();
			declaration.Name = value;
			Assert.Equal(value ?? string.Empty, declaration.Name);
		}

		[Fact]
		public void Arguments_AddMultiple_ReturnsExpected()
		{
			var declaration = new CodeAttributeDeclaration();

			CodeAttributeArgument argument1 = new CodeAttributeArgument(new CodePrimitiveExpression("Value1"));
			declaration.Arguments.Add(argument1);
			Assert.Equal(new CodeAttributeArgument[] { argument1 }, declaration.Arguments.Cast<CodeAttributeArgument>());

			CodeAttributeArgument argument2 = new CodeAttributeArgument(new CodePrimitiveExpression("Value2"));
			declaration.Arguments.Add(argument2);
			Assert.Equal(new CodeAttributeArgument[] { argument1, argument2 }, declaration.Arguments.Cast<CodeAttributeArgument>());
		}
	}
}
