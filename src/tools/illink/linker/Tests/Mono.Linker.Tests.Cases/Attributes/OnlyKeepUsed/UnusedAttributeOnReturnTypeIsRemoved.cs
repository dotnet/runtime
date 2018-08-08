using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed {
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupCompileBefore ("library.dll", new [] { "Dependencies/AssemblyWithUnusedAttributeOnReturnParameterDefinition.il" })]
	[RemovedTypeInAssembly ("library.dll", "Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed.Dependencies.AssemblyWithUnusedAttributeOnReturnParameterDefinition/FooAttribute")]
	public class UnusedAttributeOnReturnTypeIsRemoved {
		static void Main ()
		{
#if IL_ASSEMBLY_AVAILABLE
			var tmp = new Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed.Dependencies.AssemblyWithUnusedAttributeOnReturnParameterDefinition ().Method (1);
#endif
		}
	}
}