// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.CodeDom.Tests
{
	public class CodeCompileUnitTests : CodeObjectTestBase<CodeCompileUnit>
	{
		[Fact]
		public void Ctor_Default()
		{
			var compileUnit = new CodeCompileUnit();
			Assert.Empty(compileUnit.Namespaces);
			Assert.Empty(compileUnit.ReferencedAssemblies);
			Assert.Empty(compileUnit.AssemblyCustomAttributes);
			Assert.Empty(compileUnit.StartDirectives);
			Assert.Empty(compileUnit.EndDirectives);
		}

		[Fact]
		public void Namespaces_AddMultiple_ReturnsExpected()
		{
			var compileUnit = new CodeCompileUnit();

			CodeNamespace namespace1 = new CodeNamespace("Name1");
			compileUnit.Namespaces.Add(namespace1);
			Assert.Equal(new CodeNamespace[] { namespace1 }, compileUnit.Namespaces.Cast<CodeNamespace>());

			CodeNamespace namespace2 = new CodeNamespace("Name2");
			compileUnit.Namespaces.Add(namespace2);
			Assert.Equal(new CodeNamespace[] { namespace1, namespace2 }, compileUnit.Namespaces.Cast<CodeNamespace>());
		}

		[Fact]
		public void ReferencedAssemblies_AddMultiple_ReturnsExpected()
		{
			var compileUnit = new CodeCompileUnit();

			string assembly1 = "Name1";
			compileUnit.ReferencedAssemblies.Add(assembly1);
			Assert.Equal(new string[] { assembly1 }, compileUnit.ReferencedAssemblies.Cast<string>());

			string assembly2 = "Name2";
			compileUnit.ReferencedAssemblies.Add(assembly2);
			Assert.Equal(new string[] { assembly1, assembly2 }, compileUnit.ReferencedAssemblies.Cast<string>());
		}

		[Fact]
		public void CustomAttributes_AddMultiple_ReturnsExpected()
		{
			var compileUnit = new CodeCompileUnit();

			CodeAttributeDeclaration attribute1 = new CodeAttributeDeclaration("Name1");
			compileUnit.AssemblyCustomAttributes.Add(attribute1);
			Assert.Equal(new CodeAttributeDeclaration[] { attribute1 }, compileUnit.AssemblyCustomAttributes.Cast<CodeAttributeDeclaration>());

			CodeAttributeDeclaration attribute2 = new CodeAttributeDeclaration("Name2");
			compileUnit.AssemblyCustomAttributes.Add(attribute2);
			Assert.Equal(new CodeAttributeDeclaration[] { attribute1, attribute2 }, compileUnit.AssemblyCustomAttributes.Cast<CodeAttributeDeclaration>());
		}

		[Fact]
		public void StartDirectives_AddMultiple_ReturnsExpected()
		{
			var compileUnit = new CodeCompileUnit();

			CodeRegionDirective directive1 = new CodeRegionDirective(CodeRegionMode.None, "Region1");
			compileUnit.StartDirectives.Add(directive1);
			Assert.Equal(new CodeRegionDirective[] { directive1 }, compileUnit.StartDirectives.Cast<CodeDirective>());

			CodeRegionDirective directive2 = new CodeRegionDirective(CodeRegionMode.None, "Region2");
			compileUnit.StartDirectives.Add(directive2);
			Assert.Equal(new CodeRegionDirective[] { directive1, directive2 }, compileUnit.StartDirectives.Cast<CodeDirective>());
		}

		[Fact]
		public void EndDirectives_AddMultiple_ReturnsExpected()
		{
			var compileUnit = new CodeCompileUnit();

			CodeRegionDirective directive1 = new CodeRegionDirective(CodeRegionMode.None, "Region1");
			compileUnit.EndDirectives.Add(directive1);
			Assert.Equal(new CodeRegionDirective[] { directive1 }, compileUnit.EndDirectives.Cast<CodeDirective>());

			CodeRegionDirective directive2 = new CodeRegionDirective(CodeRegionMode.None, "Region2");
			compileUnit.EndDirectives.Add(directive2);
			Assert.Equal(new CodeRegionDirective[] { directive1, directive2 }, compileUnit.EndDirectives.Cast<CodeDirective>());
		}
	}
}
