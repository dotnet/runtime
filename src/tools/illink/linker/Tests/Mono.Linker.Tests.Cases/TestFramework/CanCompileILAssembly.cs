using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TestFramework.Dependencies;

namespace Mono.Linker.Tests.Cases.TestFramework {
	[SetupCompileBefore ("ILAssembly.dll", new[] { "Dependencies/ILAssemblySample.il" })]
	[KeptMemberInAssembly ("ILAssembly.dll", typeof (ILAssemblySample), "GiveMeAValue()")]
	public class CanCompileILAssembly {
		static void Main ()
		{
			Console.WriteLine (new ILAssemblySample ().GiveMeAValue ());
		}
	}
}
