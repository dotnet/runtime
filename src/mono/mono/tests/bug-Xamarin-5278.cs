//
// bug-Xamarin-5278.cs
//
//  Tests for System.Reflection.Binder class that require an unmanaged COM object
//  (Xamarin bug 5278)
//
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public class Tests
{
	[DllImport ("libtest")]
	public static extern int mono_test_marshal_com_object_create (out IntPtr pUnk);

	[DllImport("libtest")]
	public static extern bool mono_cominterop_is_supported ();

	#region Definition of COM object
	[ComImport ()]
	[Guid ("00000000-0000-0000-0000-000000000001")]
	[InterfaceType (ComInterfaceType.InterfaceIsIUnknown)]
	public interface ITest
	{
		// properties need to go first since mcs puts them there
		ITest Test {
			[return: MarshalAs (UnmanagedType.Interface)]
			[MethodImpl (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId (5242884)]
			get;
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void SByteIn (sbyte val);

		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void ByteIn (byte val);

		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void ShortIn (short val);

		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void UShortIn (ushort val);

		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void IntIn (int val);

		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void UIntIn (uint val);

		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void LongIn (long val);

		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void ULongIn (ulong val);

		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void FloatIn (float val);

		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void DoubleIn (double val);

		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void ITestIn ([MarshalAs (UnmanagedType.Interface)]ITest val);

		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		void ITestOut ([MarshalAs (UnmanagedType.Interface)]out ITest val);
	}

	[System.Runtime.InteropServices.GuidAttribute ("00000000-0000-0000-0000-000000000002")]
	[System.Runtime.InteropServices.ComImportAttribute ()]
	[System.Runtime.InteropServices.ClassInterfaceAttribute (ClassInterfaceType.None)]
	public class _TestClass : ITest
	{
		// properties need to go first since mcs puts them there
		public virtual extern ITest Test {
			[return: MarshalAs (UnmanagedType.Interface)]
			[MethodImpl (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime), DispId (5242884)]
			get;
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public virtual extern void SByteIn (sbyte val);

		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public virtual extern void ByteIn (byte val);

		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public virtual extern void ShortIn (short val);

		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public virtual extern void UShortIn (ushort val);

		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public virtual extern void IntIn (int val);

		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public virtual extern void UIntIn (uint val);

		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public virtual extern void LongIn (long val);

		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public virtual extern void ULongIn (ulong val);

		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public virtual extern void FloatIn (float val);

		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public virtual extern void DoubleIn (double val);

		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public virtual extern void ITestIn ([MarshalAs (UnmanagedType.Interface)]ITest val);

		[MethodImplAttribute (MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
		public virtual extern void ITestOut ([MarshalAs (UnmanagedType.Interface)]out ITest val);
	}

	[System.Runtime.InteropServices.GuidAttribute ("00000000-0000-0000-0000-000000000002")]
	public class TestClass : _TestClass
	{
		static TestClass ()
		{
			ExtensibleClassFactory.RegisterObjectCreationCallback (new ObjectCreationDelegate (CreateObject));
			;
		}

		private static System.IntPtr CreateObject (System.IntPtr aggr)
		{
			IntPtr pUnk3;
			mono_test_marshal_com_object_create (out pUnk3);
			return pUnk3;
		}
	}
	#endregion

	public class Foo
	{
		public Foo (ITest test)
		{
		}
	}

	public static bool CreateInstanceWithComObjectParameter ()
	{
		try {
			var testObj = new TestClass ();
			var comObject = testObj.Test;
			var assembly = Assembly.GetExecutingAssembly ();
			var foo = assembly.CreateInstance (typeof(Foo).FullName, false, BindingFlags.Instance | BindingFlags.Public,
				null, new object[] { comObject }, null, null);
			return foo != null;
		} catch (MissingMethodException) {
			return false;
		}
	}
	
	public static int Main ()
	{
		bool isWindows = !(((int)Environment.OSVersion.Platform == 4) ||
			((int)Environment.OSVersion.Platform == 128));

		if (!mono_cominterop_is_supported () && !isWindows)
			return 0;

		if (!CreateInstanceWithComObjectParameter ())
			return 1;

		return 0;
	}
}
