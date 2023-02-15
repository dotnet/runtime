using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Serialization
{
	[Reference ("System.Xml.ReaderWriter.dll")]
	[Reference ("System.Xml.XmlSerializer.dll")]
	public class CanDisableSerializationDiscovery
	{
		public static void Main ()
		{
			var ser = new XmlSerializer (typeof (AttributedType));
		}

		[Kept]
		[KeptAttributeAttribute (typeof (XmlRootAttribute))]
		[XmlRoot]
		class AttributedType
		{
			// removed
			public int f1;

			// removed
			public class RecursiveType
			{
				public int f1;
			}

			// removed
			RecursiveType f2;
		}
	}
}
