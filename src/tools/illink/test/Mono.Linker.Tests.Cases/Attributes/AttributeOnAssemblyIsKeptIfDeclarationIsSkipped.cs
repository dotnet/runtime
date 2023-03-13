using System;
using Mono.Linker.Tests.Cases.Attributes;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

#if REFERENCE_INCLUDED
[assembly: Mono.Linker.Tests.Cases.Attributes.Dependencies.AttributeDefinedInReference]
#endif
[assembly: KeptAttributeAttribute ("Mono.Linker.Tests.Cases.Attributes.Dependencies.AttributeDefinedInReference")]

namespace Mono.Linker.Tests.Cases.Attributes
{
	[SkipPeVerify]
	[Define ("REFERENCE_INCLUDED")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/AttributeDefinedInReference.cs" })]
	[SetupLinkerAction ("skip", "library")]
	class AttributeOnAssemblyIsKeptIfDeclarationIsSkipped
	{
		static void Main ()
		{
		}
	}
}
