// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TypeForwarding.Dependencies;

namespace Mono.Linker.Tests.Cases.TypeForwarding
{
	[SetupCompileBefore ("SecondForwarder.dll", new[] { "Dependencies/ReferenceImplementationLibrary.cs" }, defines: new[] { "INCLUDE_REFERENCE_IMPL" })]
	[SetupCompileBefore ("FirstForwarder.dll", new[] { "Dependencies/ForwarderLibrary.cs" }, references: new[] { "SecondForwarder.dll" })]

	// After compiling the test case we then replace the reference impl with implementation + type forwarder
	[SetupCompileAfter ("Implementation.dll", new[] { "Dependencies/ImplementationLibrary.cs" })]
	[SetupCompileAfter ("SecondForwarder.dll", new[] { "Dependencies/ForwarderLibrary.cs" }, references: new[] { "Implementation.dll" })]
	[SetupLinkerAction ("copy", "Implementation")]

	[KeptMemberInAssembly ("Implementation.dll", typeof (ImplementationLibrary), "GetSomeValue()")]
	[RemovedMemberInAssembly ("Implementation.dll", nameof (ImplementationStruct))]
	class UsedTransitiveForwarderIsResolvedAndFacadeRemovedInCopyAssembly
	{
		static void Main ()
		{
			var instance = new ImplementationLibrary ();
			instance.GetSomeValue ();
		}
	}
}
