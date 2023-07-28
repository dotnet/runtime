using System.Diagnostics;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.Debugger.KeepDebugMembers
{
#if !NETCOREAPP
	[SetupLinkerKeepDebugMembers ("true")]
#endif

	// Can be removed once this bug is fixed https://bugzilla.xamarin.com/show_bug.cgi?id=58168
	[SkipPeVerify (SkipPeVerifyForToolchian.Pedump)]
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