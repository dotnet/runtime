using System;

namespace B
{
	public class MyAttribute : Attribute {
		public Type Type { get; set; }
		public MyAttribute (Type t) {
			Type = t;
		}
		public override string ToString () {
			return "My " + Type;
		}
	}

	[My (typeof (A.ClassA))]
	public class ClassB { // A.AnotherClassA

		public ClassB () {
			Console.WriteLine ("IN B");
			Console.WriteLine (typeof (ClassB).AssemblyQualifiedName);
			var t = Type.GetType ("B.ClassB, reflection-load-with-context-lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
			Console.WriteLine (t);
			t = Type.GetType ("A.ClassA, reflection-load-with-context-second-lib, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
			Console.WriteLine ("class a: {0}", t);
			if (t == null)
				throw new Exception ("FAIL");
		}
	}
}