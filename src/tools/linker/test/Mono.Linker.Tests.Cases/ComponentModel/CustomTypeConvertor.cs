using System;
using System.ComponentModel;
using System.Globalization;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.ComponentModel
{
	[TypeConverter (typeof (Custom1))]

	[Kept]
	[KeptAttributeAttribute (typeof (TypeConverterAttribute))]
	[KeptMember (".ctor()")]
	class CustomDataType
	{
		[Kept]
		[KeptBaseType (typeof (TypeConverter))]
		class Custom1 : TypeConverter
		{
			[Kept]
			public Custom1 (Type type)
			{
			}

			[Kept]
			public override object ConvertFrom (ITypeDescriptorContext context, CultureInfo culture, object value)
			{
				return "test";
			}
		}
	}

	[TypeConverter ("Mono.Linker.Tests.Cases.ComponentModel.CustomDataType_2/Custom2")]

	[Kept]
	[KeptAttributeAttribute (typeof (TypeConverterAttribute))]
	[KeptMember (".ctor()")]
	class CustomDataType_2
	{
		[Kept]
		[KeptBaseType (typeof (TypeConverter))]
		class Custom2 : TypeConverter
		{
			[Kept]
			public Custom2 ()
			{
			}

			[Kept]
			public override object ConvertFrom (ITypeDescriptorContext context, CultureInfo culture, object value)
			{
				return "test";
			}
		}
	}

	[Reference ("System.dll")]
	// System.dll referenced by a dynamically (for example in TypeConverterAttribute on IComponent)
	// has unresolved references.
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	public class CustomTypeConvertor
	{
		public static void Main ()
		{
			var tc1 = TypeDescriptor.GetConverter (typeof (CustomDataType));
			var res1 = tc1.ConvertFromString ("from");

			var tc2 = TypeDescriptor.GetConverter (typeof (CustomDataType_2));
			var res2 = tc2.ConvertFromString ("from");

		}
	}
}
