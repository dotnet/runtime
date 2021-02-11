using System;

namespace Test {
	public class Test {
		public static int Main () {
			Attribute[] attr_array = new Attribute [1];
			object obj = (object) attr_array;
			object [] obj_array = (object[]) obj;

			obj_array = obj as object[];

			if (obj_array == null)
				return 1;
			
			return 0;
		}
	}
}
