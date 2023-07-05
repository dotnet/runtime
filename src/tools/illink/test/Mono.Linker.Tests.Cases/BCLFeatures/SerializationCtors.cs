using System;
using System.Runtime.Serialization;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.BCLFeatures
{
	public class SerializationCtors
	{
		public static void Main ()
		{
			new C (2);
			new CustomSerialization ();
		}
	}

	[Kept]
	[Serializable]
	class C
	{
		public C ()
		{
		}

		[Kept]
		public C (int i)
		{
		}

		protected C (SerializationInfo info, StreamingContext context)
		{
		}
	}

	[Kept]
	class CustomSerialization
	{
		[Kept]
		public CustomSerialization ()
		{
		}

		[OnSerializing]
		[KeptAttributeAttribute (typeof (OnSerializingAttribute))]
		internal void OnSerializingMethod (StreamingContext context)
		{
		}

		[OnSerialized]
		[KeptAttributeAttribute (typeof (OnSerializedAttribute))]
		internal void OnSerializedMethod (StreamingContext context)
		{
		}

		[OnDeserializing]
		[KeptAttributeAttribute (typeof (OnDeserializingAttribute))]
		internal void OnDeserializingMethod (StreamingContext context)
		{
		}

		[OnDeserialized]
		[KeptAttributeAttribute (typeof (OnDeserializedAttribute))]
		internal void OnDeserializedMethod (StreamingContext context)
		{
		}
	}
}
