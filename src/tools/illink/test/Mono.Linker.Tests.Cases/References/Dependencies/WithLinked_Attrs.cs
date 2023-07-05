using System;

namespace Mono.Linker.Tests.Cases.References.Dependencies
{
	public class WithLinked_Attrs
	{
		public enum FooEnum
		{
			One,
			Two,
			Three
		}
		public class MethodAttribute : Attribute
		{
		}

		public class MethodWithEnumValueAttribute : Attribute
		{
			public MethodWithEnumValueAttribute (FooEnum value, Type t)
			{
			}
		}

		public class FieldAttribute : Attribute
		{
		}

		public class EventAttribute : Attribute
		{
		}

		public class PropertyAttribute : Attribute
		{
		}

		public class TypeAttribute : Attribute
		{
		}

		public class ParameterAttribute : Attribute
		{
		}
	}
}