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
		if (pNativeData != IntPtr.Zero)
			Marshal.FreeHGlobal (pNativeData);
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
		Console.WriteLine("MarshalManagedToNative()");
		return IntPtr.Zero;
 	}


	// Convert a pointer to unmanaged data into a System.Object.
	// This method simply converts the unmanaged Ansi C-string
	// into a System.String and surrounds it with asterisks
	// to differentiate it from the default marshaler.
	public object MarshalNativeToManaged (IntPtr pNativeData)
	{
		Console.WriteLine("MarshalNativeToManaged()");
		return "*" + Marshal.PtrToStringAnsi( pNativeData ) + "*";
	}
}

public class Testing
{
	[DllImport("libtest")]
	[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(MyMarshal))]
	private static extern string functionReturningString();

	public static int Main()
	{
		string res = functionReturningString();
		Console.WriteLine ("native string function returns {0}", res);

		if (res != "*ABC*")
			return 1;
		return 0;
	}



}
