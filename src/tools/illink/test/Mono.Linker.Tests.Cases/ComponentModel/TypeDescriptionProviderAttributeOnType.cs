using System.ComponentModel;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.ComponentModel
{
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

	[Reference ("System.dll")]
	public class TypeDescriptionProviderAttributeOnType
	{		
		public static void Main ()
		{
			var r1 = new CustomTypeDescriptionProvider_1 ();
		}
	}
}
