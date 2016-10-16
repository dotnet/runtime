// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeNamespaceTests : CodeObjectTestBase<CodeNamespace>
	{
		[Fact]
		public void Ctor_Default()
		{
			var codeNamespace = new CodeNamespace();
			Assert.Empty(codeNamespace.Name);
			Assert.Empty(codeNamespace.Types);
			Assert.Empty(codeNamespace.Imports);
			Assert.Empty(codeNamespace.Comments);
		}

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void Ctor_String(string name)
		{
			var codeNamespace = new CodeNamespace(name);
			Assert.Equal(name ?? string.Empty, codeNamespace.Name);
			Assert.Empty(codeNamespace.Types);
			Assert.Empty(codeNamespace.Imports);
			Assert.Empty(codeNamespace.Comments);
		}

        [Fact]
        public void Ctor_SerializationInfo_StreamingContext()
        {
            ConstructorInfo constructor = typeof(CodeNamespace).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(SerializationInfo), typeof(StreamingContext) }, null);
            var codeNamespace = (CodeNamespace)constructor.Invoke(new object[] { default(SerializationInfo), default(StreamingContext) });
            Assert.Empty(codeNamespace.Name);
            Assert.Empty(codeNamespace.Types);
            Assert.Empty(codeNamespace.Imports);
            Assert.Empty(codeNamespace.Comments);
        }

		[Theory]
		[MemberData(nameof(String_TestData))]
		public void Name_Set_Get_ReturnsExpected(string value)
		{
			var codeNamespace = new CodeNamespace();
			codeNamespace.Name = value;
			Assert.Equal(value ?? string.Empty, codeNamespace.Name);
		}

		[Fact]
		public void Types_AddMultiple_ReturnsExpected()
		{
			var codeNamespace = new CodeNamespace();

			CodeTypeDeclaration type1 = new CodeTypeDeclaration("Name1");
			codeNamespace.Types.Add(type1);
			Assert.Equal(new CodeTypeDeclaration[] { type1 }, codeNamespace.Types.Cast<CodeTypeDeclaration>());

			CodeTypeDeclaration type2 = new CodeTypeDeclaration("Name2");
			codeNamespace.Types.Add(type2);
			Assert.Equal(new CodeTypeDeclaration[] { type1, type2 }, codeNamespace.Types.Cast<CodeTypeDeclaration>());
		}

		[Fact]
		public void Types_Get_CallsPopulateTypesOnce()
		{
			var codeNamespace = new CodeNamespace();
			bool calledPopulateTypes = false;
			codeNamespace.PopulateTypes += (object sender, EventArgs args) =>
			{
				calledPopulateTypes = true;
				Assert.Same(codeNamespace, sender);
				Assert.Equal(EventArgs.Empty, args);
			};

			codeNamespace.Types.Add(new CodeTypeDeclaration("Name1"));
			Assert.True(calledPopulateTypes);

			// Only calls PopulateTypes once
			calledPopulateTypes = false;
			codeNamespace.Types.Add(new CodeTypeDeclaration("Name2"));
			Assert.False(calledPopulateTypes);
		}

		[Fact]
		public void Imports_AddMultiple_ReturnsExpected()
		{
			var codeNamespace = new CodeNamespace();

			CodeNamespaceImport type1 = new CodeNamespaceImport("Namespace1");
			codeNamespace.Imports.Add(type1);
			Assert.Equal(new CodeNamespaceImport[] { type1 }, codeNamespace.Imports.Cast<CodeNamespaceImport>());

			CodeNamespaceImport type2 = new CodeNamespaceImport("Namespace2");
			codeNamespace.Imports.Add(type2);
			Assert.Equal(new CodeNamespaceImport[] { type1, type2 }, codeNamespace.Imports.Cast<CodeNamespaceImport>());
		}

		[Fact]
		public void Imports_Get_CallsPopulateImportsOnce()
		{
			var codeNamespace = new CodeNamespace();
			bool calledPopulateImports = false;
			codeNamespace.PopulateImports += (object sender, EventArgs args) =>
			{
				calledPopulateImports = true;
				Assert.Same(codeNamespace, sender);
				Assert.Equal(EventArgs.Empty, args);
			};

			CodeNamespaceImport type1 = new CodeNamespaceImport("Namespace1");
			codeNamespace.Imports.Add(type1);
			Assert.Equal(new CodeNamespaceImport[] { type1 }, codeNamespace.Imports.Cast<CodeNamespaceImport>());
			Assert.True(calledPopulateImports);

			// Only calls PopulateImports once
			calledPopulateImports = false;
			CodeNamespaceImport type2 = new CodeNamespaceImport("Namespace2");
			codeNamespace.Imports.Add(type2);
			Assert.Equal(new CodeNamespaceImport[] { type1, type2 }, codeNamespace.Imports.Cast<CodeNamespaceImport>());
			Assert.False(calledPopulateImports);
		}

		[Fact]
		public void Comments_AddMultiple_ReturnsExpected()
		{
			var codeNamespace = new CodeNamespace();

			CodeCommentStatement comment1 = new CodeCommentStatement("Comment1");
			codeNamespace.Comments.Add(comment1);
			Assert.Equal(new CodeCommentStatement[] { comment1 }, codeNamespace.Comments.Cast<CodeCommentStatement>());

			CodeCommentStatement comment2 = new CodeCommentStatement("Comment2");
			codeNamespace.Comments.Add(comment2);
			Assert.Equal(new CodeCommentStatement[] { comment1, comment2 }, codeNamespace.Comments.Cast<CodeCommentStatement>());
		}

		[Fact]
		public void Comments_Get_CallsPopulateImportsONce()
		{
			var codeNamespace = new CodeNamespace();
			bool calledPopulateComments = false;
			codeNamespace.PopulateComments += (object sender, EventArgs args) =>
			{
				calledPopulateComments = true;
				Assert.Same(codeNamespace, sender);
				Assert.Equal(EventArgs.Empty, args);
			};

			CodeCommentStatement comment1 = new CodeCommentStatement("Comment1");
			codeNamespace.Comments.Add(comment1);
			Assert.Equal(new CodeCommentStatement[] { comment1 }, codeNamespace.Comments.Cast<CodeCommentStatement>());
			Assert.True(calledPopulateComments);

			// Only calls PopulateComments once
			calledPopulateComments = false;
			CodeCommentStatement comment2 = new CodeCommentStatement("Comment2");
			codeNamespace.Comments.Add(comment2);
			Assert.Equal(new CodeCommentStatement[] { comment1, comment2 }, codeNamespace.Comments.Cast<CodeCommentStatement>());
			Assert.False(calledPopulateComments);
		}
	}
}
