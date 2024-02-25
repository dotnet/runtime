using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

#if IVT
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo ("missing")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo ("test-with-key, PublicKey=00240000")]
#endif

[assembly: KeptAttributeAttribute ("System.Runtime.CompilerServices.InternalsVisibleToAttribute")]

namespace Mono.Linker.Tests.Cases.Attributes
{
	[SetupLinkerArgument("--used-attrs-only", "true")]
	[Define ("IVT")]
	class IVTUnused
	{
		static void Main ()
		{
		}
	}
}
