using System;
using System.Runtime.InteropServices;

// in this test we spend 
// 30% of the time locking
// 10 % allocating the handles
class T {

	static GCHandle[] handle_array;

	static int count = 4 * 400000; /* multiple of handle types */
	static int loops = 2;

	static void build_array () {
		int i;
		handle_array = new GCHandle [count];

		for (i = 0; i < count; ++i) {
			GCHandleType t = (GCHandleType) (i & 3);
			handle_array [i] = GCHandle.Alloc (i, t);
		}
	}
	static void get_stats (){
		int i;
		object o;
		int has_target = 0;
		int is_allocated = 0;
		int normal_reclaimed = 0;
		for (i = 0; i < count; ++i) {
			GCHandleType t = (GCHandleType) (i & 3);
			if (handle_array [i].IsAllocated)
				is_allocated++;
			else
				continue;
			o = handle_array [i].Target;
			if (o != null) {
				has_target++;
				int val = (int)o;
				if (val != i)
					Console.WriteLine ("obj at {0} inconsistent: {1}", i, val);
			} else {
				if (t == GCHandleType.Normal || t == GCHandleType.Pinned) {
					normal_reclaimed++;
				}
			}
		}
		Console.WriteLine ("allocated: {0}, has target: {1}, normal reclaimed: {2}", is_allocated, has_target, normal_reclaimed);
	}

	static void free_some (int d) {
		int i;
		int freed = 0;
		for (i = 0; i < count; ++i) {
			if ((i % d) == 0) {
				if (handle_array [i].IsAllocated) {
					handle_array [i].Free ();
					freed++;
				}
			}
		}
		Console.WriteLine ("freed: {0}", freed);
	}

	static void alloc_many () {
		int small_count = count / 2;
		GCHandle[] more = new GCHandle [small_count];
		int i;
		for (i = 0; i < small_count; ++i) {
			GCHandleType t = (GCHandleType) (i & 3);
			more [i] = GCHandle.Alloc (i, t);
		}
		for (i = 0; i < small_count; ++i) {
			more [i].Free ();
		}
		Console.WriteLine ("alloc many: {0}", small_count);
	}

	static void Main (string[] args) {
		if (args.Length > 0)
			count = 4 * int.Parse (args [0]);
		if (args.Length > 1)
			loops = int.Parse (args [1]);

		for (int j = 0; j < loops; ++j) {
			do_one ();
		}
	}

	static void do_one () {
		Console.WriteLine ("start");
		build_array ();
		get_stats ();
		GC.Collect ();
		Console.WriteLine ("after collect");
		get_stats ();
		free_some (10);
		Console.WriteLine ("after free(10)");
		get_stats ();
		free_some (4);
		Console.WriteLine ("after free(4)");
		get_stats ();
		GC.Collect ();
		Console.WriteLine ("after collect");
		get_stats ();
		for (int i = 0; i < 10; ++i)
			alloc_many ();
		Console.WriteLine ("after alloc_many");
		get_stats ();
		free_some (1);
		Console.WriteLine ("after free all");
		get_stats ();
		GC.Collect ();
		Console.WriteLine ("after collect");
		get_stats ();
	}
}

