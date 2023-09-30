#define FLAG

using System.Diagnostics;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes.Debugger.KeepDebugMembers
{
	public class DebuggerDisplayAttributeOnTypeWithNonExistentMethod
	{
		public static void Main ()
		{
			var bar = new Bar ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptAttributeAttribute (typeof (DebuggerDisplayAttribute))]
		[DebuggerDisplay ("{Method()}")]
		class Bar
		{
#if !FLAG
			public int Method ()
			{
				return 1;
			}
#endif
		}
	}
}