// A demonstration of a custom marshaler that marshals
// unmanaged to managed data.

using System;
using System.Runtime.InteropServices;

public class MyMarshal: ICustomMarshaler
{

	// GetInstance() is not part of ICustomMarshaler, but
	// custom marshalers are required to implement this
	// method.
	public static ICustomMarshaler GetInstance (string s)
	{
		Console.WriteLine ("GetInstance called");
		return new MyMarshal ();
	}
	
	public void CleanUpManagedData (object managedObj)
	{
		Console.WriteLine ("CleanUpManagedData called");
	}

	public void CleanUpNativeData (IntPtr pNativeData)
	{
		Console.WriteLine("CleanUpNativeData called");
		if (pNativeData != IntPtr.Zero) {
			IntPtr realPtr = new IntPtr (pNativeData.ToInt64 () - Marshal.SizeOf (typeof (int)));

			Marshal.FreeHGlobal (realPtr);
		}
	}


	// I really do not understand the purpose of this method
	// or went it would be called. In fact, Rotor never seems
	// to call it.
	public int GetNativeDataSize ()
	{
		Console.WriteLine("GetNativeDataSize() called");
		return 4;
	}

	public IntPtr MarshalManagedToNative (object managedObj)
	{
		int number;
		IntPtr ptr;

		try {
			number = Convert.ToInt32 (managedObj);
			ptr = Marshal.AllocHGlobal (8);
			Marshal.WriteInt32 (ptr, 0);
			Marshal.WriteInt32 (new IntPtr (ptr.ToInt64 () + Marshal.SizeOf (typeof(int))), number);
			return new IntPtr (ptr.ToInt64 () + Marshal.SizeOf (typeof (int)));
		} catch {
			return IntPtr.Zero;
		}
 	}


	// Convert a pointer to unmanaged data into a System.Object.
	// This method simply converts the unmanaged Ansi C-string
	// into a System.String and surrounds it with asterisks
	// to differentiate it from the default marshaler.
	public object MarshalNativeToManaged (IntPtr pNativeData)
	{
		return "*" + Marshal.PtrToStringAnsi( pNativeData ) + "*";
	}
}

public class Testing
{
	[DllImport("libtest")]
	private static extern int printInt([MarshalAs( UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(MyMarshal ))] object number );

	[DllImport("libtest")]
	private static extern void callFunction (Delegate d);

	delegate void Del ([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(MyMarshal))] string x);

	public static void TestMethod (string s)
	{
		Console.WriteLine("s = {0}", s);
		if (s != "*ABC*")
			throw new Exception ("received wrong value");
	}

	public static int Main()
	{
		object x = 5;
		if (printInt (x) != 6)
			return 1;

		Del del = new Del (TestMethod);
		callFunction (del);

		return 0;
	}



}
