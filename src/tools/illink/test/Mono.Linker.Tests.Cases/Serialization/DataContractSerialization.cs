using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Serialization
{
	[Reference ("System.Runtime.Serialization.dll")]
	[Reference ("System.Runtime.Serialization.Xml.dll")]
	[Reference ("System.Runtime.Serialization.Primitives.dll")]
	[SetupLinkerArgument ("--enable-serialization-discovery")]
	public class DataContractSerialization
	{
		public static void Main ()
		{
			// We don't discover roots passed to the ctor
			new DataContractSerializer (typeof (RootType));

			// We don't model type arrays, so the extra type will not be discovered for serialization.
			new DataContractSerializer (typeof (RootType), new Type[] { typeof (ExtraType) });

			// We don't track generic instance typerefs in dataflow, so generic parameters in root types will not be discovered.
			new DataContractSerializer (typeof (GenericRootType<GenericRootParameter>));

			// There are no annotations for serialized types, so we can only discover types statically referenced by the direct caller of the serializer ctor.
			DataContractSerializerHelper (typeof (RootType));
			GenericDataContractSerializerHelper<RootType> ();

			// Instantiating types for serialized collection interfaces doesn't keep members of those types.
			var collectionMembersType = new CollectionMembersType {
				enumerable = new Enumerable (),
				genericEnumerable = new GenericEnumerable<ItemType> ()
			};

			// Reference types to ensure they are scanned for attributes.
			Type t;
			t = typeof (AttributedType);
			t = typeof (AttributedFieldType);
			t = typeof (AttributedPropertyType);
			t = typeof (AttributedPrivateFieldType);
			t = typeof (AttributedStaticFieldType);
			t = typeof (CollectionMembersType);
			t = typeof (Enumerable);
			t = typeof (ItemType);
			t = typeof (GenericEnumerable<>);
		}

		[Kept]
		public static DataContractSerializer DataContractSerializerHelper (Type t)
		{
			return new DataContractSerializer (t);
		}

		[Kept]
		public static DataContractSerializer GenericDataContractSerializerHelper<T> ()
		{
			return new DataContractSerializer (typeof (T));
		}

		[Kept]
		class RootType
		{
			// removed
			int f1;
		}

		[Kept]
		class ExtraType
		{
			// removed
			int f1;
		}

		[Kept]
		class GenericRootParameter
		{
			// removed
			int f2;
		}

		[Kept]
		class GenericRootType<T>
		{
			// removed
			T f1;
			// removed
			int f2;
		}

		[DataContract]
		class AttributedUnusedType
		{
			public int f1;
		}

		// removed
		class AttributedFieldUnusedType
		{
			[DataMember]
			public int f1;
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (DataContractAttribute))]
		[DataContract]
		class AttributedType
		{
			[Kept]
			public int f1;
		}

		[Kept]
		[KeptMember (".ctor()")]
		class AttributedFieldType
		{
			[Kept]
			[KeptAttributeAttribute (typeof (DataMemberAttribute))]
			[DataMember]
			public int f1;

			[Kept]
			public int f2;
		}

		[Kept]
		[KeptMember (".ctor()")]
		class AttributedPropertyType
		{
			[Kept]
			[KeptBackingField]
			[KeptAttributeAttribute (typeof (DataMemberAttribute))]
			[DataMember]
			public int P { [Kept] get; }

			[Kept]
			public int f1;
		}

		[Kept]
		[KeptMember (".ctor()")]
		class AttributedPrivateFieldType
		{
			[Kept]
			[KeptMember (".ctor()")]
			class PrivateFieldType
			{
				[Kept]
				public int f1;
			}

			// Attributed private member is kept even though the recursive
			// type discovery doesn't consider private members.
			[Kept]
			[KeptAttributeAttribute (typeof (DataMemberAttribute))]
			[DataMember]
			PrivateFieldType f1;

			[Kept]
			public int f2;

			// removed
			int f3;
		}

		[Kept]
		[KeptMember (".ctor()")]
		class AttributedStaticFieldType
		{
			[Kept]
			[KeptMember (".ctor()")]
			public class StaticFieldType
			{
				[Kept]
				public int f1;
			}

			// Attributed static member is kept even though the recursive
			// type discovery doesn't consider static members.
			[Kept]
			[KeptAttributeAttribute (typeof (DataMemberAttribute))]
			[DataMember]
			public static StaticFieldType f1;

			[Kept]
			public int f2;

			// removed
			int f3;
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (DataContractAttribute))]
		[DataContract]
		class CollectionMembersType
		{
			[Kept]
			public IEnumerable enumerable;

			[Kept]
			public IEnumerable<ItemType> genericEnumerable;
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IEnumerable))]
		class Enumerable : IEnumerable
		{
			// removed
			public int f1;

			// IEnumerable implementation
			[Kept]
			public IEnumerator GetEnumerator () => null;
		}

		[Kept]
		[KeptMember (".ctor()")]
		class ItemType
		{
			[Kept]
			public int f1;
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IEnumerable<>))]
		[KeptInterface (typeof (IEnumerable))]
		class GenericEnumerable<T> : IEnumerable<T>
		{
			// removed
			public T f1;
			// removed
			public int f2;

			// IEnumerable<T> implementation
			[Kept]
			public IEnumerator<T> GetEnumerator () => null;
			[Kept]
			IEnumerator IEnumerable.GetEnumerator () => null;
		}
	}
}