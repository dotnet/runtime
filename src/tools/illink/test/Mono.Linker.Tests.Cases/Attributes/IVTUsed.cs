using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Attributes.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes
{
	[SetupCompileBefore ("lib.dll", new[] { "Dependencies/IVTUsed_Lib.cs" }, defines: new[] { "IVT" })]
	[KeptAssembly ("lib.dll")]
	[KeptMemberInAssembly ("lib.dll", typeof (External), "InternalMethod()")]
	[KeptAttributeInAssembly ("lib.dll", typeof (InternalsVisibleToAttribute))]

	// This is a bit fragile but it's used to test that ITV attribute is marked correctly
	[SetupLinkerTrimMode ("link")]
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[KeptTypeInAssembly (PlatformAssemblies.CoreLib, typeof (InternalsVisibleToAttribute))]
	class IVTUsed
	{
		static void Main ()
		{
			External.InternalMethod ();
		}
	}
}
