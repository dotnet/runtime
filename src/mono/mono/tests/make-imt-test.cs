using System;

public class Tests
{
	public static void Main (String[] args) {
		int low = 2000;
		int high = 2000;

		Console.WriteLine ("using System;");
		Console.WriteLine ();

		for (int count = low; count <= high; ++count) {
			Console.WriteLine ("public interface Iface_" + count + " {");
			for (int i = 0; i <= count; ++i)
				Console.WriteLine ("    int Method_" + i + " (int a, int b, int c, int d);");
			Console.WriteLine ("}");

			Console.WriteLine ("public class Impl_" + count + " : Iface_" +  count + " {");
			for (int i = 0; i <= count; ++i)
				Console.WriteLine ("    public virtual int Method_" + i + " (int a, int b, int c, int d) { return a - b - c -d + " + i + "; }");
			Console.WriteLine ("}");
		}

		Console.WriteLine ("public class Driver");
		Console.WriteLine ("{");

		for (int iface = low; iface <= high; ++iface) {
			Console.WriteLine ("	static Iface_" + iface + " var_" + iface + " = new Impl_" + iface + " ();");
			Console.WriteLine ("	static int Test_" + iface + " () {");
			Console.WriteLine ("        int res = 0;");
			Console.WriteLine ("        int r;");

			for (int i = 0; i < iface; ++i) {
				Console.WriteLine (String.Format ("		if ((r = var_{0}.Method_{1} (10,5,3,2)) != {1}) {{", iface, i));
				Console.WriteLine (String.Format ("     Console.WriteLine(\"iface {0} method {1} returned {{0}}\", r);", iface, i));
				Console.WriteLine ("    res = 1;");
				Console.WriteLine ("}");
			}
			Console.WriteLine ("return res;");
			Console.WriteLine ("}");
		}

		Console.WriteLine ("    public static int Main () {");
		Console.WriteLine ("        int res = 0;");

		for (int iface = low; iface <= high; ++iface)
			Console.WriteLine (String.Format ("        res |= Test_{0} ();", iface));
		Console.WriteLine ("		return res;");
		Console.WriteLine ("    }");
		Console.WriteLine ("}");
	}
}
	