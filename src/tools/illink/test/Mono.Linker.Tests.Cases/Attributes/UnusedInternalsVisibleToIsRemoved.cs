using System;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Attributes;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

#if REFERENCE_INCLUDED
[assembly: InternalsVisibleTo ("library")]
#endif

namespace Mono.Linker.Tests.Cases.Attributes
{
	[Define ("REFERENCE_INCLUDED")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/UnusedIVTAttribute.cs" })]
	class UnusedInternalsVisibleToIsRemoved
	{
		static void Main ()
		{
		}

		void UnusedMethod ()
		{
			External.TestA ();
		}
	}
}
