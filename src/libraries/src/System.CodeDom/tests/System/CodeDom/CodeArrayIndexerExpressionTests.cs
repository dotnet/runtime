// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeArrayIndexerExpressionTests : CodeObjectTestBase<CodeArrayIndexerExpression>
	{
		[Fact]
		public void Ctor_Default()
		{
			var arrayIndexer = new CodeArrayIndexerExpression();
			Assert.Null(arrayIndexer.TargetObject);
			Assert.Empty(arrayIndexer.Indices);
		}

		public static IEnumerable<object[]> Ctor_TestData()
		{
			yield return new object[] { null, new CodeExpression[0] };
			yield return new object[] { new CodePrimitiveExpression("Value1"), new CodeExpression[] { new CodePrimitiveExpression("Value2") } };
		}

		[Theory]
		[MemberData(nameof(Ctor_TestData))]
		public void Ctor(CodeExpression targetObject, CodeExpression[] indices)
		{
			var arrayIndexer = new CodeArrayIndexerExpression(targetObject, indices);
			Assert.Equal(targetObject, arrayIndexer.TargetObject);
			Assert.Equal(indices, arrayIndexer.Indices.Cast<CodeExpression>());
		}

		[Fact]
		public void Ctor_NullIndices_ThrowsArgumentNullException()
		{
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeArrayIndexerExpression(new CodeExpression(), null));
		}

		[Fact]
		public void Ctor_NullObjectInIndices_ThrowsArgumentNullException()
		{
			AssertExtensions.Throws<ArgumentNullException>("value", () => new CodeArrayIndexerExpression(new CodeExpression(), new CodeExpression[] { null }));
		}

		[Theory]
		[MemberData(nameof(CodeExpression_TestData))]
		public void TargetObject_Set_Get_ReturnsExpected(CodeExpression value)
		{
			var arrayIndexer = new CodeArrayIndexerExpression();
			arrayIndexer.TargetObject = value;
			Assert.Equal(value, arrayIndexer.TargetObject);
		}

		[Fact]
		public void Indices_AddMultiple_ReturnsExpected()
		{
			var arrayIndexer = new CodeArrayIndexerExpression();

			CodeExpression expression1 = new CodePrimitiveExpression("Value1");
			arrayIndexer.Indices.Add(expression1);
			Assert.Equal(new CodeExpression[] { expression1 }, arrayIndexer.Indices.Cast<CodeExpression>());

			CodeExpression expression2 = new CodePrimitiveExpression("Value2");
			arrayIndexer.Indices.Add(expression2);
			Assert.Equal(new CodeExpression[] { expression1, expression2 }, arrayIndexer.Indices.Cast<CodeExpression>());
		}
	}
}
