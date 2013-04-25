using System;
using Mono.Simd;
using System.Threading;

class A {
	public void Print() { Console.WriteLine("A"); }
}

class B : A {
	public void Print() { Console.WriteLine("B"); }
}

class ThreadRunner {
	public Int32 Inc2(Int32 a) { return Inc1(a); }
	public Int32 Inc1(Int32 a) { return a + 2; }
	public void PrintA(A a) { a.Print(); ((B)a).Print(); }
	public void Run() {
		Console.WriteLine("Running thread" );
		B b = new B();
		Int32 a=0;
		for(int i = 0; i < 1000000; i++) {
			a = Inc2(a);
			if(i % 100000 == 0) PrintA(b);
		}
		Console.WriteLine("Ending thread");
	}
}


class Extensions { public static string BogusProperty { get; set; } }

class RuntimeServices {
	public System.Reflection.MemberInfo[] members = typeof(Extensions).GetMembers();
	public void Run() {
		foreach (var m in members) System.Console.WriteLine(m);
	}
}

class Tests {
	struct myvt {
		public int X;
		public int Y;
	}

	static int test_0_vector4i_cmp_gt () {
		Vector4i a = new Vector4i (10, 5, 12, -1);
		Vector4i b = new Vector4i (-1, 5, 10, 10);

		Vector4i c = a.CompareGreaterThan (b);

		if (c.X != -1)
			return 1;
		if (c.Y != 0)
			return 2;
		if (c.Z != -1)
			return 3;
		if (c.W != 0)
			return 4;
		return 0;
	}

	static myvt CompareGT(myvt a, myvt b) {
		myvt r;
		r.X = a.X > b.X ? -1 : 0;
		r.Y = a.Y > b.Y ? -1 : 0;
		return r;
	}

	static int test_0_struct2i_cmp_gt() {
		myvt a;
		myvt b;
		a.X = 10;
		a.Y = 5;
		b.X = -1;
		b.Y = 5;
		myvt c = CompareGT(a, b);
		if (c.X != -1)
			return 1;
		if (c.Y != 0)
			return 2;
		return 0;
	}

	static int vararg_sum(params int[] args) {
		int sum = 0;
		foreach(int arg in args) {
			sum += arg;
		}
		return sum;
	}
	static int test_21_vararg_test() {
		int sum = 0;
		sum += vararg_sum();
		sum += vararg_sum(1);
		sum += vararg_sum(2, 3);
		sum += vararg_sum(4, 5, 6);
		return sum;
	}

	static int test_0_threads() {
		// Run a bunch of threads, make them JIT some code and
		// do some casts
		ThreadRunner runner = new ThreadRunner();
		Thread[] threads = new Thread[10];
		for (int i = 0; i < 10; i++) {
			threads[i] = new Thread(new ThreadStart(runner.Run));
			threads[i].Start();
		}
		for (int i = 0; i < 10; i++) {
			threads[i].Join();
		}
		return 0;
	}


	static int test_0_reflection() {
		RuntimeServices r = new RuntimeServices();
		r.Run();
		return 0;
	}

	public class BaseClass {
	}

	public class LongClass : BaseClass {
		public long Value;
		public LongClass(long val) { Value = val; }
	}

	static public long add_two_LongClass(BaseClass l1, BaseClass l2) {
		long l = checked (((LongClass)l1).Value + ((LongClass)l2).Value);
		return l;
	}

	static int test_0_laddcc() {
		long l = add_two_LongClass(new LongClass(System.Int64.MinValue), new LongClass(1234));
		if (l == 1234)
			return 1;
		return 0;
	}

	public static int Main(String[] args) {
		return TestDriver.RunTests(typeof(Tests));
	}
}
