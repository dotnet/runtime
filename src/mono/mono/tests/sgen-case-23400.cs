using System;
using System.Collections.Generic;

class StructArray16Test
{
    struct RefAt64
    {
	public long p1, p2, p3, p4, p5, p6, p7, p8;
	public object reference;
    }

    struct RefAt128
    {
	public long p1, p2, p3, p4, p5, p6, p7, p8;
	public long q1, q2, q3, q4, q5, q6, q7, q8;
	public object reference;
    }

    public static void Main ()
    {
	Random r = new Random (123);
	for (int j = 0; j < 5000; ++j)
	{
	    var list64 = new List<RefAt64>();
	    var list128 = new List<RefAt128>();
	    for (int i = 0; i < 200; i++)
	    {
		// This allocation is just to force collections, to crash more quickly
		object[] dummy = new object [r.Next(3, 100)];
		list64.Add (new RefAt64() { reference = new object () });
		list128.Add (new RefAt128() { reference = new object () });
	    }
	}
    }
}
