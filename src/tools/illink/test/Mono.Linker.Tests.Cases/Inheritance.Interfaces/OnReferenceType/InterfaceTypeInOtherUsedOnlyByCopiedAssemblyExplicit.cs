using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Inheritance.Interfaces.Dependencies;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType
{
	[SetupCompileBefore ("linked.dll", new[] { typeof (InterfaceTypeInOtherUsedOnlyByCopiedAssemblyExplicit_Link) })]
	[SetupCompileBefore ("copy.dll", new[] { typeof (InterfaceTypeInOtherUsedOnlyByCopiedAssemblyExplicit_Copy) }, new[] { "linked.dll" })]

	[SetupLinkerAction ("copy", "copy")]
	[SetupLinkerArgument ("-a", "copy.dll")]

	[KeptMemberInAssembly ("linked.dll", typeof (InterfaceTypeInOtherUsedOnlyByCopiedAssemblyExplicit_Link.IFoo), "Method()")]
	[KeptMemberInAssembly ("copy.dll", typeof (InterfaceTypeInOtherUsedOnlyByCopiedAssemblyExplicit_Copy.A), "Mono.Linker.Tests.Cases.Inheritance.Interfaces.Dependencies.InterfaceTypeInOtherUsedOnlyByCopiedAssemblyExplicit_Link.IFoo.Method()")]
	[KeptInterfaceOnTypeInAssembly ("copy.dll", typeof (InterfaceTypeInOtherUsedOnlyByCopiedAssemblyExplicit_Copy.A), "linked.dll", typeof (InterfaceTypeInOtherUsedOnlyByCopiedAssemblyExplicit_Link.IFoo))]
	public class InterfaceTypeInOtherUsedOnlyByCopiedAssemblyExplicit
	{
		public static void Main ()
		{
			InterfaceTypeInOtherUsedOnlyByCopiedAssemblyExplicit_Copy.ToKeepReferenceAtCompileTime ();
		}
	}
}