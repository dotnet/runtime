// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
			return RunTest ();
		}

		[Fact]
		public Task ConstructorUsedViaReflection ()
		{
			return RunTest ();
		}

		[Fact]
		public Task EventUsedViaReflection ()
		{
			return RunTest ();
		}

		[Fact]
		public Task EventsUsedViaReflection ()
		{
			return RunTest ();
		}

		[Fact]
		public Task ExpressionCallString ()
		{
			// https://github.com/dotnet/linker/issues/2578
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ExpressionFieldString ()
		{
			return RunTest ();
		}

		[Fact]
		public Task ExpressionNewType ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task ExpressionPropertyMethodInfo ()
		{
			return RunTest ();
		}

		[Fact]
		public Task ExpressionPropertyString ()
		{
			return RunTest ();
		}

		[Fact]
		public Task FieldUsedViaReflection ()
		{
			return RunTest ();
		}

		[Fact]
		public Task FieldsUsedViaReflection ()
		{
			return RunTest ();
		}

		[Fact]
		public Task MembersUsedViaReflection ()
		{
			return RunTest ();
		}

		[Fact]
		public Task MemberUsedViaReflection ()
		{
			return RunTest ();
		}

		[Fact]
		public Task MethodUsedViaReflection ()
		{
			return RunTest ();
		}

		[Fact]
		public Task MethodUsedViaReflectionAndLocal ()
		{
			return RunTest ();
		}

		[Fact]
		public Task MethodUsedViaReflectionWithDefaultBindingFlags ()
		{
			return RunTest ();
		}

		[Fact]
		public Task MethodsUsedViaReflection ()
		{
			return RunTest ();
		}

		[Fact]
		public Task NestedTypeUsedViaReflection ()
		{
			return RunTest ();
		}

		[Fact]
		public Task NestedTypesUsedViaReflection ()
		{
			return RunTest ();
		}

		[Fact]
		public Task ObjectGetType ()
		{
			// https://github.com/dotnet/linker/issues/2578
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task PropertyUsedViaReflection ()
		{
			return RunTest ();
		}

		[Fact]
		public Task PropertiesUsedViaReflection ()
		{
			return RunTest ();
		}

		[Fact]
		public Task RuntimeReflectionExtensionsCalls ()
		{
			return RunTest ();
		}

		[Fact]
		public Task TypeBaseTypeUseViaReflection ()
		{
			return RunTest ();
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
