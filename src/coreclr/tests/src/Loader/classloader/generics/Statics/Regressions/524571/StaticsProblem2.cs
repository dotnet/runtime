// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// the subtypes here that contain a Canonical type are Node<NodeSys<a>> and NodeSys<a>.
// Both Node and NodeSys contain static fields.

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


public class NodeSys<a>
{
    public static NodeSys<a> leafSys;
    
    static NodeSys() 
    {
        leafSys = new NodeSys<a>();
        Console.WriteLine("NodeSys<A>'s .cctor ran, where A was {0}.", typeof(a));
        Console.WriteLine("LeafSys: {0}", leafSys);
    }
}

public class SystemMap<a>
{
    	public NodeSys<a> rootSys;
	public Node<NodeSys<a>> root;

    	public SystemMap(a x)
    	{
    		Console.WriteLine("Accessing a static from NodeSys<a>...");
        	this.rootSys = NodeSys<a>.leafSys;
       
  	      	Console.WriteLine("\nAccessing a static from Node<NodeSys<a>>...");
       	 this.root = Node<NodeSys<a>>.leaf;
    	}

	public bool Eval()
	{
		Console.WriteLine("Read a static from NodeSys<a>.");
		Console.WriteLine("Got: {0}", (this.rootSys == null) ? "<null>" : this.rootSys.ToString());

		Console.WriteLine("Read a static from Node<NodeSys<a>>.");
        	Console.WriteLine("Got: {0}", (this.root == null) ? "<null>" : this.root.ToString());

		if (rootSys == null || root == null)
			return false;
		else
			return true;
	}
}


class Test
{
	public static int Main () 
    	{ 
    		Console.WriteLine("-------------------------------------------------------------------");
		SystemMap<Int32>  y1 = new SystemMap<Int32> (5);
		Console.WriteLine("-------------------------------------------------------------------");
       	SystemMap<Object> y2 = new SystemMap<Object> ("S");
		Console.WriteLine("-------------------------------------------------------------------");
       	SystemMap<string> y3 = new SystemMap<string> ("S");
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
