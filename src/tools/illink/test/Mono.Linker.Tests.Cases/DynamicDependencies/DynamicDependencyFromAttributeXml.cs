using System;
using System.Collections.Generic;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	// For netcoreapp we don't have to specify the assembly for the attribute, since the attribute comes from corelib
	// and will be found always.
	// For mono though, we have to specify the assembly (Mono.Linker.Tests.Cases.Expectations) because at the time of processing
	// that assembly is not yet loaded into the closure in the linker, so it won't find the attribute type.
#if NETCOREAPP
	[SetupLinkAttributesFile ("DynamicDependencyFromAttributeXml.netcore.Attributes.xml")]
#else
	[SetupLinkAttributesFile ("DynamicDependencyFromAttributeXml.mono.Attributes.xml")]
#endif
	[IgnoreLinkAttributes (false)]
	class DynamicDependencyFromAttributeXml
	{
		public static void Main ()
		{
			DependencyToUnusedMethod ();
		}

		[Kept]
		static void DependencyToUnusedMethod ()
		{
		}

		[Kept]
		static void UnusedMethod ()
		{
		}
	}
}
