using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions {
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class KeptSecurityAttribute : KeptAttribute {
		public KeptSecurityAttribute (string attributeName)
		{
			if (string.IsNullOrEmpty (attributeName))
				throw new ArgumentException ("Value cannot be null or empty.", nameof (attributeName));
		}

		public KeptSecurityAttribute (Type type)
		{
			if (type == null)
				throw new ArgumentNullException (nameof (type));
		}
	}
}
