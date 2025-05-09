// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.TypeForwarding;

[SetupCompileBefore ("Forwarder.dll", new[] { "Dependencies/ForwardedNestedTypeLibrary.cs" }, defines: new[] { "INCLUDE_REFERENCE_IMPL" })]
[SetupCompileBefore ("Implementation.dll", new[] { "Dependencies/ForwardedNestedTypeLibrary.cs" }, defines: new[] { "INCLUDE_REFERENCE_IMPL" })]
[SetupCompileAfter ("Forwarder.dll", new[] { "Dependencies/ForwardedNestedTypeLibrary.cs" }, references: new[] { "Implementation.dll" }, defines: new[] { "INCLUDE_FORWARDERS" })]
[SetupLinkerDescriptorFile("NestedTypeForwarder.xml")]
[KeptAssembly("Forwarder.dll")]
[KeptTypeInAssembly("Forwarder.dll", "Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ForwardedNestedTypeLibrary")]
[KeptTypeInAssembly("Forwarder.dll", "Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ForwardedNestedTypeLibrary/NestedOne")]
[KeptTypeInAssembly("Forwarder.dll", "Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ForwardedNestedTypeLibrary/NestedOne/NestedTwo")]
[KeptTypeInAssembly("Forwarder.dll", "Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ForwardedNestedTypeLibrary/NestedOne/NestedTwo/NestedThree")]
public class NestedTypeForwarder
{
	public static void Main ()
	{

	}
}
