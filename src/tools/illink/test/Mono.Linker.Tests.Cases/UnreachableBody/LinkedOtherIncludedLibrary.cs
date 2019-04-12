using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.UnreachableBody.Dependencies;

namespace Mono.Linker.Tests.Cases.UnreachableBody {
	[SetupLinkerArgument ("-r", "other")]
	[SetupCompileBefore ("other.dll", new [] {typeof (OtherAssembly)})]
	[KeptMemberInAssembly ("other.dll", typeof (OtherAssembly.Foo), "Method()")]
	[KeptMemberInAssembly ("other.dll", typeof (OtherAssembly.Foo), "UsedByMethod()")]
	[KeptMemberInAssembly ("other.dll", typeof (OtherAssembly), "UnusedSanityCheck()")]
	public class LinkedOtherIncludedLibrary {
		public static void Main()
		{
			UsedToMarkMethod (null);
		}

		[Kept]
		static void UsedToMarkMethod (OtherAssembly.Foo f)
		{
			f.Method ();
		}
	}
}