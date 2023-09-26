using System.Diagnostics;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.Debugger
{
	[SetupLinkAttributesFile ("DebuggerAttributesRemoved.xml")]
	public class DebuggerDisplayAttributeOnTypeThatIsNotUsed
	{
		public static void Main ()
		{
		}

		[DebuggerDisplay ("{Property}")]
		class Foo
		{
			public int Property { get; set; }
		}
	}
}