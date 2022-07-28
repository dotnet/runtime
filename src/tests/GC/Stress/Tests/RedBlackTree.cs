// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*************************** Left Leaning Red-Black Tree *************************************

 * An Implementation of Left Leaning Red Black trees based on 
        Robert Sedgewick's paper:
          http://www.cs.princeton.edu/~rs/talks/LLRB/LLRB.pdf
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

[System.Diagnostics.DebuggerDisplay("key={key};l={left.key};r={right.key};p={parent.key}")]
public class Node
{
    public int key;
    public Color color;
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
            InsertNode();
#if DEBUG
            TestLibrary.Logging.WriteLine("RedBlack tree now has {0} nodes", i);
#endif 
            GC.Collect();
        }
#if DEBUG
        TestLibrary.Logging.WriteLine("Final Red Black Tree Constructed");
#endif
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public void CheckTree(bool expectBalanced)
    {
#if CHECKINVARIANTS
        CheckTreeRecursive(expectBalanced, _root, null, -1, _nodes + 1, false);
#endif
    }

    private int CheckTreeRecursive(bool expectBalanced, Node curr, Node parent, int min, int max, bool notRed)
    {
        if (curr == null) return 0;

        if (expectBalanced && notRed && curr.color == Color.Red)
            TestLibrary.TestFramework.LogError("", "Rule 5: Red node, with red child, or red right child");
        if (curr.parent != parent)
            TestLibrary.TestFramework.LogError("", "Parent pointer has become corrupt.");

        if (curr.left == null && curr.right != null)
            TestLibrary.TestFramework.LogError("", "Not left leaning.");

        if (curr.key > max && curr.key < min)
            TestLibrary.TestFramework.LogError("", "Rule 1: Tree is not sorted");

        var leftRank = CheckTreeRecursive(expectBalanced, curr.left, curr, min, curr.key, curr.color == Color.Red);
        var rightRank = CheckTreeRecursive(expectBalanced, curr.right, curr, curr.key, max, true);

        if (expectBalanced && leftRank != rightRank)
            TestLibrary.TestFramework.LogError("", "Rule 6: Tree is not balanced");

        var currRank = curr.color == Color.Black ? leftRank + 1 : leftRank;

        return currRank;
    }
    private void RotateLeft(ref Node x)
    {
        //Swing pointers around for rotation
        var r = x.right;
        x.right = r.left;
        r.left = x;

        //patch parent pointers
        r.parent = x.parent;
        if (x.right != null) x.right.parent = x;
        x.parent = r;

        //Fix colors
        var c = x.color;
        x.color = r.color;
        r.color = c;

        //Update incoming reference
        x = r;
    }
    private void RotateRight(ref Node x)
    {
        //Swing pointers around for rotation
        Node l = x.left;
        x.left = l.right;
        l.right = x;

        //patch parent pointers
        l.parent = x.parent;
        x.parent = l;
        if (x.left != null) x.left.parent = x;

        //Fix colors
        var c = x.color;
        x.color = l.color;
        l.color = c;

        //Update incoming reference
        x = l;
    }

    private bool IsRed(Node x)
    {
        return x != null && x.color == Color.Red;
    }

    private void Flip(ref Color c)
    {
        if (c == Color.Red)
            c = Color.Black;
        else
            c = Color.Red;
    }

    private void ColorFlip(Node curr)
    {
        Flip(ref curr.left.color);
        Flip(ref curr.right.color);
        Flip(ref curr.color);
    }

    private void Fixup(ref Node curr)
    {
        if (IsRed(curr.right))
            RotateLeft(ref curr);

        if (IsRed(curr.left) && IsRed(curr.left.left))
            RotateRight(ref curr);

        if (IsRed(curr.left) && IsRed(curr.right))
        {
            ColorFlip(curr);
        }
    }

    public void InsertNode(ref Node curr, Node newNode, Node parent)
    {
        if (curr == null)
        {
            curr = newNode;
            newNode.parent = parent;
            return;
        }

        if (newNode.key < curr.key)
        {
            InsertNode(ref curr.left, newNode, curr);
        }
        else if (newNode.key > curr.key)
        {
            InsertNode(ref curr.right, newNode, curr);
        }

        Fixup(ref curr);
    }

    public void InsertNode()
    {
        Node n = BuildNode();

        InsertNode(ref _root, n, null);

        // Adjust tree after insertion
        if (_root.color != Color.Black)
            _root.color = Color.Black;

        CheckTree(true);
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

        if (k < r.key)
        {
            if (r.left != null) return (UniqueKey(r.left, k));
            else return true;
        }
        else
        {
            if (r.right != null) return (UniqueKey(r.right, k));
            else return true;
        }
    }

    public void DeleteTree()
    {
        int n;
        //Choose random number of nodes to delete
        int num = Rand.Next(1, _nodes);
#if DEBUG
        TestLibrary.Logging.WriteLine("Deleting {0} nodes...", num);
#endif

        for (int i = 0; i < num; i++)
        {
            n = Rand.Next(0, _nodes);
#if DEBUG
            TestLibrary.Logging.WriteLine("Deleting node no. {0} with key {1}", i, n);
#endif
            DeleteNode(ref _root, n);
            if (_root.color != Color.Black)
                _root.color = Color.Black;
            CheckTree(true);
        }
#if DEBUG
        TestLibrary.Logging.WriteLine("Final Tree after deletion");
        PrintTree(_root);
#endif
    }

    void MoveRedRight(ref Node curr)
    {
        ColorFlip(curr);
        if (IsRed(curr.left.left))
        {
            RotateRight(ref curr);
            ColorFlip(curr);
        }
    }

    void MoveRedLeft(ref Node curr)
    {
        ColorFlip(curr);
        if (IsRed(curr.right.left))
        {
            RotateRight(ref curr.right);
            RotateLeft(ref curr);
            ColorFlip(curr);
        }
    }

    public bool DeleteNode(ref Node n, int key)
    {

        if (n.key != key && n.left == null && n.right == null)
        {
            Fixup(ref n);
            return false;
        }

        var result = false;
        if (n.key > key)
        {
            if (!IsRed(n.left) && !IsRed(n.left.left))
                MoveRedLeft(ref n);

            result = DeleteNode(ref n.left, key);
        }
        else
        {
            if (IsRed(n.left)) RotateRight(ref n);

            if (n.right == null)
            {
                if (n.key == key)
                {
                    n = null;
                    return true;
                }
                else
                    return false;
            }

            if (!IsRed(n.right) && !IsRed(n.right.left))
                MoveRedRight(ref n);

            if (n.key == key)
            {
                var minRight = DeleteMin(ref n.right);
                n.key = minRight.key;
                n.array = minRight.array;
                result = true;
            }
            else
                result = DeleteNode(ref n.right, key);
        }

        Fixup(ref n);

        return result;
    }

    Node DeleteMin(ref Node curr)
    {
        Node min;
        if (curr.left == null)
        {
            min = curr;// Left leaning, so no left child means no child.
            curr = null;
            return min;
        }

        if (!IsRed(curr.left) && !IsRed(curr.left.left))
            MoveRedLeft(ref curr);

        min = DeleteMin(ref curr.left);

        Fixup(ref curr);

        return min;
    }


    public void PrintTree(Node r)
    {
        PrintTree(r, "");
    }

    public void PrintTree(Node r, String offset)
    {
        if (r == null) return;

        offset = offset + " ";

        if (r.left != null) PrintTree(r.left, offset);
        TestLibrary.Logging.WriteLine("{0}{1}, {2}", offset, r.key, r.color);
        if (r.right != null) PrintTree(r.right, offset);
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

