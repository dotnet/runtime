using System;

namespace Mono.Linker.Tests.Cases.TypeForwarding.Dependencies
{
	public class AttributeWithEnumArgumentAttribute : Attribute
	{
		public AttributeWithEnumArgumentAttribute (MyEnum arg)
		{
		}
	}

	[AttributeWithEnumArgument (MyEnum.A)]
	public class AttributedType
	{
	}
}