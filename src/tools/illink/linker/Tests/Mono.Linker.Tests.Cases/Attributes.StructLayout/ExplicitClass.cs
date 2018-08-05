using System;
using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes.StructLayout {
	[StructLayout (LayoutKind.Explicit)]
	[KeptMember (".ctor()")]
	class ExplicitClassData {
		[FieldOffset (0)]
		[Kept] // the linker could remove this
		public int never_used;
		[FieldOffset (4)]
		[Kept]
		public int used;
		[FieldOffset (8)]
		[Kept]
		public int never_ever_used;
	}

	public class ExplicitClass
	{
		public static void Main ()
		{
			var c = new ExplicitClassData ();
			c.used = 1;
		}
	}
}
