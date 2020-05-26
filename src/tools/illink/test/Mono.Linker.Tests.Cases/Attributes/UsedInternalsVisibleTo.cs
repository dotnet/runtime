using System;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Attributes;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes
{
	[KeptAssembly ("library.dll")]
	[KeptAssembly ("internals.dll")]
	[KeptAttributeInAssembly ("internals", "System.Runtime.CompilerServices.InternalsVisibleToAttribute")]

	[SetupCompileBefore ("internals.dll", new[] { "Dependencies/InternalsImpl.cs" }, defines: new[] { "IVT_INCLUDED" })]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/UsedIVTAttribute.cs" }, references: new[] { "internals.dll" })]
	class UsedInternalsVisibleTo
	{
		static void Main ()
		{
			UsedInternalsVisibleToLib.TestA ();
		}
	}
}
