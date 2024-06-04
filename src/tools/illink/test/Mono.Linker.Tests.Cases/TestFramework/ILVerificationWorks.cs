// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.TestFramework;

[DisableILVerifyDiffing] // Needed to produce an il failure
[ExpectILFailure ("Mono.Linker.Tests.Cases.TestFramework.Dependencies.AssemblyWithInvalidIL.GiveMeAValue()",
	"ReturnMissing: Return value missing on the stack",
	"Offset IL_0000")]
[SetupLinkerArgument ("--skip-unresolved", "true")] // needed due to the mscorlib shim
[Define ("IL_ASSEMBLY_AVAILABLE")]
[SetupCompileBefore ("ILAssembly.dll", new[] { "Dependencies/AssemblyWithInvalidIL.il" })]
[KeptMemberInAssembly ("ILAssembly.dll", "Mono.Linker.Tests.Cases.TestFramework.Dependencies.AssemblyWithInvalidIL", "GiveMeAValue()")]
public class ILVerificationWorks
{
	public static void Main ()
	{
#if IL_ASSEMBLY_AVAILABLE
			System.Console.WriteLine (new Mono.Linker.Tests.Cases.TestFramework.Dependencies.AssemblyWithInvalidIL ().GiveMeAValue ());
#endif
	}
}
