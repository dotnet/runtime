// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TypeForwarding.Dependencies;

namespace Mono.Linker.Tests.Cases.TypeForwarding
{

	[SetupLinkerAction ("copyused", "Forwarder")]
	[KeepTypeForwarderOnlyAssemblies ("false")]

	[SetupCompileBefore ("Forwarder.dll", new[] { "Dependencies/ReferenceImplementationLibrary.cs" }, defines: new[] { "INCLUDE_REFERENCE_IMPL" })]

	// After compiling the test case we then replace the reference impl with implementation + type forwarder
	[SetupCompileAfter ("Implementation.dll", new[] { "Dependencies/ImplementationLibrary.cs" })]
	[SetupCompileAfter ("Forwarder.dll", new[] { "Dependencies/ForwarderLibrary.cs" }, references: new[] { "Implementation.dll" })]

	// https://github.com/mono/linker/issues/1536
	//[KeptMemberInAssembly ("Forwarder.dll", typeof (ImplementationLibrary))]
	[KeptMemberInAssembly ("Implementation.dll", typeof (ImplementationLibrary))]
	class UsedForwarderInGenericIsDynamicallyAccessedWithAssemblyCopyUsed
	{
		static void Main ()
		{
			PointToTypeInFacade ("Mono.Linker.Tests.Cases.TypeForwarding.OuterGeneric`1[[Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ImplementationLibrary, Forwarder]], test");
		}

		[Kept]
		static void PointToTypeInFacade (
			[KeptAttributeAttribute (typeof(DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] string typeName)
		{
		}
	}

	[Kept]
	class OuterGeneric<T>
	{
		[Kept]
		public OuterGeneric () { }
	}
}
