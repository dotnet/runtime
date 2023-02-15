using System;
using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes.StructLayout
{
	[StructLayout (LayoutKind.Explicit)]
	[KeptMember (".ctor()")]
	class ExplicitClassData
	{
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

	[StructLayout (LayoutKind.Explicit)]
	[Kept]
	class UnallocatedExplicitClassData
	{
		[FieldOffset (0)]
		public int never_used;
	}

	[StructLayout (LayoutKind.Explicit)]
	[Kept]
	class UnallocatedButReferencedWithReflectionExplicitClassData
	{
		[Kept]
		[FieldOffset (0)]
		public int never_used;
	}

	public class ExplicitClass
	{
		[Kept]
		static UnallocatedExplicitClassData _myField;

		public static void Main ()
		{
			var c = new ExplicitClassData ();
			c.used = 1;

			_myField = null;

			typeof (UnallocatedButReferencedWithReflectionExplicitClassData).ToString ();
		}
	}
}
