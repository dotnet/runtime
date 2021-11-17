// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

	// https://github.com/dotnet/linker/issues/2359
	// One of the type forwarders in NestedForwarderLibrary will not be kept.
	// Which one depends on order.
	//[KeptTypeInAssembly ("NestedForwarderLibrary.dll", typeof (ImplementationLibrary.ForwardedNestedType))]
	//[KeptTypeInAssembly ("NestedForwarderLibrary.dll", typeof (AnotherImplementationClass.ForwardedNestedType))]
	//[KeptTypeInAssembly ("NestedForwarderLibrary_2.dll", typeof (ImplementationLibrary.ForwardedNestedType))]
	//[KeptTypeInAssembly ("NestedForwarderLibrary_2.dll", typeof (AnotherImplementationClass.ForwardedNestedType))]
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
