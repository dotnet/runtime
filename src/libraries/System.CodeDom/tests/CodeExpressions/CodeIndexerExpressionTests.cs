// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeIndexerExpressionTests : CodeObjectTestBase<CodeIndexerExpression>
	{
		[Fact]
		public void Ctor_Default()
		{
			var indexer = new CodeIndexerExpression();
			Assert.Null(indexer.TargetObject);
			Assert.Empty(indexer.Indices);
		}

		public static IEnumerable<object[]> Ctor_CodeExpression_ParamsCodeExpression_TestData()
		{
			yield return new object[] { null, new CodeExpression[0] };
			yield return new object[] { new CodePrimitiveExpression(""), new CodeExpression[] { new CodePrimitiveExpression() } };
			yield return new object[] { new CodePrimitiveExpression("Hello"), new CodeExpression[] { new CodePrimitiveExpression("Value1") } };
		}

		[Theory]
		[MemberData(nameof(Ctor_CodeExpression_ParamsCodeExpression_TestData))]
		public void Ctor_CodeExpression_ParamsCodeExpression(CodeExpression targetObject, CodeExpression[] parameters)
		{
			var indexer = new CodeIndexerExpression(targetObject, parameters);
			Assert.Equal(targetObject, indexer.TargetObject);
			Assert.Equal(parameters, indexer.Indices.Cast<CodeExpression>());
		}

		[Fact]
		public void Ctor_NullIndices_ThrowsArgumentNullException()
		{
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeIndexerExpression(new CodePrimitiveExpression("Hello"), null));
		}

		[Fact]
		public void Ctor_NullObjectInIndices_ThrowsArgumentNullException()
		{
			CodeExpression[] parameters = new CodeExpression[] { null };
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeIndexerExpression(new CodePrimitiveExpression("Hello"), parameters));
		}

		[Theory]
		[MemberData(nameof(CodeExpression_TestData))]
		public void TargetObject_Set_Get_ReturnsExpected(CodeExpression value)
		{
			var indexer = new CodeIndexerExpression();
			indexer.TargetObject = value;
			Assert.Equal(value, indexer.TargetObject);
		}

		[Fact]
		public void Parameters_AddMultiple_ReturnsExpected()
		{
			var indexer = new CodeIndexerExpression();

			CodeExpression expression1 = new CodePrimitiveExpression("Value1");
			indexer.Indices.Add(expression1);
			Assert.Equal(new CodeExpression[] { expression1 }, indexer.Indices.Cast<CodeExpression>());

			CodeExpression expression2 = new CodePrimitiveExpression("Value2");
			indexer.Indices.Add(expression2);
			Assert.Equal(new CodeExpression[] { expression1, expression2 }, indexer.Indices.Cast<CodeExpression>());
		}
	}
}
