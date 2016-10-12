//
// reference-loader.cs:
//
//  Test for reference assembly loading

using System;
using System.IO;
using System.Reflection;

public class Tests {
	public static int Main (string[] args)
	{
		return TestDriver.RunTests (typeof (Tests), args);
	}

	public static int test_0_loadFrom_reference ()
	{
		// Check that loading a reference assembly by filename for execution is an error
		try {
			var a = Assembly.LoadFrom ("./TestingReferenceAssembly.dll");
		} catch (BadImageFormatException exn) {
			// .NET Framework 4.6.2 throws BIFE here.
			return 0;
		}
		return 1;
	}

	public static int test_0_load_reference ()
	{
		// Check that loading a reference assembly for execution is an error
		try {
			var an = new AssemblyName ("TestingReferenceAssembly");
			var a = Assembly.Load (an);
		} catch (FileNotFoundException exn) {
			return 0;
		} catch (BadImageFormatException exn) {
			// .NET Framework 4.6.2 throws BIFE here.
			return 0;
		}
		return 1;
	}

	public static int test_0_reflection_load_reference ()
	{
		// Check that reflection-only loading a reference assembly is okay
		var an = new AssemblyName ("TestingReferenceAssembly");
		var a = Assembly.ReflectionOnlyLoad (an.FullName);
		var t = a.GetType ("X");
		var f = t.GetField ("Y");
		if (f.FieldType.Equals (typeof (Int32)))
			return 0;
		return 1;
	}

	public static int test_0_load_reference_asm_via_reference ()
	{
		// Check that loading an assembly that references a reference assembly doesn't succeed.
		var an = new AssemblyName ("TestingReferenceReferenceAssembly");
		try {
			var a = Assembly.Load (an);
			var t = a.GetType ("Z");
		} catch (FileNotFoundException){
			return 0;
		}
		return 1;
	}

	public static int test_0_reflection_load_reference_asm_via_reference ()
	{
		// Check that reflection-only loading an assembly that
		// references a reference assembly is okay.
		var an = new AssemblyName ("TestingReferenceReferenceAssembly");
		var a = Assembly.ReflectionOnlyLoad (an.FullName);
		var t = a.GetType ("Z");
		var f = t.GetField ("Y");
		if (f.FieldType.Equals (typeof (Int32)))
			return 0;
		return 1;
	}


	public static int test_0_load_reference_bytes ()
	{
		// Check that loading a reference assembly from a byte array for execution is an error
		byte[] bs = File.ReadAllBytes ("./TestingReferenceAssembly.dll");
		try {
			var a = Assembly.Load (bs);
		} catch (BadImageFormatException) {
			return 0;
		} catch (FileNotFoundException exn) {
			Console.Error.WriteLine ("incorrect exn was {0}", exn);
			return 2;
		}
		return 1;
	}

	public static int test_0_reflection_load_reference_bytes ()
	{
		// Check that loading a reference assembly from a byte
		// array for reflection only is okay.
		byte[] bs = File.ReadAllBytes ("./TestingReferenceAssembly.dll");
		var a = Assembly.ReflectionOnlyLoad (bs);
		var t = a.GetType ("X");
		var f = t.GetField ("Y");
		if (f.FieldType.Equals (typeof (Int32)))
			return 0;
		return 1;
	}

}
