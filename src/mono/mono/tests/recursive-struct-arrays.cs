using System;

/* Test that the runtime can represent value types that have array fields that
 * recursively refer to the same value type */

struct S1 {
	static S1[][] foo;
}

struct S2 {
	static S2[] foo;
}

struct S3a {
	static S3b[] foo;
}

struct S3b {
	static S3a[][] foo;
}

struct P<X> where X : struct {
	static P<X>[][] foo;
}

public struct S4
{
	private static S4[][] foo;

	public static readonly S4 West = new S4(-1, 0);
	public static readonly S4 East = new S4(1, 0);
	public static readonly S4 North = new S4(0, 1);
	public static readonly S4 South = new S4(0, -1);
	public static readonly S4[] Directions = { North, South, East, West };

	public readonly int x;
	public readonly int z;

	public S4(int x, int z)
	{
		this.x = x;
		this.z = z;
	}

	public override string ToString()
	{
		return string.Format("[{0}, {1}]", x, z);
	}
}


class Program {
	static int Main() {
		Console.WriteLine (typeof (S1).Name);
		Console.WriteLine (typeof (S2).Name);
		Console.WriteLine (typeof (S3a).Name);
		Console.WriteLine (typeof (S3b).Name);
		foreach (var s4 in S4.Directions) {
			Console.WriteLine (s4);
		}
		Console.WriteLine (typeof (P<S1>).Name);
		Console.WriteLine (typeof (P<int>).Name);
		return 0;
	}
}
