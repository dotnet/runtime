
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
			return 0;
		}
	}
}
