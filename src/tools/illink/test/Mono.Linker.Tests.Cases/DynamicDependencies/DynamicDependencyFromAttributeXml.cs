using System;
using System.Collections.Generic;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	[SetupLinkAttributesFile ("DynamicDependencyFromAttributeXml.Attributes.xml")]
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
