// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TypeForwarding.Dependencies;

namespace Mono.Linker.Tests.Cases.TypeForwarding
{
	[SkipUnresolved (true)]

	[SetupCompileBefore ("NestedForwarderLibrary_2.dll", new[] { "Dependencies/ReferenceImplementationLibrary.cs" }, defines: new[] { "INCLUDE_REFERENCE_IMPL" })]

	// After compiling the test case we then replace the reference impl with implementation + type forwarder
	[SetupCompileAfter ("Implementation.dll", new[] { "Dependencies/ImplementationLibrary.cs" })]
	[SetupCompileAfter ("NestedForwarderLibrary.dll", new[] { "Dependencies/NestedForwarderLibrary.il" }, references: new[] { "Implementation.dll" })]
	[SetupCompileAfter ("NestedForwarderLibrary_2.dll", new[] { "Dependencies/NestedForwarderLibrary_2.il" }, references: new[] { "NestedForwarderLibrary.dll" })]

	[SetupLinkerAction ("copy", "test")]
	[SetupLinkerAction ("copy", "NestedForwarderLibrary_2")]
	[SetupLinkerAction ("link", "NestedForwarderLibrary")]
	[SetupLinkerAction ("link", "Implementation")]

	[KeptTypeInAssembly ("NestedForwarderLibrary.dll", typeof (ImplementationLibrary.ForwardedNestedType))]
	[KeptTypeInAssembly ("NestedForwarderLibrary.dll", typeof (AnotherImplementationClass.ForwardedNestedType))]
	[KeptTypeInAssembly ("NestedForwarderLibrary_2.dll", typeof (ImplementationLibrary.ForwardedNestedType))]
	[KeptTypeInAssembly ("NestedForwarderLibrary_2.dll", typeof (AnotherImplementationClass.ForwardedNestedType))]
	[KeptTypeInAssembly ("Implementation.dll", typeof (ImplementationLibrary.ForwardedNestedType))]
	[KeptTypeInAssembly ("Implementation.dll", typeof (AnotherImplementationClass.ForwardedNestedType))]

	[KeptMember (".ctor()")]
	class MultiForwardedTypesWithLink
	{
		static void Main ()
		{
			Console.WriteLine (typeof (ImplementationLibrary.ForwardedNestedType).FullName);
			Console.WriteLine (typeof (AnotherImplementationClass.ForwardedNestedType).FullName);
		}
	}
}
