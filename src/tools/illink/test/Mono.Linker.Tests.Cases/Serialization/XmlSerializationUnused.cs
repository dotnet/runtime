using System.Runtime.Serialization;
using System.Xml.Serialization;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Serialization
{
	[Reference ("System.Xml.XmlSerializer.dll")]
	[Reference ("System.Private.Xml.dll")]
	[Reference ("System.Runtime.Serialization.dll")]
	[Reference ("System.Runtime.Serialization.Xml.dll")]
	[Reference ("System.Private.DataContractSerialization.dll")]
	public class XmlSerializationUnused
	{
		public static void Main ()
		{
			// Even if the attributed type is referenced, don't keep its members if
			// not using the serializer.
			var _ = typeof (AttributedType);

			// Using a different serializer shouldn't keep XmlSerializer attributed types
			var ser = new DataContractSerializer (typeof (DataContractSerializerType));
		}

		[Kept]
		class DataContractSerializerType
		{
			// removed
			public int f1;
		}

		[Kept]
		[KeptAttributeAttribute (typeof (XmlRootAttribute))]
		[XmlRoot]
		class AttributedType
		{
			// removed
			public int f1;
		}
	}
}
