using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.TestFramework
{
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupCompileBefore ("ILAssembly.dll", new[] { "Dependencies/ILAssemblySample.il" })]
	[KeptMemberInAssembly ("ILAssembly.dll", "Mono.Linker.Tests.Cases.TestFramework.Dependencies.ILAssemblySample", "GiveMeAValue()")]
	public class CanCompileILAssembly
	{
		static void Main ()
		{
#if IL_ASSEMBLY_AVAILABLE
			Console.WriteLine (new Mono.Linker.Tests.Cases.TestFramework.Dependencies.ILAssemblySample ().GiveMeAValue ());
#endif
		}
	}
}
