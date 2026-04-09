using System;
using System.Collections.Generic;
using System.Diagnostics;

class Program {
	
	class Node {
		Node child;
		Node sibling;

		public Node (int depth)	: this (depth, null) {}

		public Node (int depth, Node sibling) {
			if (depth > 0)
				this.child = new Node (depth - 1);

			this.sibling = sibling;
		}

		public override String ToString () {
			return String.Format ("Node[child={0},sibling={1}]", this.child, this.sibling);
		}
	}
	
	/**
	 * Usage : width [depth [collections]]
	 *  - width : trigger the overflow
	 *  - depth : modify the cost difference of the overflow
	 *  - collections : # of collections to perform
	 */
	public static void Main (String[] args) {
		int width = 125;
		if (args.Length > 0)
			width = Math.Max (width, Int32.Parse (args [0]));

		// Windows x64 only has 1 MB of stack per thread which is less than other x86 64-bit OSes.
		// Using 10000 for depth will cause a stack overflow on Windows x64. 5000 will fit.
		int platform = (int) Environment.OSVersion.Platform;
		bool isWin64 = !(platform == 4 || platform == 128) && Environment.Is64BitProcess;
		int depth = isWin64 ? 5000 : 10000;
		if (args.Length > 1)
			depth = Math.Max (depth, Int32.Parse (args [1]));

		int collections = 100;
		if (args.Length > 2)
			collections = Math.Max (collections, Int32.Parse (args [2]));

		Node sibling = null;

		for (int i = 0; i < width; i++) {
			sibling = new Node(depth, sibling);
			if (i > 0 && i % 10 == 0)
				Console.Write ("+");
		}

		for (int i = 0; i < collections; i++) {
			GC.Collect();
			if (i > 0 && i % 10 == 0)
				Console.Write (".");
		}
		Console.WriteLine ();
	}
}
