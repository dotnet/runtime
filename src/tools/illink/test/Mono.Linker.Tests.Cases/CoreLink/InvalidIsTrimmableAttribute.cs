using Mono.Linker.Tests.Cases.CoreLink.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CoreLink
{
	[SetupLinkerDefaultActionAttribute ("copy")]
	[SetupCompileBefore ("trimmable.dll", new[] { "Dependencies/InvalidIsTrimmableAssembly.cs" })]

	[KeptMemberInAssembly ("trimmable.dll", typeof (InvalidIsTrimmableAssembly), "Used()")]
	[RemovedMemberInAssembly ("trimmable.dll", typeof (InvalidIsTrimmableAssembly), "Unused()")]
	[ExpectedWarning ("IL2102", "Invalid AssemblyMetadata(\"IsTrimmable\", \"False\") attribute in assembly 'trimmable'. Value must be \"True\"", FileName = "trimmable.dll")]
	[KeptMember (".ctor()")]
	public class InvalidIsTrimmableAttribute
	{
		public static void Main ()
		{
			Used ();
			InvalidIsTrimmableAssembly.Used ();
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
