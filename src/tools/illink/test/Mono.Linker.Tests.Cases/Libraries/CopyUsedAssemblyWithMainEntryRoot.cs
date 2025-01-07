using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Libraries.Dependencies;

namespace Mono.Linker.Tests.Cases.Libraries
{
	[IgnoreTestCase ("NativeAOT doesn't implement copy used behavior", IgnoredBy = Tool.NativeAot)]
	[KeptAttributeAttribute (typeof (IgnoreTestCaseAttribute), By = Tool.Trimmer)]
	[Kept]
	[KeptMember (".ctor()")]
	[SetupLinkerAction ("copyused", "test")]
	[SetupCompileBefore ("lib.dll", new[] { "Dependencies/CopyUsedAssemblyWithMainEntryRoot_Lib.cs" })]
	[KeptMemberInAssembly ("lib.dll", typeof (CopyUsedAssemblyWithMainEntryRoot_Lib), "Used()")]
	// Marked CopyUsed assemblies are not fully marked like Copy assemblies, so the Unused dependency is not kept.
	[RemovedMemberInAssembly ("lib.dll", typeof (CopyUsedAssemblyWithMainEntryRoot_Lib), "Unused()")]
	public class CopyUsedAssemblyWithMainEntryRoot
	{
		[Kept]
		public static void Main ()
		{
			CopyUsedAssemblyWithMainEntryRoot_Lib.Used ();
		}

		[Kept]
		public void UnusedPublicMethod ()
		{
		}

		[Kept]
		private void UnusedPrivateMethod ()
		{
			CopyUsedAssemblyWithMainEntryRoot_Lib.Unused ();
		}
	}
}
