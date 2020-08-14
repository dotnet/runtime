// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace EEGC
{
    using System;
    using System.Threading;

    public class EEGC
    {
        internal static int kStretchTreeDepth;
        internal static int kLongLivedTreeDepth;
        internal static int kShortLivedTreeDepth;
        const int NUM_ITERATIONS = 200;

        internal static void Populate(int iDepth, Node thisNode)
        {
            if (iDepth <= 0)
                return;

            else
            {
                iDepth--;
                thisNode.left = new Node();
                thisNode.right = new Node();
                Populate(iDepth, thisNode.left);
                Populate(iDepth, thisNode.right);
            }
        }

        public static void Main(string[] p_args)
        {
            Node root;
            Node longLivedTree;
            Node tempTree;
            Thread sleepThread;

            kStretchTreeDepth = 19;     // about 24MB
            kLongLivedTreeDepth = 18;   // about 12MB
            kShortLivedTreeDepth = 13;  // about 0.4MB

            tempTree = new Node();
            Populate(kStretchTreeDepth, tempTree);
            tempTree = null;

            longLivedTree = new Node();
            Populate(kLongLivedTreeDepth, longLivedTree);

            SleepThread sThread;
            sThread = new SleepThread(100);
            sleepThread = new Thread(new ThreadStart(SleepThread.ThreadStart));

            sleepThread.Start();

            for (long i = 0; i < NUM_ITERATIONS; i++)
            {
                root = new Node();
                Populate(kShortLivedTreeDepth, root);
            }

            root = longLivedTree;

            SleepThread.shouldContinue = false;
            sleepThread.Join(500);
        }
    }
}
