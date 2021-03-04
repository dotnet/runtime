using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Attributes.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes
{
	[SetupCompileBefore ("lib.dll", new[] { "Dependencies/IVTUsed_Lib.cs" }, defines: new[] { "IVT" })]
	[KeptAssembly ("lib.dll")]
	[KeptMemberInAssembly ("lib.dll", typeof (External), "InternalMethod()")]
	[KeptAttributeInAssembly ("lib.dll", typeof (InternalsVisibleToAttribute))]
	class IVTUsed
	{
		static void Main ()
		{
			External.InternalMethod ();
		}
	}
}
