using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata {
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true)]
	public class DefineAttribute : BaseMetadataAttribute {
		public DefineAttribute (string name)
		{
			if (string.IsNullOrEmpty (name))
				throw new ArgumentException ("Value cannot be null or empty.", nameof (name));
		}
	}
}
