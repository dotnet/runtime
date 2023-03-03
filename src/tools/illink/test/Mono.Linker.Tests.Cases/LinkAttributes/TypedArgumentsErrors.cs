using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkAttributes
{
	[SetupLinkAttributesFile ("TypedArgumentsErrors.xml")]
	[IgnoreLinkAttributes (false)]
	[NoLinkedOutput]

	[LogContains ("The type 'System.NoOBJECT' used with attribute value 'str' could not be found")]
	[LogContains ("Cannot convert value 'str' to type 'System.Int32'")]
	[LogContains ("Custom attribute argument for 'System.Object' requires nested 'argument' node")]
	[LogContains ("The type 'System.Bar' used with attribute value 'str4' could not be found")]
	[LogContains ("Could not resolve custom attribute type value 'str5'")]
	class TypedArgumentsErrors
	{
		static object field;

		public static void Main ()
		{
		}

		public class ObjectAttribute : Attribute
		{
			public ObjectAttribute (object objectValue)
			{
			}

			public ObjectAttribute (string stringValue)
			{
			}
		}
	}
}
