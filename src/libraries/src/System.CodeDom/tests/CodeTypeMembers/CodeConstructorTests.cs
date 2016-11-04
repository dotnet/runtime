// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeConstructorTests : CodeObjectTestBase<CodeConstructor>
	{
		[Fact]
		public void Ctor_Default()
		{
			var constructor = new CodeConstructor();
			Assert.Equal(".ctor", constructor.Name);
			Assert.Empty(constructor.BaseConstructorArgs);
			Assert.Empty(constructor.ChainedConstructorArgs);
		}

		[Fact]
		public void BaseConstructorArgs_AddMultiple_ReturnsExpected()
		{
			var constructor = new CodeConstructor();

			CodeExpression expression1 = new CodePrimitiveExpression("Value1");
			constructor.BaseConstructorArgs.Add(expression1);
			Assert.Equal(new CodeExpression[] { expression1 }, constructor.BaseConstructorArgs.Cast<CodeExpression>());

			CodeExpression expression2 = new CodePrimitiveExpression("Value2");
			constructor.BaseConstructorArgs.Add(expression2);
			Assert.Equal(new CodeExpression[] { expression1, expression2 }, constructor.BaseConstructorArgs.Cast<CodeExpression>());
		}

		[Fact]
		public void ChainedConstructorArgs_AddMultiple_ReturnsExpected()
		{
			var constructor = new CodeConstructor();

			CodeExpression expression1 = new CodePrimitiveExpression("Value1");
			constructor.ChainedConstructorArgs.Add(expression1);
			Assert.Equal(new CodeExpression[] { expression1 }, constructor.ChainedConstructorArgs.Cast<CodeExpression>());

			CodeExpression expression2 = new CodePrimitiveExpression("Value2");
			constructor.ChainedConstructorArgs.Add(expression2);
			Assert.Equal(new CodeExpression[] { expression1, expression2 }, constructor.ChainedConstructorArgs.Cast<CodeExpression>());
		}
	}
}
