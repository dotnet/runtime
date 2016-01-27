// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*************************** Red-Black Tree ************************************************

 Rule 0: Binary Tree useful for data storage
 Rule 1: Every node has value (key) greater than left-child & less than the right-child value
 Rule 2: Every node is "red" or "black" (color)
 Rule 3: Root is black
 Rule 4: Leaf is red
 Rule 5. Red node which is not a leaf has black children(consecutive red nodes not allowed)
 Rule 6: Every path from root to leaf contains same no. of black nodes(black-height of the tree)
 Rule 7: Every node has a rank(0 to max) where root has max. rank..called height of the tree
 Rule 8: If a black node has rank "r", it's parent will have rank "r+1"
 Rule 9: If a red node had rank "r", it's parent will have rank "r"
 Rule 10: Readjustment of the tree is by rotation

**********************************************************************************************/


using System;

public enum Color
{
    Red,
    Black
}

public enum Child
{
    Left,
    Right,
    None
}

public class Node
{
    public int key;
    public Color color;
    public int rank;
    public Node left;
    public Node right;
    public Node parent;
    public int[] array;
}

public class Tree
{
    // initialize random number generator

    public static int Seed = (int)DateTime.Now.Ticks;
    public static Random Rand = new Random(Seed);

    public static int MaxDepth = 15;
    private int _curDepth = 0;
    private int _curDepth2 = 0;

    private int _nodes;
    private Node _root;

    public Tree(int n)
    {
        _nodes = n;
    }

    public void BuildTree()
    {
        for (int i = 0; i < _nodes; i++)
        {
            bool result = InsertNode();
            if (result == false) return;

            //PrintTree(root);
            TestLibrary.Logging.WriteLine("RedBlack tree now has {0} nodes", i);
            GC.Collect();
        }
        TestLibrary.Logging.WriteLine("Final Red Black Tree Constructed");
        //PrintTree(root);
    }

    public bool InsertNode()
    {
        Node n = BuildNode();

        if (n == null) return false;

        if (_root == null)
        {
            _root = n;
            _root.color = Color.Black;
            return true;
        }

        //Rule 1: Every node has value (key) greater than left-child & less than the right-child value
        // so traverse tree to find it's place

        Node temp = _root;
        while ((temp.left != null) || (temp.right != null))
        {
            if (n.key < temp.key)
            {
                if (temp.left != null)
                {
                    temp = temp.left;
                    continue;
                }
                else
                {
                    temp.left = n;
                    n.parent = temp;
                    return true;
                }
            }
            else if (n.key > temp.key)
            {
                if (temp.right != null)
                {
                    temp = temp.right;
                    continue;
                }
                else
                {
                    temp.right = n;
                    n.parent = temp;
                    return true;
                }
            }
        }
        if (n.key < temp.key)
            temp.left = n;
        else if (n.key > temp.key)
            temp.right = n;
        n.parent = temp;

        // Adjust tree after insertion
        AdjustTree(n);
        return true;
    }

    public Node BuildNode()
    {
        Node temp = new Node();
        int k = Rand.Next(0, _nodes);
        bool result = UniqueKey(_root, k);

        while (result == false)
        {
            k = Rand.Next(0, _nodes);
            result = UniqueKey(_root, k);
        }

        temp.key = k;
        temp.color = Color.Red;   //Rule 4: Leaf is red
        temp.array = new int[k]; // Just allocating array of size of the key

        return temp;
    }

    public bool UniqueKey(Node r, int k)
    {
        if (_root == null) return true;

        if (k == r.key) return false;

        else if (k < r.key)
        {
            if (r.left != null) return (UniqueKey(r.left, k));
            else { return true; }
        }
        else
        {
            if (r.right != null) return (UniqueKey(r.right, k));
            else { return true; }
        }
    }

    public void AdjustTree(Node x)
    {
        //Rule 10: Readjustment of the tree is by rotation
        RotateTree(x);

        //Rule 3: Root is black
        _root.color = Color.Black;

        //Rule 4: Leaf is red...automatically as we have all nodes as red to begin with

        //Rule 5. Red node which is not a leaf has black children(consecutive red nodes not allowed)
        //SetNodeColor(root);
    }

