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

	[Kept]
	[StructLayout (LayoutKind.Sequential)]
	class UnallocatedButWithSingleFieldUsedSequentialClassDataBase
	{
		// We expect this to be kept because of the sequential layout.
		// Code could do Unsafe.As tricks to alias a never allocated type
		// with another type and access fields on it.
		// Removing the base or fields on the base would shift the offsets
		// and break the aliasing.
		[Kept]
		public int never_used_base;
	}

	[Kept]
	[StructLayout (LayoutKind.Sequential)]
	[KeptBaseType (typeof (UnallocatedButWithSingleFieldUsedSequentialClassDataBase))]
	class UnallocatedButWithSingleFieldUsedSequentialClassData : UnallocatedButWithSingleFieldUsedSequentialClassDataBase
	{
		[Kept]
		public int never_used;

		[Kept]
		public int used;
	}

	public class SequentialClass
	{
		[Kept]
		static UnallocatedSequentialClassData _field;

		[Kept]
		static UnallocatedButWithSingleFieldUsedSequentialClassData _otherField;

		public static void Main ()
		{
			var c = new SequentialClassData ();
			c.used = 1;
			if (Marshal.SizeOf (c) != 8)
				throw new ApplicationException ();

			_field = null;

			typeof (UnallocatedButReferencedWithReflectionSequentialClassData).ToString ();

			if (string.Empty.Length > 0) {
				_otherField.used = 123;
			}
		}
	}
}
