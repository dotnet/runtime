using System;
using Mono.Linker.Tests.Cases.Attributes.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[assembly: AttributeInReference]

namespace Mono.Linker.Tests.Cases.Attributes
{
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/AssemblyAttributeIsRemovedIfOnlyTypesUsedInAssembly_Lib.cs" })]
	[RemovedAssembly ("library.dll")]
	class AssemblyAttributeIsRemovedIfOnlyTypesUsedInAssembly
	{
		static void Main ()
		{
		}
	}
}
