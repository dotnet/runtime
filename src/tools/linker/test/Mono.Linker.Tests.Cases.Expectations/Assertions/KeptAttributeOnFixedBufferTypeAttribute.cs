using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions {
	[AttributeUsage (AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
	public class KeptAttributeOnFixedBufferTypeAttribute : KeptAttribute {
		public KeptAttributeOnFixedBufferTypeAttribute (string attributeName)
		{
			if (string.IsNullOrEmpty (attributeName))
				throw new ArgumentException ("Value cannot be null or empty.", nameof (attributeName));
		}

		public KeptAttributeOnFixedBufferTypeAttribute (Type type)
		{
			if (type == null)
				throw new ArgumentNullException (nameof (type));
		}
	}
}