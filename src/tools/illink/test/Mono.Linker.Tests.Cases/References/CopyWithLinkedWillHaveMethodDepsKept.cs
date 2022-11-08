using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.References.Dependencies;

namespace Mono.Linker.Tests.Cases.References
{
	[SetupCSharpCompilerToUse ("csc")]
	[SetupLinkerAction ("copy", "test")]
	[SetupCompileBefore ("linked.dll", new[] { typeof (WithLinked_Methods) })]

	[KeptMember (".ctor()")]
	[KeptMemberInAssembly ("linked.dll", typeof (WithLinked_Methods), nameof (WithLinked_Methods.UsedByPublic) + "()")]
	[KeptMemberInAssembly ("linked.dll", typeof (WithLinked_Methods), nameof (WithLinked_Methods.UsedByInternal) + "()")]
	[KeptMemberInAssembly ("linked.dll", typeof (WithLinked_Methods), nameof (WithLinked_Methods.UsedByPrivate) + "()")]
	public class CopyWithLinkedWillHaveMethodDepsKept
	{
		public static void Main ()
		{
		}

		[Kept]
		public static void UnusedPublic ()
		{
			WithLinked_Methods.UsedByPublic ();
		}

		[Kept]
		internal static void UnusedInternal ()
		{
			WithLinked_Methods.UsedByInternal ();
		}

		[Kept]
		static void UnusedPrivate ()
		{
			WithLinked_Methods.UsedByPrivate ();
		}
	}
}