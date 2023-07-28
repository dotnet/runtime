using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;


namespace Mono.Linker.Tests.Cases.ComponentModel
{
	[Reference ("System.dll")]
	[ExpectedNoWarnings]
	public class TypeDescriptionProviderAttributeOnType
	{
		public static void Main ()
		{
			var r1 = new CustomTypeDescriptionProvider_1 ();
			IInterface v = InterfaceTypeConverter.CreateVisual (typeof (System.String));
		}

		[TypeDescriptionProvider (typeof (CustomTDP))]
		[Kept]
		[KeptAttributeAttribute (typeof (TypeDescriptionProviderAttribute))]
		class CustomTypeDescriptionProvider_1
		{
			[Kept]
			public CustomTypeDescriptionProvider_1 ()
			{
			}

			[Kept]
			[KeptBaseType (typeof (TypeDescriptionProvider))]
			class CustomTDP : TypeDescriptionProvider
			{
				[Kept]
				public CustomTDP ()
				{
				}
			}
		}

		[Kept]
		[KeptAttributeAttribute (typeof (TypeConverterAttribute))]
		[TypeConverter (typeof (InterfaceTypeConverter))]
		public interface IInterface
		{ }

		[Kept]
		[KeptBaseType (typeof (TypeConverter))]
		public class InterfaceTypeConverter : TypeConverter
		{
			[Kept]
			public static IInterface CreateVisual (
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
				Type visualType)
			{
				try {
					return (IInterface) Activator.CreateInstance (visualType);
				} catch {
				}

				return null;
			}

			[Kept]
			public InterfaceTypeConverter () { }
		}
	}
}
