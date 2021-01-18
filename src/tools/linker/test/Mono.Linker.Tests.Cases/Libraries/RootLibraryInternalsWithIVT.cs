using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

#if RootLibraryInternalsWithIVT
[assembly: InternalsVisibleToAttribute ("somename")]

[assembly: KeptAttributeAttribute (typeof (InternalsVisibleToAttribute))]
#endif

namespace Mono.Linker.Tests.Cases.Libraries
{
#if !NETCOREAPP
	[IgnoreTestCase ("Build with illink")]
#endif
	[Kept]
	[KeptMember (".ctor()")]
	[SetupLinkerLinkPublicAndFamily]
	[Define ("RootLibraryInternalsWithIVT")]
	public class RootLibraryInternalsWithIVT
	{
		[Kept]
		public static void Main ()
		{
		}

		[Kept]
		public void UnusedPublicMethod ()
		{
		}

		[Kept]
		internal void UnusedPrivateMethod ()
		{
		}
	}
}