    public void RotateTree(Node x)
    {
        Node uncle = null;
        Node sibling = null;

        if (x.parent.color == Color.Red)
        {
            if (WhichChild(x.parent) == Child.Left)    // uncle is the right child
                uncle = (x.parent).parent.right;
            else if (WhichChild(x.parent) == Child.Right)  // uncle is the left child
                uncle = (x.parent).parent.left;

            if (WhichChild(x) == Child.Left) // x is left child
                sibling = x.parent.right;
            else  //x is right child
                sibling = x.parent.left;
        }
        else return;

        //Rotation Type 1 : x is red, p[x] is red and uncle=null, sibling=null
        if ((uncle == null) && (sibling == null))
        {
            //Different orientation...Works!

            if (WhichChild(x) != WhichChild(x.parent))
            {
                int temp = x.key;
                x.key = x.parent.key;
                x.parent.key = temp;

                // reverse orientations
                if (WhichChild(x) == Child.Left)
                {
                    x.parent.right = x;
                    x.parent.left = null;
                }
                else
                {
                    x.parent.left = x;
                    x.parent.right = null;
                }
                RotateTree(x);
            }

            // Same orientation..Works!

            else
            {
                if (x.parent.parent != _root)
                {
                    if (WhichChild(x.parent.parent) == Child.Left)
                        x.parent.parent.parent.left = x.parent;
                    else x.parent.parent.parent.right = x.parent;
                }

                else _root = x.parent;

                if (WhichChild(x) == Child.Left)
                {
                    (x.parent).right = (x.parent).parent;

                    ((x.parent).right).parent = x.parent;

                    x.parent.parent.left = null;
                    x.parent.right.color = Color.Red;
                }
                else
                {
                    x.parent.left = x.parent.parent;
                    x.parent.left.parent = x.parent;
                    if (WhichChild(x.parent) == Child.Left) x.parent.left.left = null;
                    else x.parent.left.right = null;
                    x.parent.left.color = Color.Red;
                }

                x.parent.color = Color.Black;
            }
        }  // end of Rotation Type 1

        //Rotation Type 2: depending on uncle's color
        else
        {
            switch (uncle.color)
            {
                case Color.Red:
                    if (WhichChild(uncle) == Child.Left) x.parent.parent.left.color = Color.Black;   //u[x] = black
                    else x.parent.parent.right.color = Color.Black;
                    x.parent.color = Color.Black;     //p[x] = black
                    x.parent.parent.color = Color.Red;    //p(p[x]) = red
                    break;
                case Color.Black:
                    if (WhichChild(x) == Child.Right)
                    {
                        x.parent.color = Color.Black;
                        x.parent.left.color = Color.Red;
                    }
                    break;
            }
        }
    }

    public Child WhichChild(Node n)
    {
        if (n == _root) return Child.None;
        if (n == n.parent.left) return Child.Left;
        else return Child.Right;
    }

    public void SetNodeColor(Node n)
    {
        //Rule 5. Red node which is not a leaf has black children(consecutive red nodes not allowed)

        if (n.color == Color.Red)
        {   //set child color as black
            if (n.left != null) n.left.color = Color.Black;
            if (n.right != null) n.right.color = Color.Black;
        }

        if (n.left != null) SetNodeColor(n.left);
        if (n.right != null) SetNodeColor(n.right);
    }

    public void DeleteTree()
    {
        Node node = null;
        int n;
        //Choose random number of nodes to delete
        int num = Rand.Next(1, _nodes);
        TestLibrary.Logging.WriteLine("Deleting {0} nodes...", num);

        for (int i = 0; i < num; i++)
        {
            n = Rand.Next(0, _nodes);
            TestLibrary.Logging.WriteLine("Deleting node no. {0}", i);
            _curDepth = 0;
            node = FindNode(_root, n);
            _curDepth = 0;
            if (node != null) DeleteNode(node);
        }
        TestLibrary.Logging.WriteLine("Final Tree after deletion");
        //PrintTree(root);
    }

