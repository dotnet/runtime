// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class RequiresCapabilityTests : LinkerTestBase
	{
		protected override string TestSuiteName => "RequiresCapability";

		[Fact]
		public Task BasicRequires ()
		{
			return RunTest (nameof (BasicRequires));
		}

		[Fact]
		public Task ReflectionAccessFromCompilerGeneratedCode ()
		{
			return RunTest ();
		}

		[Fact]
		public Task RequiresAccessedThrough ()
		{
			return RunTest (nameof (RequiresAccessedThrough));
		}

		[Fact]
		public Task RequiresAttributeMismatch ()
		{
			return RunTest (nameof (RequiresAttributeMismatch));
		}

		[Fact]
		public Task RequiresCapabilityFromCopiedAssembly ()
		{
			return RunTest (nameof (RequiresCapabilityFromCopiedAssembly));
		}

		[Fact]
		public Task RequiresCapabilityReflectionAnalysisEnabled ()
		{
			return RunTest (nameof (RequiresCapabilityReflectionAnalysisEnabled));
		}

		[Fact]
		public Task RequiresInCompilerGeneratedCode ()
		{
			return RunTest (nameof (RequiresInCompilerGeneratedCode));
		}

		[Fact]
		public Task RequiresInCompilerGeneratedCodeRelease ()
		{
			return RunTest ();
		}

		[Fact]
		public Task RequiresInLibraryAssembly ()
		{
			return RunTest ();
		}

		[Fact]
		public Task RequiresOnAttribute ()
		{
			return RunTest (nameof (RequiresOnAttribute));
		}

		[Fact]
		public Task RequiresOnAttributeCtor ()
		{
			return RunTest (nameof (RequiresOnAttributeCtor));
		}

		[Fact]
		public Task RequiresOnClass ()
		{
			return RunTest (nameof (RequiresOnClass));
		}

		[Fact]
		public Task RequiresOnStaticConstructor ()
		{
			return RunTest (nameof (RequiresOnStaticConstructor));
		}

		[Fact]
		public Task RequiresOnVirtualsAndInterfaces ()
		{
			return RunTest (nameof (RequiresOnVirtualsAndInterfaces));
		}

		[Fact]
		public Task RequiresViaDataflow ()
		{
			return RunTest (nameof (RequiresViaDataflow));
		}

		[Fact]
		public Task RequiresViaXml ()
		{
			return RunTest (nameof (RequiresViaXml));
		}

		[Fact]
		public Task RequiresWithCopyAssembly ()
		{
			return RunTest (nameof (RequiresWithCopyAssembly));
		}

		[Fact]
		public Task SuppressRequires ()
		{
			return RunTest (nameof (SuppressRequires));
		}
	}
}
