using System;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Serialization
{
	[Reference ("System.Runtime.Serialization.dll")]
	[Reference ("System.Runtime.Serialization.Primitives.dll")]
	[Reference ("System.Runtime.Serialization.Json.dll")]
	[SetupLinkerArgument ("--enable-serialization-discovery")]
	public class DataContractJsonSerialization
	{
		public static void Main ()
		{
			// This ctor call should activate the data contract serializer logic
			new DataContractJsonSerializer (typeof (RootType));

			// Reference types to ensure they are scanned for attributes.
			Type t = typeof (AttributedType);
		}

		[Kept]
		class RootType
		{
			// removed
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
	}
}