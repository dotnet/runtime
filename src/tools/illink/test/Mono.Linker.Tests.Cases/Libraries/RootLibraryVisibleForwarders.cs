using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

#if RootLibraryVisibleForwarders
[assembly: TypeForwardedTo (typeof (ExternalPublic))]
#endif

namespace Mono.Linker.Tests.Cases.Libraries
{
#if !NETCOREAPP
	[IgnoreTestCase ("Build with illink")]
#endif
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/RootLibraryVisibleForwarders_Lib.cs" })]
	[SetupLinkerLinkPublicAndFamily]
	[Define ("RootLibraryVisibleForwarders")]

	[Kept]
	[KeptMember (".ctor()")]
	[KeptExportedType (typeof (ExternalPublic))]
	public class RootLibraryVisibleForwarders
	{
		[Kept]
		public static void Main ()
		{
		}
	}
}
