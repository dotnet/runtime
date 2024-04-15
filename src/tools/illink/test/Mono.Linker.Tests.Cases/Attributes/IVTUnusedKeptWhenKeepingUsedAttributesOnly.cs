using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Attributes.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes;

[SetupCompileBefore ("lib.dll", ["Dependencies/IVTUnused_Lib.cs"], defines: ["IVT"])]
[KeptAssembly ("lib.dll")]
[KeptTypeInAssembly ("lib.dll", typeof(IVTUnusedLib))]
[KeptAttributeInAssembly ("lib.dll", typeof (InternalsVisibleToAttribute))]

[SetupLinkerArgument("--used-attrs-only", "true")]
class IVTUnusedKeptWhenKeepingUsedAttributesOnly
{
	static void Main ()
	{
		_ = new IVTUnusedLib ();
	}
}
