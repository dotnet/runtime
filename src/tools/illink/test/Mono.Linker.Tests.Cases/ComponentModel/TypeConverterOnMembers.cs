using System;
using System.ComponentModel;
using System.Globalization;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.ComponentModel
{
	// Keep framework code that calls TypeConverter methods like ConvertFrom
	[SetupLinkerTrimMode ("skip")]
	[Reference ("System.dll")]
	public class TypeConverterOnMembers
	{
		public static void Main ()
		{
			var r1 = new OnProperty ().Foo;
			var r2 = new OnField ().Field;
			TestArgumentWithTypeNameReferencingANonExistentType ();
		}

		[Kept]
		public static void TestArgumentWithTypeNameReferencingANonExistentType ()
		{
			_ = new OnProperty ().Bar;
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

		[TypeConverter ("NonExistentType")]

		[Kept]
		[KeptAttributeAttribute (typeof (TypeConverterAttribute))]
		[KeptBackingField]
		[ExpectedWarning ("IL2105",
			"Type 'NonExistentType' was not found in the caller assembly nor in the base library. " +
			"Type name strings used for dynamically accessing a type should be assembly qualified.")]
		public string Bar { [Kept] get; set; }

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
