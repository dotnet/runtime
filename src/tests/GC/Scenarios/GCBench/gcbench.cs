// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is adapted from a benchmark written by John Ellis and Pete Kovac
// of Post Communications.
// It was modified by Hans Boehm of Silicon Graphics.
//
//      This is no substitute for real applications.  No actual application
//      is likely to behave in exactly this way.  However, this benchmark was
//      designed to be more representative of real applications than other
//      GC benchmarks of which we are aware.
//      It attempts to model those properties of allocation requests that
//      are important to current GC techniques.
//      It is designed to be used either to obtain a single overall performance
//      number, or to give a more detailed estimate of how collector
//      performance varies with object lifetimes.  It prints the time
//      required to allocate and collect balanced binary trees of various
//      sizes.  Smaller trees result in shorter object lifetimes.  Each cycle
//      allocates roughly the same amount of memory.
//      Two data structures are kept around during the entire process, so
//      that the measured performance is representative of applications
//      that maintain some live in-memory data.  One of these is a tree
//      containing many pointers.  The other is a large array containing
//      double precision floating point numbers.  Both should be of comparable
//      size.
//
//      The results are only really meaningful together with a specification
//      of how much memory was used.  It is possible to trade memory for
//      better time performance.  This benchmark should be run in a 32 MB
//      heap, though we don't currently know how to enforce that uniformly.
//
//      Unlike the original Ellis and Kovac benchmark, we do not attempt
//      measure pause times.  This facility should eventually be added back
//      in.  There are several reasons for omitting it for now.  The original
//      implementation depended on assumptions about the thread scheduler
//      that don't hold uniformly.  The results really measure both the
//      scheduler and GC.  Pause time measurements tend to not fit well with
//      current benchmark suites.  As far as we know, none of the current
//      commercial  implementations seriously attempt to minimize GC pause
//      times.

namespace DefaultNamespace {
    using System;

    internal class Node
    {
        internal Node left;
        internal Node right;

        internal Node(Node l, Node r)
        {
            left = l;
            right = r;
        }

        internal Node()
        {
        }
    }

    public class GCBench
    {

        public const int kStretchTreeDepth    = 18;      // about 16Mb
        public const int kLongLivedTreeDepth  = 16;  // about 4Mb
        public const int kArraySize  = 50;  // about 4Mb
        public const int kMinTreeDepth = 4;
        public const int kMaxTreeDepth = 16;

        // Nodes used by a tree of a given size
        internal static int TreeSize(int i)
        {
            return ((1 << (i + 1)) - 1);
        }

        // Number of iterations to use for a given tree depth
        internal static int NumIters(int i)
        {
            return 2 * TreeSize(kStretchTreeDepth) / TreeSize(i);
        }

        // Build tree top down, assigning to older objects.
        internal static void Populate(int iDepth, Node thisNode)
        {
            if (iDepth<=0)
            {
                return;
            }
            else
            {
                iDepth--;
                thisNode.left  = new Node();
                thisNode.right = new Node();
                Populate (iDepth, thisNode.left);
                Populate (iDepth, thisNode.right);
            }
        }

        // Build tree bottom-up
        internal static Node MakeTree(int iDepth)
        {
            if (iDepth<=0)
            {
                return new Node();
            }
            else
            {
                return new Node(MakeTree(iDepth-1), MakeTree(iDepth-1));
            }
        }

        internal void TimeConstruction(int depth)
        {

            int     iNumIters = NumIters(depth);
            Node    tempTree;

            for (int i = 0; i < iNumIters; ++i)
            {
                tempTree = new Node();
                Populate(depth, tempTree);
                tempTree = null;
            }


            for (int i = 0; i < iNumIters; ++i)
            {
                tempTree = MakeTree(depth);
                tempTree = null;
            }

        }

        public static int Main(String [] args)
        {
            Node    longLivedTree;
            Node    tempTree;

            Console.WriteLine("Test should return with ExitCode 100 ...");

            GCBench Mv_Obj = new GCBench();

            // Stretch the memory space quickly
            tempTree = MakeTree(kStretchTreeDepth);
            tempTree = null;

            // Create a long lived object
            longLivedTree = new Node();
            Populate(kLongLivedTreeDepth, longLivedTree);

            // Create long-lived array, filling half of it
            double []array = new double[kArraySize];
            for (int i = 0; i < kArraySize/2; ++i)
            {
                array[i] = 1.0/i;
            }

            GC.Collect();

            for (int d = kMinTreeDepth; d <= kMaxTreeDepth; d += 2)
            {
                Mv_Obj.TimeConstruction(d);
            }

            Console.WriteLine("Test Passed");
            return 100;
        }
    }

}