    public Node FindNode(Node r, int k)
    {
        if (_curDepth == MaxDepth)
        {
            _curDepth = 0;
            return null;
        }

        _curDepth++;


        if (k == r.key) return r;

        else if (k < r.key)
        {
            if (r.left != null) return (FindNode(r.left, k));
            else return null; // skip this node
        }
        else
        {
            if (r.right != null) return (FindNode(r.right, k));
            else return null;
        }
    }

    public void DeleteNode(Node n)
    {
        TestLibrary.Logging.WriteLine("In DeleteNode()");

        if (_curDepth == MaxDepth)
        {
            _curDepth = 0;
            return;
        }

        _curDepth++;


        Node onlychild;
        // Deleting the node as in a BST

        // Case 1: n is a leaf..just delete n
        if ((n.left == null && n.right == null))
        {
            if (WhichChild(n) == Child.Left)
            {
                if (n.parent != null)
                    n.parent.left = null;
            }
            else
            {
                if (n.parent != null)
                    n.parent.right = null;
            }

            //reassign root
            if (n == _root) _root = null;
            n = null;
        }
        // Case 2: n has 1 child
        else if (n.left == null || n.right == null)
        {
            if (n.left != null) onlychild = n.left;
            else onlychild = n.right;

            //replace n with child node

            if (WhichChild(n) == Child.Left)
            {
                n.parent.left = onlychild;
                onlychild.parent = n.parent;
            }
            else if (WhichChild(n) == Child.Right)
            {
                n.parent.right = onlychild;
                onlychild.parent = n.parent;
            }
            else
            {  // n is root
                if (n.left != null)
                {
                    _root = n.left;
                    n.left.parent = null;
                }
                else
                {
                    _root = n.right;
                    n.right.parent = null;
                }
            }
            n = null;
        }
        // Case 3: n has 2 children
        else
        {
            _curDepth2 = 0;
            Node largestleft = FindLargestInLeftSubTree(n.left, n.left.key);

            int temp = largestleft.key;
            largestleft.key = n.key;
            n.key = temp;
            DeleteNode(largestleft);
        }
    }

    public Node FindLargestInLeftSubTree(Node x, int max)
    {
        if (_curDepth2 == MaxDepth)
        {
            _curDepth2 = 0;
            return x;
        }

        _curDepth2++;

        if (x.key > max) max = x.key;

        if (x.right == null && x.key == max) return x;

        if (x.key <= max)
        {
            if (x.right != null) return FindLargestInLeftSubTree(x.right, max);
            else return x;
        }
        else return x;
    }

    public void PrintTree(Node r)
    {
        if (r == null) return;

        TestLibrary.Logging.WriteLine("{0}, {1}", r.key, r.color);
        if (r.left != null) PrintTree(r.left);
        if (r.right != null) PrintTree(r.right);
    }
}


public class Test
{
    public static int DEFAULT = 10000;

    public static void Usage()
    {
        TestLibrary.Logging.WriteLine("USAGE: redblacktree [num nodes]");
        TestLibrary.Logging.WriteLine("       where num nodes > 0");
        TestLibrary.Logging.WriteLine("       Default is 10,000");
    }

    public static int Main(string[] args)
    {
        int numNodes = DEFAULT;

        if (args.Length > 0)
        {
            if (args[0].Equals("/?"))
            {
                Usage();
                return 0;
            }

            try
            {
                numNodes = int.Parse(args[0]);
            }
            catch (FormatException)
            {
                Usage();
                return 0;
            }

            if (numNodes <= 0)
            {
                Usage();
                return 0;
            }
        }

        //TestLibrary.Logging.WriteLine("Forcing JIT of overflow path....");
        try
        {
            // must force this exception
            throw new OverflowException();
        }
        catch (OverflowException) { }

        TestLibrary.Logging.WriteLine("Constructing Red-Black Tree with {0} nodes", numNodes);
        TestLibrary.Logging.WriteLine("Using {0} as random seed", Tree.Seed);
        Tree rbtree = new Tree(numNodes);
        rbtree.BuildTree();

        TestLibrary.Logging.WriteLine("Deleting random nodes and re-adjusting tree");
        rbtree.DeleteTree();

        TestLibrary.Logging.WriteLine("Test Passed");
        return 100;
    }
}

