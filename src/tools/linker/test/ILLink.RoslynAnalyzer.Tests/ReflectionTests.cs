// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class ReflectionTests : LinkerTestBase
	{
		protected override string TestSuiteName => "Reflection";

		[Fact]
		public Task ActivatorCreateInstance ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task AssemblyImportedViaReflectionWithSweptReferences ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ConstructorsUsedViaReflection ()
		{
			// https://github.com/dotnet/linker/issues/2578
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ConstructorUsedViaReflection ()
		{
			// https://github.com/dotnet/linker/issues/2578
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task EventsUsedViaReflection ()
		{
			// https://github.com/dotnet/linker/issues/2578
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ExpressionCallString ()
		{
			// https://github.com/dotnet/linker/issues/2578
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ExpressionNewType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ExpressionPropertyMethodInfo ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task FieldsUsedViaReflection ()
		{
			// https://github.com/dotnet/linker/issues/2578
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MembersUsedViaReflection ()
		{
			// https://github.com/dotnet/linker/issues/2578
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MemberUsedViaReflection ()
		{
			// https://github.com/dotnet/linker/issues/2578
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task MethodsUsedViaReflection ()
		{
			// https://github.com/dotnet/linker/issues/2578
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task NestedTypesUsedViaReflection ()
		{
			// https://github.com/dotnet/linker/issues/2578
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ObjectGetType ()
		{
			// https://github.com/dotnet/linker/issues/2578
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task PropertiesUsedViaReflection ()
		{
			// https://github.com/dotnet/linker/issues/2578
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeHierarchyReflectionWarnings ()
		{
			// https://github.com/dotnet/linker/issues/2578
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeHierarchySuppressions ()
		{
			// https://github.com/dotnet/linker/issues/2578
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeUsedViaReflection ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeUsedViaReflectionTypeDoesntExist ()
		{
			return RunTest (allowMissingWarnings: true);
		}
	}
}
