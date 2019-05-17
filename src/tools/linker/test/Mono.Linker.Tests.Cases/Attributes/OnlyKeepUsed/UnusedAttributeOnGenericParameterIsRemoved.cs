using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed {
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupCompileBefore ("library.dll", new [] { "Dependencies/AssemblyWithUnusedAttributeOnGenericParameter.il" })]
	[RemovedTypeInAssembly ("library.dll", "Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed.Dependencies.FooAttribute")]
	public class UnusedAttributeOnGenericParameterIsRemoved {
		static void Main ()
		{
#if IL_ASSEMBLY_AVAILABLE
			var tmp = new Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed.Dependencies.GenericType<int> (8).Method ();
			var tmp2 = new Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed.Dependencies.TypeWithGenericMethod ().GenericMethod<int> (9);
#endif
		}
	}
}