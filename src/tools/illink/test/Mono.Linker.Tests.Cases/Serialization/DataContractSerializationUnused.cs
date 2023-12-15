using System.Runtime.Serialization;
using System.Xml.Serialization;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Serialization
{
	[Reference ("System.Runtime.Serialization.dll")]
	[Reference ("System.Runtime.Serialization.Xml.dll")]
	[Reference ("System.Runtime.Serialization.Primitives.dll")]
	[Reference ("System.Xml.XmlSerializer.dll")]
	public class DataContractSerializationUnused
	{
		public static void Main ()
		{
			// Even if the attributed type is referenced, don't keep its members if
			// not using the serializer.
			var _ = typeof (AttributedType);

			// Using a different serializer shouldn't keep DataContract attributed types
			var ser = new XmlSerializer (typeof (XmlSerializerType));
		}

		[Kept]
		class XmlSerializerType
		{
			// removed
			public int f1;
		}

		[Kept]
		[KeptAttributeAttribute (typeof (DataContractAttribute))]
		[DataContract]
		class AttributedType
		{
			// removed
			public int f1;
		}
	}
}