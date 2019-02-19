using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata {

	/// <summary>
	/// Used to define arguments to pass to the linker.
	/// 
	/// Don't use this attribute to setup single character flags.  These flags do a poor job of communicating their purpose
	/// and although we need to continue to support the usages that exist today, that doesn't mean we need to make our tests harder to read
	/// </summary>
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true)]
	public class SetupLinkerArgumentAttribute : BaseMetadataAttribute {
		public SetupLinkerArgumentAttribute (string flag, params string [] values)
		{
			if (string.IsNullOrEmpty (flag))
				throw new ArgumentNullException (nameof (flag));

			if (flag[0] == '-' && flag.Length == 2) {
				string errorMessage = "Flag `" + flag + "` is short.";
				errorMessage += "  Avoid using this attribute with command line flags that are to short to communicate their meaning.  Support a more descriptive flag or create a new attribute for tests to use similar to " + nameof (SetupLinkerCoreActionAttribute);
				throw new ArgumentException (errorMessage);
			}
		}
	}
}
