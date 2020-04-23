using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Inheritance.Interfaces.Dependencies;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType
{
	[SetupCompileBefore ("linked.dll", new[] { typeof (InterfaceTypeInOtherUsedOnlyByCopiedAssembly_Link) })]
	[SetupCompileBefore ("save.dll", new[] { typeof (InterfaceTypeInOtherUsedOnlyByCopiedAssembly_Copy) }, new[] { "linked.dll" })]

	[SetupLinkerAction ("save", "save")]
	[SetupLinkerArgument ("-a", "save")]

	[KeptTypeInAssembly ("linked.dll", typeof (InterfaceTypeInOtherUsedOnlyByCopiedAssembly_Link.IFoo))]
	[KeptMemberInAssembly ("save.dll", typeof (InterfaceTypeInOtherUsedOnlyByCopiedAssembly_Copy.A), "Method()")]
	[KeptInterfaceOnTypeInAssembly ("save.dll", typeof (InterfaceTypeInOtherUsedOnlyByCopiedAssembly_Copy.A), "linked.dll", typeof (InterfaceTypeInOtherUsedOnlyByCopiedAssembly_Link.IFoo))]
	public class InterfaceTypeInOtherUsedOnlyBySavedAssembly
	{
		public static void Main ()
		{
			InterfaceTypeInOtherUsedOnlyByCopiedAssembly_Copy.ToKeepReferenceAtCompileTime ();
		}
	}
}