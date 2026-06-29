#!/bin/sh

cat "$1/array-coop-1.cs"

cat <<EOF
using t = System.Int32;

class test
{
	// FIXME? Can this line be the same for valuetypes and int?
	static t newt (int aa) { return aa; }

EOF

cat "$1/array-coop-2.cs"
