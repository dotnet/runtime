#!/bin/sh

cat "$1/array-coop-1.cs"

cat <<EOF

struct t
{
	public t (int aa)
	{
		a = b = c = d = e = f = g = h = i = j = k = l = m = n = o = p = q = r = s = u = v = w = x = y = z = aa;
	}

	public static bool operator == (t a, t b) { return a.i == b.i; }
	public static bool operator != (t a, t b) { return a.i != b.i; }
	override public bool Equals (object a) { return i == ((t)a).i; }
	override public int GetHashCode () { return (int)i; }

	long a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, u, v, w, x, y, z;
}

class test
{
	// FIXME? Can this line be the same for valuetypes and int?
	static t newt (int aa) { return new t (aa); }

EOF

cat "$1/array-coop-2.cs"
