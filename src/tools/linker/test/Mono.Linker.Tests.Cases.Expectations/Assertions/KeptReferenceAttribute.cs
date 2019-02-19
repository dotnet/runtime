using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions {
	/// <summary>
	/// Verifies that a reference exists in the test case assembly
	/// </summary>
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class KeptReferenceAttribute : KeptAttribute {
		public KeptReferenceAttribute (string name)
		{
			if (string.IsNullOrEmpty (name))
				throw new ArgumentException ("Value cannot be null or empty.", nameof (name));
		}
	}
}