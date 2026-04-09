
using System;
using System.Reflection;

namespace Test {
	public class MyAttribute: Attribute {
		public string val;
		public MyAttribute (string stuff) {
			System.Console.WriteLine (stuff);
			val = stuff;
		}
	}
	public class My2Attribute: MyAttribute {
		public int ival;
		public My2Attribute (string stuff, int blah) : base (stuff) {
			System.Console.WriteLine ("ctor with int val"+stuff);
			ival = blah;
		}
	}

	public class My3Attribute : Attribute {
		char[] array_val;

		public char[] Prop {
			get {
				return array_val;
			}
			set {
				array_val = value;
			}
		}

		public char[] Prop2;
	}

	class XAttribute : Attribute {
		public XAttribute () 
		{
			throw new Exception ("X");
		}
	}

	interface ZInterface {
    }

	class ZAttribute : Attribute, ZInterface {
	}

	[X, Z, Serializable]
	class Y {
	}

	[My("arg\0string\0with\0nuls")]
	class NulTests {
	}
			
	[My("testclass")]
	[My2("testclass", 22)]
	[My3(Prop = new char [] { 'A', 'B', 'C', 'D' }, Prop2 = new char [] { 'A', 'D' })]
	public class Test {
		static public int Main() {
			System.Reflection.MemberInfo info = typeof (Test);
			object[] attributes = info.GetCustomAttributes (false);
			for (int i = 0; i < attributes.Length; i ++) {
				System.Console.WriteLine(attributes[i]);
			}
			if (attributes.Length != 3)
				return 1;
			for (int i = 0; i < attributes.Length; ++i) {
				if (attributes [i] is MyAttribute) {
					if (((MyAttribute)attributes [i]).val != "testclass")
						return 2;
				}
				if (attributes [i] is My3Attribute) {
					if (new String (((My3Attribute)attributes [i]).Prop) != "ABCD") {
						Console.WriteLine (new String (((My3Attribute)attributes [i]).Prop));
						return 3;
					}
					if (new String (((My3Attribute)attributes [i]).Prop2) != "AD") {
						Console.WriteLine (new String (((My3Attribute)attributes [i]).Prop2));
						return 4;
					}
				}
			}

			//
			// Test that requesting a specific custom attributes does not
			// create all the others
			//

			typeof (Y).IsDefined (typeof (ZAttribute), true);
			typeof (Y).IsDefined (typeof (XAttribute), true);

			typeof (Y).GetCustomAttributes (typeof (ZAttribute), true);

			try {
				typeof (Y).GetCustomAttributes (true);
				return 4;
			}
			catch {
			}

			if (typeof (Y).GetCustomAttributes (typeof (ZInterface), true).Length != 1)
				return 5;

			if (!typeof (Y).IsDefined (typeof (ZInterface), true))
				return 6;

			// Test that synthetic methods have no attributes
			if (typeof(int[,]).GetConstructor (new Type [] { typeof (int), typeof (int) }).GetCustomAttributes (true).Length != 0)
				return 7;

			// Test that nuls are preserved (see Xamarin bug 5732)
			if (((MyAttribute)typeof (NulTests).GetCustomAttributes (true)[0]).val != "arg\0string\0with\0nuls")
				return 8;

			return 0;
		}
	}
}
