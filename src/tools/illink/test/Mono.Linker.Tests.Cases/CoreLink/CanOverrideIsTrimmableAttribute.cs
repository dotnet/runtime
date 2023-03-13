using Mono.Linker.Tests.Cases.CoreLink.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CoreLink
{
	[SetupLinkerDefaultActionAttribute ("copy")]
	[SetupLinkerAction ("link", "test")]
	[SetupLinkerAction ("copy", "trimmable")]
	[SetupLinkerAction ("link", "nontrimmable")]

	[SetupCompileBefore ("trimmable.dll", new[] { "Dependencies/TrimmableAssembly.cs" })]
	[SetupCompileBefore ("nontrimmable.dll", new[] { "Dependencies/NonTrimmableAssembly.cs" })]

	[KeptAllTypesAndMembersInAssembly ("trimmable.dll")]
	[KeptMemberInAssembly ("nontrimmable.dll", typeof (NonTrimmableAssembly), "Used()")]
	[RemovedMemberInAssembly ("nontrimmable.dll", typeof (NonTrimmableAssembly), "Unused()")]
	public class CanOverrideIsTrimmableAttribute
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

		public static void Unused ()
		{
		}
	}
}