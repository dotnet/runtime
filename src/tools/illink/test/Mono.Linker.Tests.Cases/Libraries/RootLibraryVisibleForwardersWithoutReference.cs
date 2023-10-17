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

	[SetupCompileBefore ("library.dll", new[] { "Dependencies/RootLibraryVisibleForwarders_Lib.cs" }, outputSubFolder: "isolated")]
	[SetupLinkerLinkPublicAndFamily]
	[SetupLinkerArgument ("-a", "isolated/library.dll", "visible")] // Checks for no-eager exported type resolving
	[Define ("RootLibraryVisibleForwarders")]

	[Kept]
	[KeptMember (".ctor()")]
	[KeptExportedType (typeof (ExternalPublic))]
	public class RootLibraryVisibleForwardersWithoutReference
	{
		[Kept]
		public static void Main ()
		{
		}
	}
}
