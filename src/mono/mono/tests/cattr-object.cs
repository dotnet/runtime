using System;

[My((long)1)]
[My(TypeCode.Empty)]
[My(typeof(System.Enum))]
class T {
	static int Main() {
		object[] a = Attribute.GetCustomAttributes (typeof (T), false);
		if (a.Length != 3)
			return 1;
		foreach (object o in a) {
			My attr = (My)o;
			if (attr.obj.GetType () == typeof(long)) {
				long val = (long)attr.obj;
				Console.WriteLine ("got value: {0}", val);
				if (val != 1)
					return 2;
			} else if (attr.obj.GetType () == typeof(int)) {
				int val = (int)attr.obj;
				if (val != (int)TypeCode.Empty)
					return 3;
			} else {
				Type t = attr.obj as Type;
				if (t == null)
					return 4;
				if (t != typeof (System.Enum))
					return 5;
			}
			
		}
		return 0;
	}
}

[AttributeUsage(AttributeTargets.All,AllowMultiple=true)]
class My : Attribute {
	public object obj;
	public My (object o) {
		obj = o;
	}
}
