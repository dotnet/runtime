// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class ReflectionTests : LinkerTestBase
	{
		protected override string TestSuiteName => "Reflection";

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2578")]
		public Task ActivatorCreateInstance ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2578")]
		public Task AssemblyImportedViaReflectionWithSweptReferences ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2578")]
		public Task ConstructorsUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2578")]
		public Task ConstructorUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2578")]
		public Task EventsUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2578")]
		public Task ExpressionCallString ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2578")]
		public Task ExpressionNewType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2578")]
		public Task ExpressionPropertyMethodInfo ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2578")]
		public Task FieldsUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2578")]
		public Task MembersUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2578")]
		public Task MemberUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2578")]
		public Task MethodsUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2578")]
		public Task NestedTypesUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2578")]
		public Task ObjectGetType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2578")]
		public Task PropertiesUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2578")]
		public Task TypeHierarchyReflectionWarnings ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2578")]
		public Task TypeHierarchySuppressions ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2578")]
		public Task TypeUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact (Skip = "https://github.com/dotnet/linker/issues/2578")]
		public Task TypeUsedViaReflectionTypeDoesntExist ()
		{
			return RunTest (allowMissingWarnings: true);
		}
	}
}
