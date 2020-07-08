// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// the subtypes here that contain a Canonical type are Node<NodeStruct<NodeSys<a>>>  and Node<NodeStruct<NodeUser<a>>>.

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

public class NodeUser<a> { }

public class SystemMap<a>
{
	public Node<NodeStruct<NodeSys<a>>> rootSys;
    	public Node<NodeStruct<NodeUser<a>>> rootUser;

    	public SystemMap(a x)
    	{
    		Console.WriteLine("Accessing a static from Node<NodeStruct<NodeSys<a>>>...");
        	this.rootSys = Node<NodeStruct<NodeSys<a>>>.leaf;
        	
        	Console.WriteLine("\nAccessing a static from Node<NodeStruct<NodeUser<a>>>...");
        	this.rootUser = Node<NodeStruct<NodeUser<a>>>.leaf;
    	}

	public bool Eval()
	{
		Console.WriteLine("Read a static from Node<NodeStruct<NodeSys<a>>>.  Got: {0}", (rootSys == null) ? "<null>" : rootSys.ToString());
		Console.WriteLine("Read a static from Node<NodeStruct<NodeUser<a>>>.  Got: {0}", (rootUser == null) ? "<null>" : rootUser.ToString());

		if (rootSys == null || rootUser == null)
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
       	SystemMap<Double> y2 = new SystemMap<Double> (3.14159);
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
