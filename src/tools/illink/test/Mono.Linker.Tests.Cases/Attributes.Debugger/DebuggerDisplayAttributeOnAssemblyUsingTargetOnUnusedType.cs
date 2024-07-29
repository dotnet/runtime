using System.Diagnostics;
using Mono.Linker.Tests.Cases.Attributes.Debugger;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[assembly: DebuggerDisplay ("{Property}", Target = typeof (DebuggerDisplayAttributeOnAssemblyUsingTargetOnUnusedType.Foo))]

namespace Mono.Linker.Tests.Cases.Attributes.Debugger
{
	[SetupLinkAttributesFile ("DebuggerAttributesRemoved.xml")]
	public class DebuggerDisplayAttributeOnAssemblyUsingTargetOnUnusedType
	{
		public static void Main ()
		{
		}

		public class Foo
		{
			public int Property { get; set; }
		}
	}
}