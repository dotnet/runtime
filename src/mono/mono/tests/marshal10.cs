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

// From Gtk#
public class time_t_CustomMarshaler : ICustomMarshaler {

	static time_t_CustomMarshaler marshaler;
	int utc_offset;
	DateTime local_epoch;

	private time_t_CustomMarshaler () 
	{
		utc_offset = (int) DateTime.Now.Subtract (DateTime.UtcNow).TotalSeconds;
		local_epoch = new DateTime (1970, 1, 1, 0, 0, 0);
	}

	public static ICustomMarshaler GetInstance (string cookie)
	{
		if (marshaler == null)
			marshaler = new time_t_CustomMarshaler ();

		return marshaler;
	}

	public IntPtr MarshalManagedToNative (object obj)
	{
		//
		// This method should return a pointer to a memory buffer holding
		// the unmanaged representation of 'obj'
		// The first 4 bytes of the buffer is unused (is this really 4 bytes on a 64bit machine?)
		// The unmanaged function will receive the address of the buffer
		// as the parameter
		//

		DateTime dt = (DateTime) obj;
		int size = Marshal.SizeOf (typeof (int)) + GetNativeDataSize ();
		IntPtr ptr = Marshal.AllocCoTaskMem (size);
		int secs = ((int)dt.Subtract (local_epoch).TotalSeconds) + utc_offset;
		if (GetNativeDataSize () == 4)
			Marshal.WriteInt32 (ptr, secs);
		else if (GetNativeDataSize () == 8)
			Marshal.WriteInt64 (ptr, secs);
		else
			throw new Exception ("Unexpected native size for time_t.");

		return ptr;
	}

	public void CleanUpNativeData (IntPtr data)
	{
		Marshal.FreeHGlobal (data);
	}

	public object MarshalNativeToManaged (IntPtr data)
	{
		//
		// This function receives the return value of the unmanaged function
		// as a pointer.
		//

		int secs;
		secs = (int)data;

		TimeSpan span = new TimeSpan (0, 0, secs - utc_offset);
		return local_epoch.Add (span);
	}

	public void CleanUpManagedData (object obj)
	{
	}

    [DllImport("libtest")]
	private static extern int time_t_sizeof ();

	public int GetNativeDataSize ()
	{
		return time_t_sizeof ();
	}
}

public class Testing
{
	[DllImport("libtest")]
	[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(MyMarshal))]
	private static extern string functionReturningString();

    [DllImport("libtest", EntryPoint="mono_test_marshal_time_t")]
	[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(time_t_CustomMarshaler))]
	private static extern DateTime mono_test_marshal_time_t (
		 [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(time_t_CustomMarshaler))]
		 DateTime t);

	public static int Main()
	{
		string res = functionReturningString();
		Console.WriteLine ("native string function returns {0}", res);

		if (res != "*ABC*")
			return 1;

		DateTime d = DateTime.Now;
		DateTime d2 = mono_test_marshal_time_t (d);

		if (((d2 - d).TotalSeconds < 3599) || ((d2 - d).TotalSeconds > 3601))
			return 2;

		return 0;
	}



}
