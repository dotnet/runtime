// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodePrimitiveExpressionTests : CodeObjectTestBase<CodePrimitiveExpression>
	{
		[Fact]
		public void Ctor_Default()
		{
			var primitive = new CodePrimitiveExpression();
			Assert.Null(primitive.Value);
		}

		public static IEnumerable<object[]> Object_TestData()
		{
			yield return new object[] { null };
			yield return new object[] { 1 };
			yield return new object[] { "2" };
		}

		[Theory]
		[MemberData(nameof(Object_TestData))]
		public void Ctor_Object(object value)
		{
			var primitive = new CodePrimitiveExpression(value);
			Assert.Equal(value, primitive.Value);
		}

		[Theory]
		[MemberData(nameof(Object_TestData))]
		public void Value_Set_Get_ReturnsExpected(object value)
		{
			var primitive = new CodePrimitiveExpression();
			primitive.Value = value;
			Assert.Equal(value, primitive.Value);
		}
	}
}
