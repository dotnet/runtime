using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

#if IVT
[assembly: InternalsVisibleTo ("missing")]
[assembly: InternalsVisibleTo ("test-with-key, PublicKey=00240000")]

#endif

namespace Mono.Linker.Tests.Cases.Attributes
{
	[Define ("IVT")]
	class IVTUnused
	{
		static void Main ()
		{
		}
	}
}
