#!/bin/sh

cat "$1/array-coop-1.cs"

cat <<EOF
struct t
{
	public t (int aa)
	{
		i = aa;
	}
	public static bool operator == (t a, t b) { return a.i == b.i; }
	public static bool operator != (t a, t b) { return a.i != b.i; }
	override public bool Equals (object a) { return i == ((t)a).i; }
	override public int GetHashCode () { return (int)i; }

	int i;
}

class test
{
	// FIXME? Can this line be the same for valuetypes and int?
	static t newt (int aa) { return new t (aa); }

EOF

cat "$1/array-coop-2.cs"
