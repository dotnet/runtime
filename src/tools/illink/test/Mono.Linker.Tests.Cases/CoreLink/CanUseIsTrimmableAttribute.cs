using Mono.Linker.Tests.Cases.CoreLink.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CoreLink
{
	[SetupLinkerDefaultActionAttribute ("copy")]
	[SetupCompileBefore ("trimmable.dll", new[] { "Dependencies/TrimmableAssembly.cs" })]
	[SetupCompileBefore ("nontrimmable.dll", new[] { "Dependencies/NonTrimmableAssembly.cs" })]

	[KeptMemberInAssembly ("trimmable.dll", typeof (TrimmableAssembly), "Used()")]
	[RemovedMemberInAssembly ("trimmable.dll", typeof (TrimmableAssembly), "Unused()")]
	[KeptAllTypesAndMembersInAssembly ("nontrimmable.dll")]
	[KeptMember (".ctor()")]
	public class CanUseIsTrimmableAttribute
	{
		public static void Main ()
		{
			Used ();
			TrimmableAssembly.Used ();
			NonTrimmableAssembly.Used ();
		}

		[Kept]
		public static void Used ()
		{
		}

		[Kept]
		public static void Unused ()
		{
		}
	}
}