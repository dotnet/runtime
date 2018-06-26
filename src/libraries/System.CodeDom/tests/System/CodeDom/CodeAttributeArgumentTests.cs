// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeAttributeArgumentTests : CodeDomTestBase
	{
		[Fact]
		public void Ctor_Default()
		{
			var argument = new CodeAttributeArgument();
			Assert.Empty(argument.Name);
			Assert.Null(argument.Value);
		}

		public static IEnumerable<object[]> Ctor_TestData()
		{
			yield return new object[] { null, null };
			yield return new object[] { "", new CodePrimitiveExpression("Value1") };
			yield return new object[] { "Value1", new CodePrimitiveExpression("Value1") };
		}

		[Theory]
		[MemberData(nameof(Ctor_TestData))]
		public void Ctor(string name, CodeExpression value)
		{
			if (string.IsNullOrEmpty(name))
			{
				var argument1 = new CodeAttributeArgument(value);
				Assert.Empty(argument1.Name);
				Assert.Equal(value, argument1.Value);
			}
			var argument2 = new CodeAttributeArgument(name, value);
			Assert.Equal(name ?? string.Empty, argument2.Name);
			Assert.Equal(value, argument2.Value);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("Name")]
		public void Name_Set_Get_ReturnsExpected(string value)
		{
			var argument = new CodeAttributeArgument();
			argument.Name = value;
			Assert.Equal(value ?? string.Empty, argument.Name);
		}

		[Theory]
		[MemberData(nameof(CodeExpression_TestData))]
		public void Value_Set_Get_ReturnsExpected(CodeExpression value)
		{
			var argument = new CodeAttributeArgument();
			argument.Value = value;
			Assert.Equal(value, argument.Value);
		}
	}
}
