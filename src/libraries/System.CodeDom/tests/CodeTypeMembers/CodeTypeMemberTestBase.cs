// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.CodeDom.Tests
{
	public abstract class CodeTypeMemberTestBase<T> : CodeObjectTestBase<T> where T : CodeTypeMember, new()
	{
		[Fact]
		public void Ctor_Default_MemberBase()
		{
			CodeTypeMember member = new T();
			Assert.Empty(member.Name);

			Assert.Equal(MemberAttributes.Private | MemberAttributes.Final, member.Attributes);
			Assert.Empty(member.CustomAttributes);

			Assert.Empty(member.Comments);
			Assert.Null(member.LinePragma);

			Assert.Empty(member.StartDirectives);
			Assert.Empty(member.EndDirectives);
		}

		[Theory]
		[InlineData("Name")]
		[InlineData("")]
		[InlineData(null)]
		public void Name_Set_Get_ReturnsExpected(string value)
		{
			CodeTypeMember member = new T() { Name = value };
			Assert.Equal(value ?? string.Empty, member.Name);
		}

		[Theory]
		[InlineData(MemberAttributes.Abstract)]
		[InlineData(MemberAttributes.Public)]
		[InlineData((MemberAttributes)0)]
		[InlineData((MemberAttributes)int.MaxValue)]
		public void Attrributes_Set_Get_ReturnsExpected(MemberAttributes value)
		{
			CodeTypeMember member = new T() { Attributes = value };
			Assert.Equal(value, member.Attributes);
		}

		[Fact]
		public void LinePragma_SetNull_Get_ReturnsNull()
		{
			CodeTypeMember member = new T() { LinePragma = null };
			Assert.Null(member.LinePragma);
		}

		[Fact]
		public void LinePragma_SetNonNull_Get_ReturnsExpected()
		{
			CodeLinePragma pragma = new CodeLinePragma("FileName", 1);
			CodeTypeMember member = new T() { LinePragma = pragma };
			Assert.Equal(pragma, member.LinePragma);
		}

		[Fact]
		public void CustomAttributes_SetNull_Get_ReturnsEmpty()
		{
			CodeTypeMember member = new T() { CustomAttributes = null };
			Assert.Empty(member.CustomAttributes);
		}

		[Fact]
		public void CustomAttributes_SetNonNull_Get_ReturnsExpected()
		{
			CodeTypeMember member = new T() { CustomAttributes = null };
			CodeAttributeDeclarationCollection value = new CodeAttributeDeclarationCollection();
			value.Add(new CodeAttributeDeclaration("Name1"));
			value.Add(new CodeAttributeDeclaration("Name2"));

			member.CustomAttributes = value;
			Assert.Equal(value, member.CustomAttributes);
		}

		[Fact]
		public void CustomAttributes_AddMultiple_ReturnsExpected()
		{
			CodeTypeMember member = new T();

			CodeAttributeDeclaration attribute1 = new CodeAttributeDeclaration("Name1");
			member.CustomAttributes.Add(attribute1);
			Assert.Equal(new CodeAttributeDeclaration[] { attribute1 }, member.CustomAttributes.Cast<CodeAttributeDeclaration>());

			CodeAttributeDeclaration attribute2 = new CodeAttributeDeclaration("Name2");
			member.CustomAttributes.Add(attribute2);
			Assert.Equal(new CodeAttributeDeclaration[] { attribute1, attribute2 }, member.CustomAttributes.Cast<CodeAttributeDeclaration>());
		}

		[Fact]
		public void Comments_AddMultiple_ReturnsExpected()
		{
			CodeTypeMember member = new T();

			CodeCommentStatement comment1 = new CodeCommentStatement("Comment1");
			member.Comments.Add(comment1);
			Assert.Equal(new CodeCommentStatement[] { comment1 }, member.Comments.Cast<CodeCommentStatement>());

			CodeCommentStatement comment2 = new CodeCommentStatement("Comment2");
			member.Comments.Add(comment2);
			Assert.Equal(new CodeCommentStatement[] { comment1, comment2 }, member.Comments.Cast<CodeCommentStatement>());
		}

		[Fact]
		public void StartDirectives_AddMultiple_ReturnsExpected()
		{
			CodeTypeMember member = new T();

			CodeRegionDirective directive1 = new CodeRegionDirective(CodeRegionMode.None, "Region1");
			member.StartDirectives.Add(directive1);
			Assert.Equal(new CodeRegionDirective[] { directive1 }, member.StartDirectives.Cast<CodeDirective>());

			CodeRegionDirective directive2 = new CodeRegionDirective(CodeRegionMode.None, "Region2");
			member.StartDirectives.Add(directive2);
			Assert.Equal(new CodeRegionDirective[] { directive1, directive2 }, member.StartDirectives.Cast<CodeDirective>());
		}

		[Fact]
		public void EndDirectives_AddMultiple_ReturnsExpected()
		{
			CodeTypeMember member = new T();

			CodeRegionDirective directive1 = new CodeRegionDirective(CodeRegionMode.None, "Region1");
			member.EndDirectives.Add(directive1);
			Assert.Equal(new CodeRegionDirective[] { directive1 }, member.EndDirectives.Cast<CodeDirective>());

			CodeRegionDirective directive2 = new CodeRegionDirective(CodeRegionMode.None, "Region2");
			member.EndDirectives.Add(directive2);
			Assert.Equal(new CodeRegionDirective[] { directive1, directive2 }, member.EndDirectives.Cast<CodeDirective>());
		}
	}
}
