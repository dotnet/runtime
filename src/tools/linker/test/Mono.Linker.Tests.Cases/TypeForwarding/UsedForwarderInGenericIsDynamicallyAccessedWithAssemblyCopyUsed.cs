// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TypeForwarding.Dependencies;

namespace Mono.Linker.Tests.Cases.TypeForwarding
{

	[SetupLinkerAction ("copyused", "Forwarder")]

	[SetupCompileBefore ("Forwarder.dll", new[] { "Dependencies/ReferenceImplementationLibrary.cs" }, defines: new[] { "INCLUDE_REFERENCE_IMPL" })]

	// After compiling the test case we then replace the reference impl with implementation + type forwarder
	[SetupCompileAfter ("Implementation.dll", new[] { "Dependencies/ImplementationLibrary.cs" })]
	[SetupCompileAfter ("Forwarder.dll", new[] { "Dependencies/ForwarderLibrary.cs" }, references: new[] { "Implementation.dll" })]

	[KeptTypeInAssembly ("Forwarder.dll", typeof (ImplementationLibrary))]
	[KeptTypeInAssembly ("Implementation.dll", typeof (ImplementationLibrary))]

	[KeptTypeInAssembly ("Forwarder.dll", typeof (ImplementationLibraryImp))]
	[KeptTypeInAssembly ("Implementation.dll", typeof (ImplementationLibraryImp))]
	class UsedForwarderInGenericIsDynamicallyAccessedWithAssemblyCopyUsed
	{
		static void Main ()
		{
			PointToTypeInFacade ("Mono.Linker.Tests.Cases.TypeForwarding.OuterGeneric`1[[Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ImplementationLibrary, Forwarder]], test");

			Activator.CreateInstance ("test", "Mono.Linker.Tests.Cases.TypeForwarding.OuterGenericForCreateInstance`1[[Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ImplementationLibraryImp, Forwarder]]");
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

	[Kept]
	class OuterGenericForCreateInstance<T>
	{
		[Kept]
		public OuterGenericForCreateInstance () { }
	}
}
