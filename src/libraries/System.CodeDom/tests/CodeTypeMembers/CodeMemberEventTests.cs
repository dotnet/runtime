// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeMemberEventTests : CodeTypeMemberTestBase<CodeMemberEvent>
	{
		[Fact]
		public void Ctor_Default()
		{
			var memberEvent = new CodeMemberEvent();
			Assert.Equal(typeof(void).FullName, memberEvent.Type.BaseType);
			Assert.Empty(memberEvent.ImplementationTypes);
			Assert.Null(memberEvent.PrivateImplementationType);
		}

		[Theory]
		[MemberData(nameof(CodeTypeReference_TestData))]
		public void Type_Set_Get_ReturnsExpected(CodeTypeReference type)
		{
			var memberEvent = new CodeMemberEvent() { Type = type };
			Assert.Equal((type ?? new CodeTypeReference("")).BaseType, memberEvent.Type.BaseType);
		}

		[Theory]
		[MemberData(nameof(CodeTypeReference_TestData))]
		public void PrivateImplementationType_Set_Get_ReturnsExpected(CodeTypeReference type)
		{
			var memberEvent = new CodeMemberEvent() { PrivateImplementationType = type };
			Assert.Equal(type, memberEvent.PrivateImplementationType);
		}

		[Fact]
		public void ImplementationTypes_AddMultiple_ReturnsExpected()
		{
			var memberEvent = new CodeMemberEvent();

			CodeTypeReference type1 = new CodeTypeReference(typeof(int));
			memberEvent.ImplementationTypes.Add(type1);
			Assert.Equal(new CodeTypeReference[] { type1 }, memberEvent.ImplementationTypes.Cast<CodeTypeReference>());

			CodeTypeReference type2 = new CodeTypeReference(typeof(int));
			memberEvent.ImplementationTypes.Add(type2);
			Assert.Equal(new CodeTypeReference[] { type1, type2 }, memberEvent.ImplementationTypes.Cast<CodeTypeReference>());
		}
	}
}
