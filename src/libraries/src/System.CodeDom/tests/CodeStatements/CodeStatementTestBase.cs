// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.CodeDom.Tests
{
	public abstract class CodeStatementTestBase<T> : CodeObjectTestBase<T> where T : CodeStatement, new()
	{
		[Fact]
		public void Ctor_Default_StatementBase()
		{
			CodeStatement statement = new T();
			Assert.Null(statement.LinePragma);

			Assert.Empty(statement.StartDirectives);
			Assert.Empty(statement.EndDirectives);
		}

		[Theory]
		[MemberData(nameof(LinePragma_TestData))]
		public void LinePragma_Set_Get_ReturnsExpected(CodeLinePragma value)
		{
			CodeStatement statement = new T() { LinePragma = value };
			Assert.Equal(value, statement.LinePragma);
		}

		[Fact]
		public void StartDirectives_AddMultiple_ReturnsExpected()
		{
			CodeStatement statement = new T();

			CodeRegionDirective directive1 = new CodeRegionDirective(CodeRegionMode.None, "Region1");
			statement.StartDirectives.Add(directive1);
			Assert.Equal(new CodeRegionDirective[] { directive1 }, statement.StartDirectives.Cast<CodeDirective>());

			CodeRegionDirective directive2 = new CodeRegionDirective(CodeRegionMode.None, "Region2");
			statement.StartDirectives.Add(directive2);
			Assert.Equal(new CodeRegionDirective[] { directive1, directive2 }, statement.StartDirectives.Cast<CodeDirective>());
		}

		[Fact]
		public void EndDirectives_AddMultiple_ReturnsExpected()
		{
			CodeStatement statement = new T();

			CodeRegionDirective directive1 = new CodeRegionDirective(CodeRegionMode.None, "Region1");
			statement.EndDirectives.Add(directive1);
			Assert.Equal(new CodeRegionDirective[] { directive1 }, statement.EndDirectives.Cast<CodeDirective>());

			CodeRegionDirective directive2 = new CodeRegionDirective(CodeRegionMode.None, "Region2");
			statement.EndDirectives.Add(directive2);
			Assert.Equal(new CodeRegionDirective[] { directive1, directive2 }, statement.EndDirectives.Cast<CodeDirective>());
		}
	}
}
