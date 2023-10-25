using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

#if RootLibraryVisibleForwarders
[assembly: TypeForwardedTo (typeof (ExternalPublic))]
#endif

namespace Mono.Linker.Tests.Cases.Libraries
{
	[IgnoreTestCase ("NativeAOT doesn't implement library trimming the same way", IgnoredBy = Tool.NativeAot)]
	[KeptAttributeAttribute (typeof (IgnoreTestCaseAttribute), By = Tool.Trimmer)]

	[SetupCompileBefore ("library.dll", new[] { "Dependencies/RootLibraryVisibleForwarders_Lib.cs" })]
	[SetupLinkerLinkPublicAndFamily]
	[Define ("RootLibraryVisibleForwarders")]

	[Kept]
	[KeptMember (".ctor()")]
	[KeptExportedType (typeof (ExternalPublic))]
	[KeptMemberInAssembly ("library.dll", typeof (ExternalPublic), "ProtectedMethod()")]
	public class RootLibraryVisibleForwarders
	{
		[Kept]
		public static void Main ()
		{
		}
	}
}
