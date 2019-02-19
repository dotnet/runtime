using System;
using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes.StructLayout
{
	[StructLayout (LayoutKind.Sequential)]
	[KeptMember (".ctor()")]
	class SequentialClassData
	{
		[Kept]
		public int never_used;
		[Kept]
		public int used;
	}

	public class SequentialClass
	{
		public static void Main ()
		{
			var c = new SequentialClassData ();
			c.used = 1;
			if (Marshal.SizeOf (c) != 8)
				throw new ApplicationException ();
		}
	}
}
