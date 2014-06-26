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

		int depth = 10000;
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
