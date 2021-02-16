// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.TypeForwarding
{
	/// <summary>
	/// This test depends on a functioning peverify / il verify in order to fail if the scope of type references on security attributes
	/// were not correctly updated.
	///
	/// In order words, until https://github.com/mono/linker/issues/1703 is addressed this test will pass with or without the fix to update the scope on security attributes
	/// </summary>
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[SetupLinkerArgument ("--strip-security", "false")]
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[KeepTypeForwarderOnlyAssemblies ("false")]
	[SetupLinkerAction ("copy", "Library.dll")]
	[SetupCompileBefore ("Forwarder.dll", new[] { "Dependencies/SecurityAttributeForwarderLibrary.cs" })]
	[SetupCompileBefore ("Library.dll", new[] { "Dependencies/LibraryWithSecurityAttributes.il" }, new[] { "Forwarder.dll" })]

	// Sanity checks to verify the test was setup correctly
	[KeptTypeInAssembly ("Library.dll", "LibraryWithSecurityAttributes")]
	[RemovedAssembly ("Forwarder.dll")]
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