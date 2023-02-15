using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes
{
	class GenericAttributes
	{
		static void Main ()
		{
			new WithGenericAttribute_OfString ();
			new WithGenericAttribute_OfInt ();
			new WithConstrainedGenericAttribute ();
		}

		[Kept]
		[KeptAttributeAttribute (typeof (GenericAttribute<string>))]
		[KeptMember (".ctor()")]
		[GenericAttribute<string> ("t", F = "f", P = "p")]
		class WithGenericAttribute_OfString
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (GenericAttribute<int>))]
		[KeptMember (".ctor()")]
		[GenericAttribute<int> (1, F = 2, P = 3)]
		class WithGenericAttribute_OfInt
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (ConstrainedGenericAttribute<DerivedFromConstraintType>))]
		[KeptMember (".ctor()")]
		[ConstrainedGenericAttribute<DerivedFromConstraintType> ()]
		class WithConstrainedGenericAttribute
		{
		}

		[KeptBaseType (typeof (Attribute))]
		class GenericAttribute<T> : Attribute
		{
			[Kept]
			public GenericAttribute (T t) { }

			[Kept]
			public T F;

			[Kept]
			[KeptBackingField]
			public T P {
				get;
				[Kept]
				set;
			}
		}

		[Kept]
		class ConstraintType
		{
		}

		[KeptBaseType (typeof (ConstraintType))]
		class DerivedFromConstraintType : ConstraintType
		{
		}

		[KeptBaseType (typeof (Attribute))]
		class ConstrainedGenericAttribute<T> : Attribute
			where T : ConstraintType
		{
			[Kept]
			public ConstrainedGenericAttribute () { }
		}
	}
}