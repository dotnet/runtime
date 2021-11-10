//
// From test: Bug 348522
//
using System;
using System.Reflection;
using System.Globalization;

public struct SimpleStruct {
	public int a;
	public int b;

	public SimpleStruct (int a, int b)
	{
		this.a = a;
		this.b = b;
	}
}

class NullableTestClass
{
	public bool hasValue;
	public int bVal;

	public void F (SimpleStruct? code)
	{
		if (hasValue = code.HasValue)
			bVal = code.Value.b;
	}
}

class PrimitiveTestClass
{
	public int val;

	public void i4 (int code) {
		val = code;
	}
}

struct GenericStruct<T>
{
	T t;
}

class GenericClass<T>
{
	T t;
}

class Driver
{
	public static GenericStruct<T> StructTest <T> (GenericStruct <T> t)
	{
		return t;
	}

	public static GenericClass<T> ReferenceTest <T> (GenericClass <T> t)
	{
		return t;
	}

	static int Main ()
	{
		BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod;
		MethodInfo mi = typeof (NullableTestClass).GetMethod ("F");
		NullableTestClass nullable = new NullableTestClass ();
		SimpleStruct? test = new SimpleStruct (90, 90);


		mi.Invoke (nullable, flags, new PassesStuffBinder (null), new object [] {null}, null);
		if (nullable.hasValue) {
			Console.WriteLine ("invoked nullabled with null arg but did not get a null in the method");
			return 1;
		}


		nullable = new NullableTestClass ();
		mi.Invoke (nullable, flags, new PassesStuffBinder (new SimpleStruct (10, 20)), new object [] {200}, null);
		if (!nullable.hasValue || nullable.bVal != 20) {
			Console.WriteLine ("invoked nullabled with boxed struct, but did not get it");
			return 2;
		}
		

		nullable = new NullableTestClass ();
		mi.Invoke (nullable, flags, new PassesStuffBinder (test), new object [] {200}, null);
		if (!nullable.hasValue || nullable.bVal != 90) {
			Console.WriteLine ("invoked nullabled with nullable literal, but did not get it");
			return 3;
		}

		mi = typeof (PrimitiveTestClass).GetMethod ("i4");
		PrimitiveTestClass prim = new PrimitiveTestClass ();
		mi.Invoke (prim, flags, new PassesStuffBinder ((byte)10), new object [] {88}, null);
		if (prim.val != 10) {
			Console.WriteLine ("invoked primitive with byte, it should be widened to int "+ prim.val);
			return 4;
		}

		try {
			mi.Invoke (prim, flags, new PassesStuffBinder (Missing.Value), new object [] {null}, null);
			Console.WriteLine ("invoked literal with reference value");
			return 5;
		} catch (Exception) {

		}		

		try {
			MethodInfo method = typeof (Driver).GetMethod ("StructTest");
			MethodInfo generic_method = method.MakeGenericMethod (typeof (int));
			generic_method.Invoke (null, new object [] { new GenericStruct<int>() });

			method = typeof (Driver).GetMethod ("ReferenceTest");
			generic_method = method.MakeGenericMethod (typeof (int));
			generic_method.Invoke (null, new object [] { new GenericClass<int>() });
		} catch (Exception e) {
			Console.WriteLine ("calling with generic arg failed "+e);
			return 6;
		}

		return 0;
	}
}

class PassesStuffBinder : BaseBinder
{
	object stuff = null;

	public PassesStuffBinder (object stuff)
	{
		this.stuff = stuff;
	}

	public override object ChangeType (object value, Type type1, CultureInfo culture)
	{
		return stuff;
	}
}


class BaseBinder : Binder {
	public override MethodBase BindToMethod (BindingFlags bindingAttr, MethodBase [] match, ref object [] args,
						 ParameterModifier [] modifiers, CultureInfo culture, string [] names,
						 out object state)
	{
		state = null;
		return match [0];
	}
	
	public override object ChangeType (object value, Type type1, CultureInfo culture)
	{
		return (ulong) 0xdeadbeefcafebabe;
	}
	
	// The rest is just to please the compiler
	public override FieldInfo BindToField (System.Reflection.BindingFlags a,
					       System.Reflection.FieldInfo[] b, object c, System.Globalization.CultureInfo d)
	{
		return null;
	}
	
	public override void ReorderArgumentArray(ref object[] a, object b) {
	}
	
	public override MethodBase SelectMethod(System.Reflection.BindingFlags
						a, System.Reflection.MethodBase[] b, System.Type[] c,
						System.Reflection.ParameterModifier[] d) {
		return null;
	}
	
	public override PropertyInfo 
	    SelectProperty(System.Reflection.BindingFlags a,
			   System.Reflection.PropertyInfo[] b, System.Type c, System.Type[] d,
			   System.Reflection.ParameterModifier[] e) {
		return null;
	}
}
