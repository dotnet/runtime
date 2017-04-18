// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeMemberFieldTests : CodeTypeMemberTestBase<CodeMemberField>
	{
		[Fact]
		public void Ctor_Default()
		{
			var field = new CodeMemberField();
			Assert.Equal(typeof(void).FullName, field.Type.BaseType);
			Assert.Null(field.InitExpression);
		}

		[Theory]
		[MemberData(nameof(CodeTypeReference_TestData))]
		public void Ctor_CodeTypeReference_String(CodeTypeReference type)
		{
			var field = new CodeMemberField(type, "name");
			Assert.Equal((type ?? new CodeTypeReference("")).BaseType, field.Type.BaseType);
			Assert.Equal("name", field.Name);
		}

		[Theory]
		[MemberData(nameof(CodeTypeReference_TestData))]
		public void Ctor_Type_String(CodeTypeReference type)
		{
			if (type == null)
			{
				return;
			}

			var field = new CodeMemberField(type.BaseType, "name");
			Assert.Equal(type.BaseType, field.Type.BaseType);
			Assert.Equal("name", field.Name);
		}

		[Fact]
		public void Ctor_Type_String_NullType_ThrowsArgumentNullException()
		{
			AssertExtensions.Throws<ArgumentNullException>("type", () => new CodeMemberField((Type)null, "name"));
		}

		public static IEnumerable<object[]> Ctor_String_String_TestData()
		{
			yield return new object[] { null, null, "System.Void" };
			yield return new object[] { "", "", "System.Void" };
			yield return new object[] { "Int32", "Name", "Int32" };
		}

		[Theory]
		[MemberData(nameof(Ctor_String_String_TestData))]
		public void Ctor_String_String(string type, string name, string expectedBaseType)
		{
			var field = new CodeMemberField(type, name);
			Assert.Equal(expectedBaseType, field.Type.BaseType);
			Assert.Equal(name ?? string.Empty, field.Name);
		}

		[Theory]
		[MemberData(nameof(CodeTypeReference_TestData))]
		public void Type_Set_Get_ReturnsExpected(CodeTypeReference type)
		{
			var field = new CodeMemberField() { Type = type };
			Assert.Equal((type ?? new CodeTypeReference("")).BaseType, field.Type.BaseType);
		}
	}
}
