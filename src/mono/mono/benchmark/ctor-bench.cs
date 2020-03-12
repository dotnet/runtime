using System;
using System.Reflection;

class T {

	public T () {
	}

	const int count = 1000000;

	static void use_new () {
		for (int i = 0; i < count; ++i) 
			new T ();
	}

	object Clone () {
		return MemberwiseClone ();
	}
	
	static void use_clone () {
		T t = new T ();
		for (int i = 0; i < count; ++i) 
			t.Clone ();
	}
	
	static void use_activator () {
		for (int i = 0; i < count; ++i) 
			Activator.CreateInstance (typeof (T));
	}
	
	static void use_ctor () {
		ConstructorInfo ctor = typeof (T).GetConstructor (Type.EmptyTypes);
		for (int i = 0; i < count; ++i) 
			ctor.Invoke (null);
	}
	
	static void Main () {
		long start, end, new_val, perc;
		start = Environment.TickCount;

		start = Environment.TickCount;
		use_new ();
		end = Environment.TickCount;
		Console.WriteLine ("new took {0}", end-start);
		new_val = end-start;

		start = Environment.TickCount;
		use_clone ();
		end = Environment.TickCount;
		perc = ((end-start-new_val) * 100) / new_val;
		Console.WriteLine ("clone took {0} {1} %", end-start, perc);

		start = Environment.TickCount;
		use_activator ();
		end = Environment.TickCount;
		perc = ((end-start-new_val) * 100) / new_val;
		Console.WriteLine ("activator took {0} {1} %", end-start, perc);

		start = Environment.TickCount;
		use_ctor ();
		end = Environment.TickCount;
		perc = ((end-start-new_val) * 100) / new_val;
		Console.WriteLine ("ctor took {0} {1} %", end-start, perc);

	}
}

