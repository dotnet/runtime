// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeArrayCreateExpressionTests : CodeObjectTestBase<CodeArrayCreateExpression>
	{
		[Fact]
		public void Ctor_Default()
		{
			var arrayCreate = new CodeArrayCreateExpression();
			Assert.Equal(typeof(void).FullName, arrayCreate.CreateType.BaseType);
			Assert.Empty(arrayCreate.Initializers);

			Assert.Equal(0, arrayCreate.Size);
			Assert.Null(arrayCreate.SizeExpression);
		}

		public static IEnumerable<object[]> TypeString_TestData()
		{
			yield return new object[] { null, "System.Void" };
			yield return new object[] { "", "System.Void" };
			yield return new object[] { "Int32", "Int32" };
		}

		[Theory]
		[MemberData(nameof(TypeString_TestData))]
		public void Ctor_String_EmptyParamsCodeExpression(string type, string expectedBaseType)
		{
			CodeExpression[] initializers = new CodeExpression[0];
			var arrayCreate = new CodeArrayCreateExpression(type, initializers);
			Assert.Equal(expectedBaseType, arrayCreate.CreateType.BaseType);
			Assert.Equal(initializers, arrayCreate.Initializers.Cast<CodeExpression>());
			Assert.Equal(0, arrayCreate.Size);
			Assert.Null(arrayCreate.SizeExpression);
		}

		[Theory]
		[MemberData(nameof(TypeString_TestData))]
		public void Ctor_String_NonEmptyParamsCodeExpression(string type, string expectedBaseType)
		{
			CodeExpression[] initializers = new CodeExpression[] { new CodePrimitiveExpression("Value1"), new CodePrimitiveExpression("Value2") };
			var arrayCreate = new CodeArrayCreateExpression(type, initializers);
			Assert.Equal(expectedBaseType, arrayCreate.CreateType.BaseType);
			Assert.Equal(initializers, arrayCreate.Initializers.Cast<CodeExpression>());
			Assert.Equal(0, arrayCreate.Size);
			Assert.Null(arrayCreate.SizeExpression);
		}

		public static IEnumerable<object[]> Type_TestData()
		{
			yield return new object[] { typeof(int), "System.Int32" };
			yield return new object[] { typeof(List<>), "System.Collections.Generic.List`1" };
			yield return new object[] { typeof(void), "System.Void" };
		}

		[Theory]
		[MemberData(nameof(Type_TestData))]
		public void Ctor_Type_EmptyParamsCodeExpression(Type type, string expectedBaseType)
		{
			CodeExpression[] initializers = new CodeExpression[0];
			var arrayCreate = new CodeArrayCreateExpression(type, initializers);
			Assert.Equal(expectedBaseType, arrayCreate.CreateType.BaseType);
			Assert.Equal(initializers, arrayCreate.Initializers.Cast<CodeExpression>());
			Assert.Equal(0, arrayCreate.Size);
			Assert.Null(arrayCreate.SizeExpression);
		}

		[Theory]
		[MemberData(nameof(Type_TestData))]
		public void Ctor_Type_NonEmptyParamsCodeExpression(Type type, string expectedBaseType)
		{
			CodeExpression[] initializers = new CodeExpression[] { new CodePrimitiveExpression("Value1"), new CodePrimitiveExpression("Value2") };
			var arrayCreate = new CodeArrayCreateExpression(type, initializers);
			Assert.Equal(expectedBaseType, arrayCreate.CreateType.BaseType);
			Assert.Equal(initializers, arrayCreate.Initializers.Cast<CodeExpression>());
			Assert.Equal(0, arrayCreate.Size);
			Assert.Null(arrayCreate.SizeExpression);
		}

		[Theory]
		[MemberData(nameof(CodeTypeReference_TestData))]
		public void Ctor_CodeTypeReference_ParamsCodeExpression(CodeTypeReference type)
		{
			CodeExpression[] initializers = new CodeExpression[0];
			var arrayCreate = new CodeArrayCreateExpression(type, initializers);
			Assert.Equal((type ?? new CodeTypeReference("")).BaseType, arrayCreate.CreateType.BaseType);
			Assert.Equal(initializers, arrayCreate.Initializers.Cast<CodeExpression>());
			Assert.Equal(0, arrayCreate.Size);
			Assert.Null(arrayCreate.SizeExpression);
		}

		[Theory]
		[MemberData(nameof(CodeTypeReference_TestData))]
		public void Ctor_CodeTypeReference_NonEmptyParamsCodeExpression(CodeTypeReference type)
		{
			CodeExpression[] initializers = new CodeExpression[] { new CodePrimitiveExpression("Value1"), new CodePrimitiveExpression("Value2") };
			var arrayCreate = new CodeArrayCreateExpression(type, initializers);
			Assert.Equal((type ?? new CodeTypeReference("")).BaseType, arrayCreate.CreateType.BaseType);
			Assert.Equal(initializers, arrayCreate.Initializers.Cast<CodeExpression>());
			Assert.Equal(0, arrayCreate.Size);
			Assert.Null(arrayCreate.SizeExpression);
		}

		[Theory]
		[MemberData(nameof(TypeString_TestData))]
		public void Ctor_String_Int(string type, string expectedBaseType)
		{
			foreach (int size in new int[] { -1, 0, 1 })
			{
				var arrayCreate = new CodeArrayCreateExpression(type, size);
				Assert.Equal(expectedBaseType, arrayCreate.CreateType.BaseType);
				Assert.Empty(arrayCreate.Initializers);
				Assert.Equal(size, arrayCreate.Size);
				Assert.Null(arrayCreate.SizeExpression);
			}
		}

		[Theory]
		[MemberData(nameof(Type_TestData))]
		public void Ctor_Type_Int(Type type, string expectedBaseType)
		{
			foreach (int size in new int[] { -1, 0, 1 })
			{
				var arrayCreate = new CodeArrayCreateExpression(type, size);
				Assert.Equal(expectedBaseType, arrayCreate.CreateType.BaseType);
				Assert.Empty(arrayCreate.Initializers);
				Assert.Equal(size, arrayCreate.Size);
				Assert.Null(arrayCreate.SizeExpression);
			}
		}

		[Theory]
		[MemberData(nameof(CodeTypeReference_TestData))]
		public void Ctor_CodeTypeReference_Int(CodeTypeReference type)
		{
			foreach (int size in new int[] { -1, 0, 1 })
			{
				var arrayCreate = new CodeArrayCreateExpression(type, size);
				Assert.Equal((type ?? new CodeTypeReference("")).BaseType, arrayCreate.CreateType.BaseType);
				Assert.Empty(arrayCreate.Initializers);
				Assert.Equal(size, arrayCreate.Size);
				Assert.Null(arrayCreate.SizeExpression);
			}
		}

		[Theory]
		[MemberData(nameof(TypeString_TestData))]
		public void Ctor_String_CodeExpression(string type, string expectedBaseType)
		{
			foreach (CodeExpression sizeExpression in new CodeExpression[] { null, new CodePrimitiveExpression("Value") })
			{
				var arrayCreate = new CodeArrayCreateExpression(type, sizeExpression);
				Assert.Equal(expectedBaseType, arrayCreate.CreateType.BaseType);
				Assert.Empty(arrayCreate.Initializers);
				Assert.Equal(0, arrayCreate.Size);
				Assert.Equal(sizeExpression, arrayCreate.SizeExpression);
			}
		}

		[Theory]
		[MemberData(nameof(Type_TestData))]
		public void Ctor_Type_CodeExpression(Type type, string expectedBaseType)
		{
			foreach (CodeExpression sizeExpression in new CodeExpression[] { null, new CodePrimitiveExpression("Value") })
			{
				var arrayCreate = new CodeArrayCreateExpression(type, sizeExpression);
				Assert.Equal(expectedBaseType, arrayCreate.CreateType.BaseType);
				Assert.Empty(arrayCreate.Initializers);
				Assert.Equal(0, arrayCreate.Size);
				Assert.Equal(sizeExpression, arrayCreate.SizeExpression);
			}
		}

		[Theory]
		[MemberData(nameof(CodeTypeReference_TestData))]
		public void Ctor_CodeTypeReference_CodeExpression(CodeTypeReference type)
		{
			foreach (CodeExpression sizeExpression in new CodeExpression[] { null, new CodePrimitiveExpression("Value") })
			{
				var arrayCreate = new CodeArrayCreateExpression(type, sizeExpression);
				Assert.Equal((type ?? new CodeTypeReference("")).BaseType, arrayCreate.CreateType.BaseType);
				Assert.Empty(arrayCreate.Initializers);
				Assert.Equal(0, arrayCreate.Size);
				Assert.Equal(sizeExpression, arrayCreate.SizeExpression);
			}
		}

		[Fact]
		public void Ctor_NullType_ThrowsArgumentNullException()
		{
			AssertExtensions.Throws<ArgumentNullException>("type", () => new CodeArrayCreateExpression((Type)null, new CodePrimitiveExpression()));
			AssertExtensions.Throws<ArgumentNullException>("type", () => new CodeArrayCreateExpression((Type)null, new CodePrimitiveExpression[0]));
			AssertExtensions.Throws<ArgumentNullException>("type", () => new CodeArrayCreateExpression((Type)null, 0));
		}

		[Fact]
		public void Ctor_NullTypeInitializers_ThrowsArgumentNullException()
		{
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeArrayCreateExpression("", (CodePrimitiveExpression[])null));
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeArrayCreateExpression(typeof(void), (CodePrimitiveExpression[])null));
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeArrayCreateExpression(new CodeTypeReference(), (CodePrimitiveExpression[])null));
		}

		[Fact]
		public void Ctor_NullTypeInInitalizers_ThrowsArgumentNullException()
		{
			CodePrimitiveExpression[] initializers = new CodePrimitiveExpression[] { null };
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeArrayCreateExpression("", initializers));
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeArrayCreateExpression(typeof(void), initializers));
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeArrayCreateExpression(new CodeTypeReference(), initializers));
		}

		[Theory]
		[MemberData(nameof(CodeTypeReference_TestData))]
		public void CreateType_Set_Get_ReturnsExpected(CodeTypeReference value)
		{
			var arrayCreate = new CodeArrayCreateExpression();
			arrayCreate.CreateType = value;
			Assert.Equal((value ?? new CodeTypeReference("")).BaseType, arrayCreate.CreateType.BaseType);
		}

		[Fact]
		public void Initializers_AddMultiple_ReturnsExpected()
		{
			var arrayCreate = new CodeArrayCreateExpression();

			CodeExpression expression1 = new CodePrimitiveExpression("Value1");
			arrayCreate.Initializers.Add(expression1);
			Assert.Equal(new CodeExpression[] { expression1 }, arrayCreate.Initializers.Cast<CodeExpression>());

			CodeExpression expression2 = new CodePrimitiveExpression("Value2");
			arrayCreate.Initializers.Add(expression2);
			Assert.Equal(new CodeExpression[] { expression1, expression2 }, arrayCreate.Initializers.Cast<CodeExpression>());
		}

		[Theory]
		[InlineData(-1)]
		[InlineData(0)]
		[InlineData(1)]
		public void Size_Set_Get_ReturnsExpected(int value)
		{
			var arrayCreate = new CodeArrayCreateExpression();
			arrayCreate.Size = value;
			Assert.Equal(value, arrayCreate.Size);
		}

		[Theory]
		[MemberData(nameof(CodeExpression_TestData))]
		public void SizeExpression_Set_Get_ReturnsExpected(CodeExpression value)
		{
			var arrayCreate = new CodeArrayCreateExpression();
			arrayCreate.SizeExpression = value;
			Assert.Equal(value, arrayCreate.SizeExpression);
		}
	}
}
