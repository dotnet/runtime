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
	[SetupLinkerAction ("copy", "FirstForwarder")]

	[KeptTypeInAssembly ("FirstForwarder.dll", typeof (ImplementationLibrary))]
	// Dynamically accessing a type forwarder will cause the linker to mark the scope
	// of type pointed to as well as the resolved type.
	[KeptTypeInAssembly ("SecondForwarder.dll", typeof (ImplementationLibrary))]
	[KeptMemberInAssembly ("Implementation.dll", typeof (ImplementationLibrary), "GetSomeValue()")]
	class UsedTransitiveForwarderInCopyAssemblyIsDynamicallyAccessed
	{
		static void Main ()
		{
			// [copy]            [link]             [link]
			// FirstForwarder -> SecondForwarder -> Implementation
			PointToTypeInFacade ("Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ImplementationLibrary, FirstForwarder");
		}

		[Kept]
		static void PointToTypeInFacade (
			[KeptAttributeAttribute (typeof(DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] string typeName)
		{
		}
	}
}
