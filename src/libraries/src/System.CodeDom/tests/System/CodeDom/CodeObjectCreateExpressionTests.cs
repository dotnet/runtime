// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeObjectCreateExpressionTests : CodeObjectTestBase<CodeObjectCreateExpression>
	{
		[Fact]
		public void Ctor_Default()
		{
			var objectCreate = new CodeObjectCreateExpression();
			Assert.Equal(typeof(void).FullName, objectCreate.CreateType.BaseType);
			Assert.Empty(objectCreate.Parameters);
		}

		public static IEnumerable<object[]> Ctor_CodeTypeReference_ParamsCodeExpression_TestData()
		{
			yield return new object[] { null, new CodeExpression[0] };
			yield return new object[] { new CodeTypeReference(), new CodeExpression[] { new CodePrimitiveExpression() } };
			yield return new object[] { new CodeTypeReference(typeof(int)), new CodeExpression[] { new CodePrimitiveExpression("Value") } };
		}

		[Theory]
		[MemberData(nameof(Ctor_CodeTypeReference_ParamsCodeExpression_TestData))]
		public void Ctor(CodeTypeReference type, CodeExpression[] parameters)
		{
			var objectCreate = new CodeObjectCreateExpression(type, parameters);
			Assert.Equal((type ?? new CodeTypeReference("")).BaseType, objectCreate.CreateType.BaseType);
			Assert.Equal(parameters, objectCreate.Parameters.Cast<CodeExpression>());
		}

		public static IEnumerable<object[]> Ctor_String_ParamsCodeExpression_TestData()
		{
			yield return new object[] { null, new CodeExpression[0], "System.Void" };
			yield return new object[] { "", new CodeExpression[] { new CodePrimitiveExpression() }, "System.Void" };
			yield return new object[] { "Int32", new CodeExpression[] { new CodePrimitiveExpression("Value") }, "Int32" };
		}

		[Theory]
		[MemberData(nameof(Ctor_String_ParamsCodeExpression_TestData))]
		public void Ctor(string type, CodeExpression[] parameters, string expectedBaseType)
		{
			var objectCreate = new CodeObjectCreateExpression(type, parameters);
			Assert.Equal(expectedBaseType, objectCreate.CreateType.BaseType);
			Assert.Equal(parameters, objectCreate.Parameters.Cast<CodeExpression>());
		}

		public static IEnumerable<object[]> Ctor_Type_ParamsCodeExpression_TestData()
		{
			yield return new object[] { typeof(int), new CodeExpression[0], "System.Int32" };
			yield return new object[] { typeof(List<>), new CodeExpression[] { new CodePrimitiveExpression() }, "System.Collections.Generic.List`1" };
			yield return new object[] { typeof(void), new CodeExpression[] { new CodePrimitiveExpression("Value") }, "System.Void" };
		}

		[Theory]
		[MemberData(nameof(Ctor_Type_ParamsCodeExpression_TestData))]
		public void Ctor_Type_String(Type type, CodeExpression[] parameters, string expectedBaseType)
		{
			var objectCreate = new CodeObjectCreateExpression(type, parameters);
			Assert.Equal(expectedBaseType, objectCreate.CreateType.BaseType);
			Assert.Equal(parameters, objectCreate.Parameters.Cast<CodeExpression>());
		}

		[Fact]
		public void Ctor_NullParameters_ThrowsArgumentNullException()
		{
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeObjectCreateExpression(new CodeTypeReference(), null));
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeObjectCreateExpression("System.Int32", null));
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeObjectCreateExpression(typeof(int), null));
		}

		[Fact]
		public void Ctor_NullObjectInParameters_ThrowsArgumentNullException()
		{
			CodeExpression[] parameters = new CodeExpression[] { null };

			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeObjectCreateExpression(new CodeTypeReference(), parameters));
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeObjectCreateExpression("System.Int32", parameters));
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeObjectCreateExpression(typeof(int), parameters));
		}

		[Theory]
		[MemberData(nameof(CodeTypeReference_TestData))]
		public void CreateType_Set_Get_ReturnsExpected(CodeTypeReference value)
		{
			var objectCreate = new CodeObjectCreateExpression();
			objectCreate.CreateType = value;
			Assert.Equal((value ?? new CodeTypeReference("")).BaseType, objectCreate.CreateType.BaseType);
		}

		[Fact]
		public void Parameters_AddMultiple_ReturnsExpected()
		{
			var objectCreate = new CodeObjectCreateExpression();

			CodeExpression expression1 = new CodePrimitiveExpression("Value1");
			objectCreate.Parameters.Add(expression1);
			Assert.Equal(new CodeExpression[] { expression1 }, objectCreate.Parameters.Cast<CodeExpression>());

			CodeExpression expression2 = new CodePrimitiveExpression("Value2");
			objectCreate.Parameters.Add(expression2);
			Assert.Equal(new CodeExpression[] { expression1, expression2 }, objectCreate.Parameters.Cast<CodeExpression>());
		}
	}
}
