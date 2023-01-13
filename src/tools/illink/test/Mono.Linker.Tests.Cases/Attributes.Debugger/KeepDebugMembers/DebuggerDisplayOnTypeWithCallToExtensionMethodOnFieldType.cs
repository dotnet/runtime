using System.Diagnostics;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[assembly: KeptAttributeAttribute (typeof (ExtensionAttribute))]

namespace Mono.Linker.Tests.Cases.Attributes.Debugger.KeepDebugMembers
{
#if !NETCOREAPP
	[SetupLinkerKeepDebugMembers ("true")]
#endif
	public class DebuggerDisplayOnTypeWithCallToExtensionMethodOnFieldType
	{
		public static void Main ()
		{
			var foo = new Foo ();
			foo.Field = new Bar ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (DebuggerDisplayAttribute))]
		// Calling extension methods on members from DebuggerDisplay doesn't seem to work so in this case we shouldn't mark `Field.Count()`
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
		}
	}

	public static class DebuggerDisplayOnTypeWithCallToExtensionMethodOnFieldTypeExtensions
	{
		public static int Count (this DebuggerDisplayOnTypeWithCallToExtensionMethodOnFieldType.Bar b)
		{
			return 1;
		}
	}
}