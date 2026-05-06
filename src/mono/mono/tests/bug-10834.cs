using System;
using System.Reflection;

namespace Repro {
	class Program
	{
		static bool Check (Type t)
		{
			Console.WriteLine ($"--- {t}");

			var m = t.GetMethod ("M1");
			Console.WriteLine (m);

			foreach(var p in m.GetParameters ())
			Console.WriteLine ($"{p}: {p.ParameterType} / {p.GetRequiredCustomModifiers().Length}");

			Console.WriteLine ();
			return m.GetParameters()[0].GetRequiredCustomModifiers().Length == 1;
		}

		static int Main(string[] args)
		{
			if (!Check (typeof (C<>)))
				return 1;
			if (!Check (typeof (C<S1>)))
				return 2;

			var o = new Bug ();
			int res = o.M1 (new S1 ());
			Console.WriteLine (res);
			if (res != 0)
				return 3;
			Console.WriteLine ("All good");
			return 0;
		}
	}
	abstract class C<U>
	{
		public abstract int M1<T>(in T arg) where T : U, I1;
	}

	class Bug : C<S1>
	{
		public override int M1<T2> (in T2 arg)
		{
			Console.WriteLine ("C<S1>::M1");
			arg.M3();
			return arg.M4();
		}
	}

	interface I1
	{
		void M3();
		int M4();
	}

	public struct S1: I1
	{
		public int field;
		public void M3 ()
		{
			Console.WriteLine ("S1:M3");
			field = 42;
		}

		public int M4() {
			Console.WriteLine ("S1:M4 {0}", field);
			
			return field;
		}
	}
}
