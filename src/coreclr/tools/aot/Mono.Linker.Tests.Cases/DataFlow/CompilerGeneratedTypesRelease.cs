// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	[SetupCompileArgument ("/optimize+")]
	[SetupCompileArgument ("/main:Mono.Linker.Tests.Cases.DataFlow.CompilerGeneratedTypesRelease")]
	[SandboxDependency ("CompilerGeneratedTypes.cs")]
	class CompilerGeneratedTypesRelease
	{
		// This test just links the CompilerGeneratedTypes test in the Release configuration, to test
		// different compilation strategies for closures and state machine types.
		// Sometimes the compiler produces classes in Debug mode and structs in Release mode.
		public static void Main ()
		{
			CompilerGeneratedTypes.Main ();
		}
	}
}