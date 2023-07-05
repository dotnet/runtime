using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

#if RootLibraryVisibleForwarders
[assembly: TypeForwardedTo (typeof (ExternalPublic))]
#endif

namespace Mono.Linker.Tests.Cases.Libraries
{
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
