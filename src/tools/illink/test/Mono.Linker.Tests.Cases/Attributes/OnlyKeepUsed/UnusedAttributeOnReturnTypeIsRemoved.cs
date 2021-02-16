using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed
{
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/AssemblyWithUnusedAttributeOnReturnParameterDefinition.il" })]
	[RemovedTypeInAssembly ("library.dll", "Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed.Dependencies.AssemblyWithUnusedAttributeOnReturnParameterDefinition/FooAttribute")]
	[RemovedTypeInAssembly ("library.dll", "Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed.Dependencies.BarAttribute")]
	public class UnusedAttributeOnReturnTypeIsRemoved
	{
		static void Main ()
		{
#if IL_ASSEMBLY_AVAILABLE
			var tmp = new Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed.Dependencies.AssemblyWithUnusedAttributeOnReturnParameterDefinition ().Method (1);
			var tmp2 = new Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed.Dependencies.AssemblyWithUnusedAttributeOnReturnParameterDefinition ().MethodWithoutParameters ();
			var tmp3 = new Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed.Dependencies.AssemblyWithUnusedAttributeOnReturnParameterDefinition ().MethodWithoutParametersNonNestedAttribute ();
#endif
		}
	}
}