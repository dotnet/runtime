
using System;

namespace Test {
	public interface A {
		void method ();
	}
	public interface B : A {
		void method2 ();
	}
	public class test {
		public static int Main () {
			Type int_type = typeof (int);
			Type obj_type = typeof (object);
			Type vt_type = typeof (System.ValueType);
			Type comp_type = typeof (System.IComparable);
			Type a_type = typeof (A);
			Type b_type = typeof (B);
			int error = 1;

			if (!int_type.IsSubclassOf(vt_type))
				return error;
			++error;
			if (!int_type.IsSubclassOf(obj_type))
				return error;
			++error;
			if (int_type.IsSubclassOf(comp_type))
				return error;
			++error;

			if (int_type.IsAssignableFrom(vt_type))
				return error;
			++error;
			if (int_type.IsAssignableFrom (obj_type))
				return error;
			++error;
			if (int_type.IsAssignableFrom(comp_type))
				return error;
			++error;
	
			if (!int_type.IsAssignableFrom(int_type))
				return error;
			++error;
			if (!obj_type.IsAssignableFrom (int_type))
				return error;
			++error;
			if (!vt_type.IsAssignableFrom(int_type))
				return error;
			++error;
			if (!comp_type.IsAssignableFrom(int_type))
				return error;
			++error;
	
			if (a_type.IsSubclassOf(b_type))
				return error;
			++error;
			if (b_type.IsAssignableFrom(a_type))
				return error;
			++error;
			if (b_type.IsSubclassOf(a_type))
				return error;
			++error;
			if (!a_type.IsAssignableFrom(b_type))
				return error;
			++error;
			return 0;
		}
	}
}
