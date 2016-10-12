// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeTypeDelegateTests : CodeObjectTestBase<CodeTypeDelegate>
	{
		[Fact]
		public void Ctor_Default()
		{
			var typeDelegate = new CodeTypeDelegate();
			Assert.Empty(typeDelegate.Name);

			Assert.Equal(1, typeDelegate.BaseTypes.Count);
			Assert.Equal(typeof(Delegate).FullName, typeDelegate.BaseTypes[0].BaseType);

			Assert.True(typeDelegate.IsClass);

			Assert.Equal(typeof(void).FullName, typeDelegate.ReturnType.BaseType);
			Assert.Empty(typeDelegate.Parameters);
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void Ctor_String(string name)
		{
			var typeDelegate = new CodeTypeDelegate(name);
			Assert.Equal(name ?? string.Empty, typeDelegate.Name);

			Assert.Equal(1, typeDelegate.BaseTypes.Count);
			Assert.Equal(typeof(Delegate).FullName, typeDelegate.BaseTypes[0].BaseType);

			Assert.True(typeDelegate.IsClass);

			Assert.Equal(typeof(void).FullName, typeDelegate.ReturnType.BaseType);
			Assert.Empty(typeDelegate.Parameters);
		}

		[Theory]
		[MemberData(nameof(CodeTypeReference_TestData))]
		public void ReturnType_Set_Get_ReturnsExpected(CodeTypeReference value)
		{
			var typeDelegate = new CodeTypeDelegate();
			typeDelegate.ReturnType = value;
			Assert.Equal((value ?? new CodeTypeReference("")).BaseType, typeDelegate.ReturnType.BaseType);
		}

		[Fact]
		public void Parameters_AddMultiple_ReturnsExpected()
		{
			var typeDelegate = new CodeTypeDelegate();

			CodeParameterDeclarationExpression parameter1 = new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(int)), "name1");
			typeDelegate.Parameters.Add(parameter1);
			Assert.Equal(new CodeParameterDeclarationExpression[] { parameter1 }, typeDelegate.Parameters.Cast<CodeParameterDeclarationExpression>());

			CodeParameterDeclarationExpression parameter2 = new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(int)), "name2");
			typeDelegate.Parameters.Add(parameter2);
			Assert.Equal(new CodeParameterDeclarationExpression[] { parameter1, parameter2 }, typeDelegate.Parameters.Cast<CodeParameterDeclarationExpression>());
		}
	}
}
