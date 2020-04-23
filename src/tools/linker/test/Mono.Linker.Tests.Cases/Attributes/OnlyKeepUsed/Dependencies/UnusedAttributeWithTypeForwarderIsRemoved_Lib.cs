using System;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed.Dependencies
{
	public class UnusedAttributeWithTypeForwarderIsRemoved_LibAttribute : Attribute
	{
		public UnusedAttributeWithTypeForwarderIsRemoved_LibAttribute (string arg)
		{
		}
	}

	public class UnusedAttributeWithTypeForwarderIsRemoved_OtherUsedClass
	{
		public static void UsedMethod ()
		{
		}
	}
}
