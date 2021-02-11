/* This test is extracted from System.Core tests, that happens to be
 * problematic if *all* assemblies are full-aot'd, but the interpreter is still
 * used for SRE.
 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;

public class Repro {
	public class Foo {
		public string Bar;
		public string Baz;

		public Gazonk Gaz;

		public Gazonk Gazoo { get; set; }

		public string Gruik { get; set; }

		public Foo ()
		{
			Gazoo = new Gazonk ();
			Gaz = new Gazonk ();
		}
	}

	public class Gazonk {
		public string Tzap;

		public int Klang;

		public string Couic { get; set; }

		public string Bang () { return ""; }
	}

	public static int CompiledMemberBinding ()
	{
		var getfoo = Expression.Lambda<Func<Foo>> (
				Expression.MemberInit (
					Expression.New (typeof (Foo)),
					Expression.MemberBind (
						typeof (Foo).GetProperty ("Gazoo"),
						Expression.Bind (typeof (Gazonk).GetField ("Tzap"),
							Expression.Constant ("tzap")),
						Expression.Bind (typeof (Gazonk).GetField ("Klang"),
							Expression.Constant (42))))).Compile ();

		var foo = getfoo ();

		if (foo == null)
			return 2;
		if (foo.Gazoo.Klang != 42)
			return 3;
		if (foo.Gazoo.Tzap != "tzap")
			return 4;

		return 0;
	}

	public static int Main (string []args)
	{
		return CompiledMemberBinding ();
	}
}
