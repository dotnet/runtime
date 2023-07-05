using System;

namespace Mono.Linker.Tests.Cases.Attributes.Dependencies
{
	[AttributeUsage (AttributeTargets.All, AllowMultiple = true)]
	public class AttributeDefinedInReference : Attribute
	{
		public Type FieldType;

		public AttributeDefinedInReference ()
		{
		}

		public AttributeDefinedInReference (Type t)
		{
		}

		public AttributeDefinedInReference (Type[] t)
		{
		}

		public Type PropertyType { get; set; }
	}

	public class AttributeDefinedInReference_OtherType
	{
		public static void Method ()
		{
		}
	}
}