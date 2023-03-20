// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	[SetupCompileArgument ("/optimize+")]
	[Define ("RELEASE")]
	[SetupCompileArgument ("/main:Mono.Linker.Tests.Cases.RequiresCapability.RequiresInCompilerGeneratedCodeRelease")]
	[SandboxDependency ("RequiresInCompilerGeneratedCode.cs")]
	class RequiresInCompilerGeneratedCodeRelease
	{
		// This test just links the RequiresIncompilerGeneratedCode test in the Release configuration, to test
		// with optimizations enabled for closures and state machine types.
		// Sometimes the compiler optimizes away unused references to lambdas.
		public static void Main ()
		{
			RequiresInCompilerGeneratedCode.Main ();
		}
	}
}