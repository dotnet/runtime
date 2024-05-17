using System.Diagnostics;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.Debugger.KeepDebugMembers
{
#if !NET
	[SetupLinkerKeepDebugMembers ("true")]
#endif

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