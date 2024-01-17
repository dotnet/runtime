using System.Timers;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed
{
	[SetupLinkerTrimMode ("link")]
	[SetupLinkerArgument ("--used-attrs-only", "true")]
	[Reference ("System.dll")]
	class CanLinkCoreLibrariesWithOnlyKeepUsedAttributes
	{
		static void Main ()
		{
			// Use something from System so that the entire reference isn't linked away
			var system = new Timer ();
		}
	}
}
