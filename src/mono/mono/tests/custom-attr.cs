
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
	[My("testclass")]
	[My2("testclass", 22)]
	public class Test {
		static public int Main() {
			System.Reflection.MemberInfo info = typeof (Test);
			object[] attributes = info.GetCustomAttributes (false);
			for (int i = 0; i < attributes.Length; i ++) {
				System.Console.WriteLine(attributes[i]);
			}
			if (attributes.Length != 2)
				return 1;
			MyAttribute attr = (MyAttribute) attributes [0];
			if (attr.val != "testclass")
				return 2;
			return 0;
		}
	}
}
