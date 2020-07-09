// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// the subtype here that contains a Canonical type is Node<NodeStruct<NodeSys<a[]>>>

using System;

public class Node<a> 
{
	public static Node<a> leaf;

    	static Node() 
	{
        	leaf = new Node<a>();
        	Console.WriteLine("Node<A>'s .cctor ran, where A was {0}.", typeof(a));
       	Console.WriteLine("Leaf: {0}", leaf);
    	}
}

public struct NodeStruct<a> { }

public class NodeSys<a> { }

public class SystemMap<a>
{
	public Node <NodeStruct<NodeSys<a[]>>> root;

   	public SystemMap(a x)
    	{
    		Console.WriteLine("Accessing a static from Node<NodeStruct<NodeSys<a[]>>>...");
        	this.root = Node<NodeStruct<NodeSys<a[]>>>.leaf;
    	}
	public bool Eval()
	{
		Console.WriteLine("Read a static from Node<NodeStruct<NodeSys<a[]>>>.  Got: {0}",
                	(root == null) ? "<null>" : root.ToString());
				
		if (root == null)
			return false;
		else
			return true;

	}
}


class Test
{
    	static int Main () 
	{ 
 		Console.WriteLine("-------------------------------------------------------------------");
		SystemMap<Int32>  y1 = new SystemMap<Int32> (5);
		Console.WriteLine("-------------------------------------------------------------------");
       	SystemMap<String> y2 = new SystemMap<String> ("S");
		Console.WriteLine("-------------------------------------------------------------------");
       	SystemMap<Object> y3 = new SystemMap<Object> ("S");
		Console.WriteLine("-------------------------------------------------------------------");

		if (y1.Eval() && y2.Eval() && y3.Eval() )
		{
			Console.WriteLine("PASS");
			return 100;
		}
		else
		{
			Console.WriteLine("FAIL");
			return 101;
		}
    	}
}
