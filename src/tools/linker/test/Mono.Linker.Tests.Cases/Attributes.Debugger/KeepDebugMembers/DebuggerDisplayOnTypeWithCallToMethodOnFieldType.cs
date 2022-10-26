using System.Diagnostics;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.Debugger.KeepDebugMembers
{
#if !NETCOREAPP
	[SetupLinkerKeepDebugMembers ("true")]
#endif
	public class DebuggerDisplayOnTypeWithCallToMethodOnFieldType
	{
		public static void Main ()
		{
			var foo = new Foo ();
			foo.Field = new Bar ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (DebuggerDisplayAttribute))]

		//TODO : DebuggerDisplay supports calling methods on members.
		//However, robustly handling this case in the linker would require some non-trivial work.
		//For now let's at least make sure that the linker doesn't crash.
		[DebuggerDisplay ("Count = {Field.Count()}")]
		class Foo
		{
			[Kept]
			public Bar Field;

			public int Count ()
			{
				return 1;
			}
		}

		[Kept]
		[KeptMember (".ctor()")]
		public class Bar
		{
			public int Count ()
			{
				return 1;
			}
		}
	}
}