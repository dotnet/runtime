using System;
using System.ComponentModel;
using System.Globalization;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.ComponentModel
{
	// Keep framework code that calls TypeConverter methods like ConvertFrom
	[SetupLinkerCoreAction ("skip")]
	[Reference ("System.dll")]
	public class TypeConverterOnMembers
	{
		public static void Main ()
		{
			var r1 = new OnProperty ().Foo;
			var r2 = new OnField ().Field;
		}
	}

	[Kept]
	class OnProperty
	{
		[Kept]
		public OnProperty ()
		{
		}

		[TypeConverter (typeof (Custom1))]

		[Kept]
		[KeptAttributeAttribute (typeof (TypeConverterAttribute))]
		[KeptBackingField]
		public string Foo { [Kept] get; set; }

		[Kept]
		[KeptBaseType (typeof (TypeConverter))]
		class Custom1 : TypeConverter
		{
			[Kept]
			public Custom1 (Type type)
			{
			}
		}
	}

	[Kept]
	class OnField
	{
		[Kept]
		public OnField ()
		{
		}

		[TypeConverter (typeof (Custom2))]

		[Kept]
		[KeptAttributeAttribute (typeof (TypeConverterAttribute))]
		public object Field;

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
}
