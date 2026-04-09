// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading;

public class Node
{
    public Node Left, Right;
    private GCHandle gch;
    public static Random rand = new Random(191919);

    public Node()
    {

        GCHandleType type = GCHandleType.Normal;

        if (rand.Next() % 3 == 0)
        {
            type = GCHandleType.Pinned;
        }

        //int size = (int)(Math.Pow(2,(rand.Next()%18)));
        int size = 0;

        gch = GCHandle.Alloc(new byte[size], type);
    }


    ~Node()
    {

        if (gch.IsAllocated)
        {
            gch.Free();
        }
    }


    public static Node CreateTree(int depth)
    {
        if (depth<=0)
        {
            return null;
        }

        Node root = null;
        try
        {
            root = new Node();
        }
        catch (OutOfMemoryException)
        {
            Console.WriteLine("OOM");
            return null;
        }
        root.Left = CreateTree(depth-1);
        root.Right = CreateTree(depth-1);
        return root;
    }


    public static Node FindRandomNode(Node root, int depth)
    {
        int iters = rand.Next() % (depth-1);
        Node cur = root;

        for (int i=0; i<iters; i++)
        {

            if (cur.Left==null || cur.Right== null)
            {
                break;
            }

            if (rand.Next() % 2 == 0)
            {
                cur = cur.Left;
            }
            else
            {
                cur = cur.Right;
            }

        }

        return cur;

    }

}


public class ServerSpin
{

    int depth;
    int numIterations;
    int numThreads;
    int maxObjectSize;

    public ServerSpin(int depth, int numIterations, int numThreads, int maxObjectSize)
    {
        this.depth = depth;
        this.numIterations = numIterations;
        this.numThreads = numThreads;
        this.maxObjectSize = maxObjectSize;
    }

    public ServerSpin(int depth, int numIterations, int numThreads)
        : this(depth, numIterations, numThreads, 1024*1024)
    {
    }


    public static void Usage()
    {
        Console.WriteLine("USAGE: ServerSpin.exe <tree depth> <num iterations> <num threads> [max object size]");
        Console.WriteLine("\tdefaults to: 7, 100, 2");
    }


    public static void Main(string[] args)
    {

        // defaults
        int depth = 7;
        int numIterations = 100;
        int numThreads = 2;

        if (args.Length==3)
        {
            Int32.TryParse(args[0], out depth);
            if (depth<=1)
            {
                Usage();
                return;
            }

            Int32.TryParse(args[1], out numIterations);
            if (numIterations <= 0)
            {
                Usage();
                return;
            }

            Int32.TryParse(args[2], out numThreads);
            if (numThreads < 1)
            {
                Usage();
                return;
            }
        }
        else if (args.Length!=0)
        {
            Usage();
            return;
        }


        ServerSpin test = new ServerSpin(depth, numIterations, numThreads);
        test.RunTest();

    }


    public void RunTest()
    {
        Thread[] threads = new Thread[numThreads];
        for (int i=0; i<numThreads; i++)
        {
            threads[i] = new Thread(new ThreadStart(DoWork));
            threads[i].Start();
            Console.WriteLine("Start thread {0}", i);
        }

        for (int i=0; i<numThreads; i++)
        {
            threads[i].Join();
            Console.WriteLine("Done thread {0}", i);
        }

    }


    private void DoWork()
    {
        // create initial tree
        Node root = Node.CreateTree(depth);

        if (root==null)
        {
            return;
        }

        for (int i=0; i<numIterations; i++)
        {
            // replace subtree
            Node.FindRandomNode(root, depth).Left = Node.CreateTree(depth-1);

            // delete subtree
            Node.FindRandomNode(root, depth).Left = null;

            // replace subtree
            Node.FindRandomNode(root, depth).Right = Node.CreateTree(depth-2);

            // delete subtree
            Node.FindRandomNode(root, depth).Right = null;

            // replace subtree
            Node.FindRandomNode(root, depth).Left = Node.CreateTree(depth-3);
        }
    }
}
