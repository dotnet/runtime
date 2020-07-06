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

	[Kept]
	[StructLayout (LayoutKind.Sequential)]
	class UnallocatedSequentialClassData
	{
		public int never_used;
	}

	[Kept]
	[StructLayout (LayoutKind.Sequential)]
	class UnallocatedButReferencedWithReflectionSequentialClassData
	{
		[Kept]
		public int never_used;
	}

	public class SequentialClass
	{
		[Kept]
		static UnallocatedSequentialClassData _field;

		public static void Main ()
		{
			var c = new SequentialClassData ();
			c.used = 1;
			if (Marshal.SizeOf (c) != 8)
				throw new ApplicationException ();

			_field = null;

			typeof (UnallocatedButReferencedWithReflectionSequentialClassData).ToString ();
		}
	}
}
