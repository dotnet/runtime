// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.CppCLI.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CppCLI
{
	[IgnoreTestCase ("Test relies on checked-in binaries: https://github.com/dotnet/runtime/issues/78344")]
	[ReferenceDependency ("Dependencies/TestLibrary.dll")]
	[SetupLinkerArgument ("--skip-unresolved", "true")]

	[SetupCompileBefore ("ManagedSide.dll", new[] { "Dependencies/CallCppCLIFromManagedRef.cs" })]
	[SetupCompileAfter ("ManagedSide.dll", new[] { "Dependencies/CallCppCLIFromManaged.cs" }, references: new[] { "TestLibrary.dll" })]

	[LogContains ("Warn from C++/CLI")]
	[KeptAssembly ("TestLibrary.dll")]

	[Kept]
	public class CppCLIAssemblyIsAnalyzed
	{
		public static void Main ()
		{
			CallCppCLIFromManaged.TriggerWarning ();
		}
	}
}
