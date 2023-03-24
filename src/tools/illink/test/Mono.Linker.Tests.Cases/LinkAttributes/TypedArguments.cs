using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkAttributes
{
	[SetupLinkAttributesFile ("TypedArguments.xml")]
	[IgnoreLinkAttributes (false)]
	[SetupLinkerArgument ("--verbose")]

	[LogContains ("Assigning external custom attribute 'Mono.Linker.Tests.Cases.LinkAttributes.TypedArguments.ObjectAttribute.ObjectAttribute(Object) { args: System.Object Mono.Cecil.CustomAttributeArgument }' instance to 'System.Object Mono.Linker.Tests.Cases.LinkAttributes.TypedArguments::field1'")]
	[LogContains ("Assigning external custom attribute 'Mono.Linker.Tests.Cases.LinkAttributes.TypedArguments.ObjectAttribute.ObjectAttribute(Object) { args: System.Object Mono.Cecil.CustomAttributeArgument }' instance to 'System.Object Mono.Linker.Tests.Cases.LinkAttributes.TypedArguments::field2'")]
	[LogContains ("Assigning external custom attribute 'Mono.Linker.Tests.Cases.LinkAttributes.TypedArguments.EnumAttribute.EnumAttribute(TypedArgumentsEnumA) { args: Mono.Linker.Tests.Cases.LinkAttributes.TypedArgumentsEnumA 3 }' instance to 'System.Object Mono.Linker.Tests.Cases.LinkAttributes.TypedArguments::field3'")]
	[LogContains ("Assigning external custom attribute 'Mono.Linker.Tests.Cases.LinkAttributes.TypedArguments.ByteAttribute.ByteAttribute(Byte) { args: System.Byte 6 }' instance to 'System.Object Mono.Linker.Tests.Cases.LinkAttributes.TypedArguments::field4'")]
	[LogContains ("Assigning external custom attribute 'Mono.Linker.Tests.Cases.LinkAttributes.TypedArguments.StringAttribute.StringAttribute(String) { args: System.String str }' instance to 'System.Object Mono.Linker.Tests.Cases.LinkAttributes.TypedArguments::field5'")]
	[LogContains ("Assigning external custom attribute 'Mono.Linker.Tests.Cases.LinkAttributes.TypedArguments.TypeAttribute.TypeAttribute(Type) { args: System.Type System.DateTime }' instance to 'System.Object Mono.Linker.Tests.Cases.LinkAttributes.TypedArguments::field6'")]
	class TypedArguments
	{
		[Kept]
		static object field1;

		[Kept]
		static object field2;

		[Kept]
		static object field3;

		[Kept]
		static object field4;

		[Kept]
		static object field5;

		[Kept]
		static object field6;

		public static void Main ()
		{
			field1 = null;
			field2 = null;
			field3 = null;
			field4 = null;
			field5 = null;
			field6 = null;
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

		public class ByteAttribute : Attribute
		{
			public ByteAttribute (uint intValue)
			{
			}

			public ByteAttribute (byte byteValue)
			{
			}

			public ByteAttribute (object objectValue)
			{
			}
		}

		public class EnumAttribute : Attribute
		{
			public EnumAttribute (int intValue)
			{
			}

			public EnumAttribute (object objectValue)
			{
			}

			public EnumAttribute (TypedArgumentsEnumA enumA)
			{
			}

			public EnumAttribute (TypedArgumentsEnumB enumB)
			{
			}
		}

		public class StringAttribute : Attribute
		{
			public StringAttribute (string stringValue)
			{
			}

			public StringAttribute (object objectValue)
			{
			}
		}

		public class TypeAttribute : Attribute
		{
			public TypeAttribute (string stringValue)
			{
			}

			public TypeAttribute (Type typeValue)
			{
			}
		}
	}

	enum TypedArgumentsEnumA
	{
		Value = 2
	}

	enum TypedArgumentsEnumB
	{
	}
}
