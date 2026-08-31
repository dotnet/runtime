// Test for creating multi-dimensional arrays.
// See https://github.com/mono/mono/issues/12193.
// See mono_array_new_n_icall.
// FIXME Coverage of the doubled parameter count, with lower_bounds.

using System;

class T
{
	// These are separate functions so that they are
	// JITed in a different context.

	static System.Array f0 () { return new int[] {1,2}; }
	static System.Array f1 () { return new int[,] {{2,3}}; }
	static System.Array f2 () { return new int[,,] {{{3}}}; }
	static System.Array f3 () { return new int[,,,] {{{{4}}}}; }
	static System.Array f4 () { return new int[,,,,] {{{{{5}}}}}; }
	static System.Array f5 () { return new int[,,,,,] {{{{{{6}}}}}}; }
	static System.Array f6 () { return new int[,,,,,] {{{{{{6,7}}}}}}; }
	static System.Array f7 () { return new int[,,,,,] {{{{{{6},{7}}}}}}; }
	static System.Array f8 () { return new int[,,,,,] {{{{{{6},{8},{9}}}}}}; }
	static System.Array f9 () { return new int[,,,,,] {{{{{{6},{8},{9}},{{1},{2},{3}}}}}}; }
	static System.Array f10() { return new int[,,,,,] {{{{{{6,7,8},{9,10,11},{1,2,3},{5,6,7}}}}}}; }
	static System.Array f11() {
		// Should only have one or two alloca or sub rsp (locals and localalloc are not combined, alas).
		var a = new int[,,,,,] {{{{{{6,7,8},{9,10,11},{1,2,3},{5,6,7}}}}}};
		var b = new int[,,,,,] {{{{{{0x60,0x70,0x80},{0x90,0x100,0x110},{0x10,0x20,0x30},{0x50,0x60,0x70}}}}}};
		var c = new int[,,,,,] {{{{{{0x600,0x700,0x800},{0x900,0x1000,0x1100},{0x100,0x200,0x300},{0x500,0x600,0x700}}}}}};
		return a == b ? null : c;
	}

	static System.Array f12() {
		// Should only have one or two alloca or sub rsp (locals and localalloc are not combined, alas).
		// Should use more stack than f11.
		var a = new int[,,,,,,,,,,,,,,,,,,] {{{{{{{{{{{{{{{{{{{ 1 }}}}}}}}}}}}}}}}}}};
		var b = new int[,,,,,,,,,,,,,,,,,,] {{{{{{{{{{{{{{{{{{{ 2 }}}}}}}}}}}}}}}}}}};
		var c = new int[,,,,,,,,,,,,,,,,,,] {{{{{{{{{{{{{{{{{{{ 3 }}}}}}}}}}}}}}}}}}};
		a = new int[,,,,,,,,,,,,,,,,,,] {{{{{{{{{{{{{{{{{{{ 4 }}}}}}}}}}}}}}}}}}};
		b = new int[,,,,,,,,,,,,,,,,,,] {{{{{{{{{{{{{{{{{{{ 5 }}}}}}}}}}}}}}}}}}};
		c = new int[,,,,,,,,,,,,,,,,,,] {{{{{{{{{{{{{{{{{{{ 6 }}}}}}}}}}}}}}}}}}};
		return a == b ? null : c;
	}

	static int Main ()
	{
		return (f0 ().Rank
		 + f1 ().Rank
		 + f2 ().Rank
		 + f3 ().Rank
		 + f4 ().Rank
		 + f5 ().Rank
		 + f6 ().Rank
		 + f7 ().Rank
		 + f8 ().Rank
		 + f9 ().Rank
		 + f10 ().Rank
		 + f11 ().Rank
		 + f12 ().Rank) == 76 ? 0 : 1;
		// Matches desktop. FIXME: Verify more.
	}
}
