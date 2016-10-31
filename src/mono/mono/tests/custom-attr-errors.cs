using System;
using System.Reflection;

// this for test_0_missing_attr_on_assembly
[assembly: MissingAttribute]

public sealed class MyAttribute : Attribute
{
	public Type Type { get; set; }
	public MyAttribute (Type t) {
		Type = t;
		// throw new Exception ();
	}

	public override string ToString () {
		return "my " + Type;
	}
}

public sealed class My2Attribute : Attribute
{
	public object Obj { get; set; }
	public My2Attribute (object t) {
		Obj = t;
		// throw new Exception ();
	}

	public override string ToString () {
		return "my2 " + Obj;
	}
}

public sealed class My3Attribute : Attribute
{
	public My3Attribute (object[] arr) {
	}
}

public class MyException : Exception {}
public sealed class ExceptionOnCtor : Attribute
{
	public ExceptionOnCtor () {
		throw new MyException ();
	}
}

public class Bar {}

class Tests {

	public static int test_0_missing_attr_on_assembly () {
		try {
			Assembly.GetExecutingAssembly().GetCustomAttributes (false);
			return 1;
		} catch (TypeLoadException exn) {
			return 0;
		}
	}

	[My3 (new object[] { DisappearingEnum.V0 })]
	public static int test_0_missing_enum_arg_alt3 () {
		try {
			MethodBase.GetCurrentMethod ().GetCustomAttributes (false);
			return 1;
		} catch (TypeLoadException) {
			return 0;
		}
	}

	[My2 (new DisappearingEnum[] { DisappearingEnum.V0 })]
	public static int test_0_missing_enum_arg_alt2 () {
		try {
			MethodBase.GetCurrentMethod ().GetCustomAttributes (false);
			return 1;
		} catch (TypeLoadException) {
			return 0;
		}
	}

	[My2 (new object[] { DisappearingEnum.V0 })]
	public static int test_0_missing_enum_arg_alt () {
		try {
			MethodBase.GetCurrentMethod ().GetCustomAttributes (false);
			return 1;
		} catch (TypeLoadException) {
			return 0;
		}
	}

	[My2 (DisappearingEnum.V0)]
	public static int test_0_missing_enum_arg () {
		try {
			MethodBase.GetCurrentMethod ().GetCustomAttributes (false);
			return 1;
		} catch (TypeLoadException) {
			return 0;
		}
	}

	[My3 (new object[] { typeof (DisappearingType)})] 
	public static int test_0_array_of_missing_type_alt2 () {
		try {
			MethodBase.GetCurrentMethod ().GetCustomAttributes (false);
			return 1;
		} catch (TypeLoadException) {
			return 0;
		}
	}
	[My2 (new Type[] { typeof (DisappearingType)})] 
	public static int test_0_array_of_missing_type () {
		try {
			MethodBase.GetCurrentMethod ().GetCustomAttributes (false);
			return 1;
		} catch (TypeLoadException) {
			return 0;
		}
	}



	[My2 (typeof (DisappearingType))]
	public static int test_0_missing_type_arg_alt () {
		try {
			MethodBase.GetCurrentMethod ().GetCustomAttributes (false);
			return 1;
		} catch (TypeLoadException) {
			return 0;
		}
	}

	[My (typeof (DisappearingType))]
	public static int test_0_missing_type_arg () {
		try {
			MethodBase.GetCurrentMethod ().GetCustomAttributes (false);
			return 1;
		} catch (TypeLoadException) {
			return 0;
		}
	}


	[MissingCtor (1)]
	public static int test_0_missing_ctor () {
		try {
			MethodBase.GetCurrentMethod ().GetCustomAttributes (false);
			return 1;
		} catch (MissingMethodException) {
			return 0;
		}
	}

	[BadAttr (Field = 1)]
	public static int test_0_missing_field () {
		try {
			MethodBase.GetCurrentMethod ().GetCustomAttributes (false);
			return 1;
		} catch (CustomAttributeFormatException) {
			return 0;
		}
	}

	[BadAttr (Property = 1)]
	public static int test_0_missing_property () {
		try {
			MethodBase.GetCurrentMethod ().GetCustomAttributes (false);
			return 1;
		} catch (CustomAttributeFormatException) {
			return 0;
		}
	}

	/* FIXME Verify the type of the cattr with the one on the field/property
	[BadAttr (Field2 = 1)]
	public static int test_0_bad_field () {
		try {
			MethodBase.GetCurrentMethod ().GetCustomAttributes (false);
			return 1;
		} catch (CustomAttributeFormatException) {
			return 0;
		}
	}

	[BadAttr (Property2 = 1)]
	public static int test_0_bad_property () {
		try {
			MethodBase.GetCurrentMethod ().GetCustomAttributes (false);
			return 1;
		} catch (CustomAttributeFormatException) {
			return 0;
		}
	}
	*/

	[BadAttr (Property3 = 1)]
	public static int test_0_bad_property_no_setter () {
		try {
			MethodBase.GetCurrentMethod ().GetCustomAttributes (false);
			return 1;
		} catch (CustomAttributeFormatException) {
			return 0;
		}
	}

	[ExceptionOnCtor]
	public static int test_0_cattr_ctor_throws () {
		try {
			MethodBase.GetCurrentMethod ().GetCustomAttributes (false);
			return 1;
		} catch (MyException) {
			return 0;
		}
	}


    static int Main (String[] args) {
            return TestDriver.RunTests (typeof (Tests), args);
    }

}
