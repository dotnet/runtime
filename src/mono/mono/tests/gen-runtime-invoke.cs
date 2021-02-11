/*
 * Test generator for runtime invoke tests.
 */
using System;
using System.IO;

public class Tests
{
	public static void Main (String[] args) {
		/* There are multiple approaches, we generate c# directly */

		using (var w = new StreamWriter (Console.OpenStandardOutput ())) {
			w.WriteLine ("using System;");
			w.WriteLine ("using System.Reflection;");
			w.WriteLine ();

			// Struct with 2 int fields
			w.WriteLine ("public struct FooStruct { public int i, j; public static bool operator == (FooStruct f1, FooStruct f2) { return f1.i == f2.i && f1.j == f2.j; } public static bool operator != (FooStruct f1, FooStruct f2) { return f1.i != f2.i || f1.j != f2.j; } public override bool Equals (object obj) { return this == (FooStruct)obj; } public override int GetHashCode () { return 0; } }");

			// Struct with 1 long field
			w.WriteLine ("public struct FooStruct2 { public long i; public static bool operator == (FooStruct2 f1, FooStruct2 f2) { return f1.i == f2.i; } public static bool operator != (FooStruct2 f1, FooStruct2 f2) { return f1.i != f2.i; } public override bool Equals (object obj) { return this == (FooStruct2)obj; } public override int GetHashCode () { return 0; } }");

			// Struct with 2 bool fields
			w.WriteLine ("public struct FooStruct3 { public bool i, j; public static bool operator == (FooStruct3 f1, FooStruct3 f2) { return f1.i == f2.i && f1.j == f2.j; } public static bool operator != (FooStruct3 f1, FooStruct3 f2) { return f1.i != f2.i || f1.j != f2.j; } public override bool Equals (object obj) { return this == (FooStruct3)obj; } public override int GetHashCode () { return 0; } }");

			w.WriteLine ("public class Tests {");
			w.WriteLine ("    public static int Main (String[] args) {");
			w.WriteLine ("        return TestDriver.RunTests (typeof (Tests), args);");
			w.WriteLine ("    }");

			// int
			GenCase (w, "int", "42", new string [] { "int", "uint" }, new string [] { "Int32.MinValue", "UInt32.MaxValue" });

			// byref int
			GenCase (w, "int", "42", new string [] { "ref int" }, new string [] { "Int32.MinValue" });

			// short
			GenCase (w, "short", "42", new string [] { "short", "ushort" }, new string [] { "Int16.MinValue", "UInt16.MaxValue" });

			// bool
			GenCase (w, "bool", "true", new string [] { "bool", "bool", "bool" }, new string [] { "true", "false", "true" });

			// char
			GenCase (w, "char", "'A'", new string [] { "char", "char", "char" }, new string [] { "'A'", "'B'", "'C'" });

			// long
			GenCase (w, "long", "0x12345678AL", new string [] { "long", "long" }, new string [] { "0x123456789L", "0x123456789L" });

			// long in an odd numbered register
			GenCase (w, "long", "0x12345678AL", new string [] { "int", "long", "long" }, new string [] { "1", "0x123456789L", "0x123456789L" });

			// long in split reg/stack on arm
			GenCase (w, "void", "", new string [] { "int", "int", "int", "long" }, new string [] { "1", "2", "3", "0x123456789L" });

			// vtype in split reg/stack on arm
			GenCase (w, "void", "", new string [] { "int", "int", "int", "FooStruct" }, new string [] { "1", "2", "3", "new FooStruct () { i = 1, j = 2 }" });

			// 8 aligned vtype in split reg/stack on arm
			GenCase (w, "void", "", new string [] { "int", "int", "int", "FooStruct2" }, new string [] { "1", "2", "3", "new FooStruct2 () { i = 0x123456789L }" });

			// vtype entirely on the stack on arm
			GenCase (w, "void", "", new string [] { "int", "int", "int", "int", "FooStruct" }, new string [] { "1", "2", "3", "4", "new FooStruct () { i = 1, j = 2 }" });

			// vtype with size 2 in a register on arm
			GenCase (w, "FooStruct3", "new FooStruct3 () { i = true, j = false }", new string [] { "FooStruct3" }, new string [] { "new FooStruct3 () { i = true, j = false }" });

			// float
			GenCase (w, "void", "", new string [] { "float" }, new string [] { "0.123f" });

			// float on the stack on arm
			GenCase (w, "void", "", new string [] { "int", "int", "int", "int", "float" }, new string [] { "1", "2", "3", "4", "0.123f" });

			// float ret
			GenCase (w, "float", "0.123f", new string [] { }, new string [] { });

			// double
			GenCase (w, "void", "", new string [] { "double" }, new string [] { "0.123f" });

			// double in split reg/stack on arm
			GenCase (w, "void", "", new string [] { "int", "int", "int", "double" }, new string [] { "1", "2", "3", "0.123f" });

			// double ret
			GenCase (w, "double", "0.123f", new string [] { }, new string [] { });

			w.WriteLine ("}");
		}
	}

	static int testid_gen;

	static void WriteList (StreamWriter w, string[] values) {
		int i = 0;
		foreach (string v in values) {
			if (i > 0)
				w.Write (", ");
			w.Write (v);
			i ++;
		}
	}

	public static void GenCase (StreamWriter w, string retType, string retVal, string[] types, string[] values) {
		testid_gen ++;

		string callee_name = "meth_" + testid_gen;

		/* The caller */
		w.WriteLine ("\tpublic static int test_0_" + testid_gen + " () {");
		w.Write ("\t\t");
		if (retType != "void")
			w.Write (retType + " res = (" + retType + ")");
		w.Write ("typeof (Tests).GetMethod (\"" + callee_name + "\").Invoke (null, new object [] { ");
		WriteList (w, values);
		w.WriteLine ("});");
		if (retType != "void")
			w.WriteLine ("\t\tif (res !=  " + retVal + ") return 1;");
		w.WriteLine ("\t\treturn 0;");
		w.WriteLine ("\t}");

		/* The callee */
		w.Write ("\tpublic static " + retType + " meth_" + testid_gen + " (");

		string[] arg_decl = new string [types.Length];
		for (int i = 0; i < types.Length; ++i)
			arg_decl [i] = types [i] + " arg" + i;

		WriteList (w, arg_decl);
		w.WriteLine (") {");

		for (int i = 0; i < values.Length; ++i)
			w.WriteLine ("\t\tif (arg" + i + " != " + values [i] + ") throw new Exception ();");

		if (retType != "void")
			w.WriteLine ("\t\treturn " + retVal + ";");

		w.WriteLine ("\t}");
	}
}