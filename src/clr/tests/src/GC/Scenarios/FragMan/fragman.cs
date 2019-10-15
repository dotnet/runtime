// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/* FragMan
 *
 * This test creates an array of FragNodes, then reorganizes them into a tree.
 * Then it removes the references from the array, and verifies the tree keeps
 * all the elements alive (verified by checking the Finalized count against 0).
*/

namespace DefaultNamespace {
    using System;
    using System.Runtime.CompilerServices;

    public class FragMan
    {

        internal int nodeCount = 0;
        internal FragNode fnM = null;
        internal FragNode [] CvA_FNodes;

        public static int Main ( String [] Args)
        {
            Console.WriteLine("Test should return with ExitCode 100 ...");
            FragMan test = new FragMan( );

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            if (FragNode.Finalized == 0)
            {
                Console.WriteLine("Test Passed");
                return 100;
            }

            Console.Write(FragNode.Finalized);
            Console.WriteLine(" FragNodes were prematurely finalized");
            Console.WriteLine("Test Failed");

            GC.KeepAlive(test);
            return 1;

        }


        public FragMan( )
        {
            buildTree();
            fnM = CvA_FNodes[12];
            CvA_FNodes = null;
            enumNode( fnM );
        }


        public void enumNode( FragNode Node )
        {

            nodeCount++;

            Console.WriteLine(Node.Name);
            if ( Node.Lesser != null )
            {
                Console.Write("Lesser is:");
                Console.WriteLine(Node.Lesser.Name);
            }
            if ( Node.Larger != null )
            {
                Console.Write("Larger is:");
                Console.WriteLine(Node.Larger.Name);
            }

            Console.WriteLine();

            if ( Node.Lesser != null )
            {
                enumNode( Node.Lesser );
            }
            if ( Node.Larger != null )
            {
                enumNode( Node.Larger );
            }

        }


        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public void buildTree( )
        {
            CvA_FNodes = new FragNode[26];
            for (int i=0; i<CvA_FNodes.Length; i++)
            {
                CvA_FNodes[i] = new FragNode((char)((int)'A'+i));

            }

            //  0
            CvA_FNodes[ 0 ].Parent =( CvA_FNodes[  1 ] );          // B

            //  1
            CvA_FNodes[ 1 ].Parent =( CvA_FNodes[  3 ] );          // D
            CvA_FNodes[ 1 ].Lesser =( CvA_FNodes[  0 ] );          // A
            CvA_FNodes[ 1 ].Larger =( CvA_FNodes[  2 ] );          // C


            // 2
            CvA_FNodes[ 2 ].Parent =( CvA_FNodes[  1 ] );          // B


            // 3
            CvA_FNodes[ 3 ].Parent =( CvA_FNodes[  6 ] );          // G
            CvA_FNodes[ 3 ].Lesser =( CvA_FNodes[  1 ] );          // B
            CvA_FNodes[ 3 ].Larger =( CvA_FNodes[  5 ] );          // F


            // 4
            CvA_FNodes[ 4 ].Parent =( CvA_FNodes[  5 ] );          // F


            // 5
            CvA_FNodes[ 5 ].Parent =( CvA_FNodes[  3 ] );          // D
            CvA_FNodes[ 5 ].Lesser =( CvA_FNodes[  4 ] );          // E


            // 6
            CvA_FNodes[ 6 ].Parent =( CvA_FNodes[  12 ] );         // M
            CvA_FNodes[ 6 ].Lesser =( CvA_FNodes[  3 ] );          // D
            CvA_FNodes[ 6 ].Larger =( CvA_FNodes[  9 ] );          // J


            // 7
            CvA_FNodes[ 7 ].Parent =( CvA_FNodes[  9 ] );          // J
            CvA_FNodes[ 7 ].Larger =( CvA_FNodes[  8 ] );          // I


            // 8
            CvA_FNodes[ 8 ].Parent =( CvA_FNodes[  7 ] );          // H


            // 9
            CvA_FNodes[ 9 ].Parent =( CvA_FNodes[  6 ] );          // G
            CvA_FNodes[ 9 ].Lesser =( CvA_FNodes[  7 ] );          // H
            CvA_FNodes[ 9 ].Larger =( CvA_FNodes[  11 ] );         // L


            // 10
            CvA_FNodes[ 10 ].Parent =( CvA_FNodes[  11 ] );        // L


            // 11
            CvA_FNodes[ 11 ].Parent =( CvA_FNodes[  9 ] );         // J
            CvA_FNodes[ 11 ].Lesser =( CvA_FNodes[  10 ] );        // K


            // 12
            CvA_FNodes[ 12 ].Root= true;
            CvA_FNodes[ 12 ].Lesser =( CvA_FNodes[  6 ] );         // G
            CvA_FNodes[ 12 ].Larger =( CvA_FNodes[  19 ] );        // T


            // 13
            CvA_FNodes[ 13 ].Parent =( CvA_FNodes[  14 ] );        // O


            // 14
            CvA_FNodes[ 14 ].Parent =( CvA_FNodes[  16 ] );        // Q
            CvA_FNodes[ 14 ].Lesser =( CvA_FNodes[  13 ] );        // N
            CvA_FNodes[ 14 ].Larger =( CvA_FNodes[  15 ] );        // P


            // 15
            CvA_FNodes[ 15 ].Parent =( CvA_FNodes[ 14 ] );         // O


            // 16
            CvA_FNodes[ 16 ].Parent =( CvA_FNodes[ 19 ] );         // T
            CvA_FNodes[ 16 ].Lesser =( CvA_FNodes[ 14 ] );         // O
            CvA_FNodes[ 16 ].Larger =( CvA_FNodes[ 18 ] );         // S


            // 17
            CvA_FNodes[ 17 ].Parent =( CvA_FNodes[ 18 ] );         // S


            // 18
            CvA_FNodes[ 18 ].Parent =( CvA_FNodes[ 16 ] );         // Q
            CvA_FNodes[ 18 ].Lesser =( CvA_FNodes[ 17 ] );         // R


            // 19
            CvA_FNodes[ 19 ].Parent =( CvA_FNodes[ 12 ] );         // M
            CvA_FNodes[ 19 ].Lesser =( CvA_FNodes[ 16 ] );         // Q
            CvA_FNodes[ 19 ].Larger =( CvA_FNodes[ 22 ] );         // W


            // 20
            CvA_FNodes[ 20 ].Parent =( CvA_FNodes[ 22 ] );         // W
            CvA_FNodes[ 20 ].Larger =( CvA_FNodes[  21 ] );        // V


            // 21
            CvA_FNodes[ 21 ].Parent =( CvA_FNodes[  20 ] );        // U


            // 22
            CvA_FNodes[ 22 ].Parent =( CvA_FNodes[  19 ] );        // T
            CvA_FNodes[ 22 ].Lesser =( CvA_FNodes[  20 ] );        // U
            CvA_FNodes[ 22 ].Larger =( CvA_FNodes[  24 ] );        // Y


            // 23
            CvA_FNodes[ 23 ].Parent =( CvA_FNodes[ 24 ] );         // Y


            // 24
            CvA_FNodes[ 24 ].Parent =( CvA_FNodes[  22 ] );        // W
            CvA_FNodes[ 24 ].Lesser =( CvA_FNodes[  23 ] );        // X
            CvA_FNodes[ 24 ].Larger =( CvA_FNodes[  25 ] );        // Z


            // 25
            CvA_FNodes[ 25 ].Parent =( CvA_FNodes[ 24 ] );         // Y

        }

    }

    public class FragNode
    {
        internal bool Cv_bRoot = false;
        internal FragNode Cv_FNodeUpper = null, Cv_FNodeLess = null, Cv_FNodeGreat = null;
        internal char Cv_SName;
        public static int Finalized = 0;

        public FragNode(char name)
        {
            Cv_SName = name;
        }

        ~FragNode()
        {
            Console.WriteLine("{0} finalized!", Cv_SName);
            Finalized++;
        }

        public bool Root
        {
            get
            {
                return Cv_bRoot;
            }

            set
            {
                Cv_bRoot = value;
            }
        }

        public char Name
        {
            get
            {
                return Cv_SName;
            }

        }


        public FragNode Parent
        {
            get
            {
                return Cv_FNodeUpper;
            }

            set
            {
                Cv_FNodeUpper = value;
            }
        }


        public FragNode Lesser
        {
            get
            {
                return Cv_FNodeLess;
            }

            set
            {
                Cv_FNodeLess = value;
            }
        }



        public FragNode Larger
        {
            get
            {
                return Cv_FNodeGreat;
            }

            set
            {
                Cv_FNodeGreat = value;
            }
        }

    }

}
