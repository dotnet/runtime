// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.TypeForwarding
{
	/// <summary>
	/// This test depends on a functioning peverify / il verify in order to fail if the scope of type references on security attributes
	/// were not correctly updated.
	///
	/// In order words, until https://github.com/dotnet/linker/issues/1703 is addressed this test will pass with or without the fix to update the scope on security attributes
	/// </summary>
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[SetupLinkerArgument ("--strip-security", "false")]
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupLinkerAction ("copy", "Library")]
	[SetupCompileBefore ("Forwarder.dll", new[] { "Dependencies/SecurityAttributeForwarderLibrary.cs" })]
	[SetupCompileBefore ("Library.dll", new[] { "Dependencies/LibraryWithSecurityAttributes.il" }, new[] { "Forwarder.dll" })]

	// Sanity checks to verify the test was setup correctly
	[KeptTypeInAssembly ("Library.dll", "LibraryWithSecurityAttributes")]
	// There's a reference to `Forwarder` in the copy assembly `Library`.
	[KeptAssembly ("Forwarder.dll")]
	public class SecurityAttributeScope
	{
		public static void Main ()
		{
#if IL_ASSEMBLY_AVAILABLE
			new LibraryWithSecurityAttributes().OnAMethod ();
#endif
		}
	}
}
