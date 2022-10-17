using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBody
{
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[Define ("OTHER_INCLUDED")]
	[SetupLinkerAction ("copy", "other")]
	[SetupCompileBefore ("other.dll", new[] { "Dependencies/OtherAssemblyNoInstanceCtor.il" })]
	[KeptMemberInAssembly ("other.dll", "Mono.Linker.Tests.Cases.UnreachableBody.Dependencies.OtherAssemblyNoInstanceCtor/Foo", "Method()")]
	[KeptMemberInAssembly ("other.dll", "Mono.Linker.Tests.Cases.UnreachableBody.Dependencies.OtherAssemblyNoInstanceCtor/Foo", "UsedByMethod()")]
	[KeptMemberInAssembly ("other.dll", "Mono.Linker.Tests.Cases.UnreachableBody.Dependencies.OtherAssemblyNoInstanceCtor", "UnusedSanityCheck()")]
	[SetupLinkerArgument ("--enable-opt", "unreachablebodies")]
	public class DoesNotApplyToCopiedAssembly2
	{
		public static void Main ()
		{
#if OTHER_INCLUDED
			UsedToMarkMethod (null);
#endif
		}

#if OTHER_INCLUDED
		[Kept]
		static void UsedToMarkMethod (Mono.Linker.Tests.Cases.UnreachableBody.Dependencies.OtherAssemblyNoInstanceCtor.Foo f)
		{
			f.Method ();
		}
#endif
	}
}