// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace DefaultNamespace {
    using System;
    using System.Runtime.CompilerServices;
   

    internal class Continue
    {
// disabling unused variable warning
#pragma warning disable 0414
        internal static Object StObj;

        public class CreateObj
        {
            BNode obj;

#pragma warning restore 0414
            [MethodImplAttribute(MethodImplOptions.NoInlining)]
            public CreateObj()
            {
                Continue mv_Obj = new Continue();

                for( int i=1; i< 1000; i++)
                {
                    obj = new BNode(i); //create new one and delete the last one.
                    mv_Obj.CreateNode( i ); //create locate objects in createNode().
                }

                Console.Write(BNode.icCreateNode); 
                Console.WriteLine(" Nodes were created.");
            }

            [MethodImplAttribute(MethodImplOptions.NoInlining)]
            public void DestroyObj()
            {
                obj = null;
            }


            [MethodImplAttribute(MethodImplOptions.NoInlining)]
            public void ResurrectNodes()
            {
                for (int i = 0; i < BNode.rlNodeCount; i++)
                {
                    BNode oldNode = (BNode)BNode.rlNode[i];
                    if ( oldNode.mem[0] != 99 )
                    {
                        Console.WriteLine( "One Node is not resurrected correctly.");
                    }
                    oldNode = null;
                    BNode.rlNode[ i ] = null;
                }
            }

            public bool RunTest()
            {
                DestroyObj();

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                Console.Write(BNode.icFinalNode); 
                Console.WriteLine(" Nodes were finalized and resurrected.");

                ResurrectNodes();

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                return ( BNode.icCreateNode == BNode.icFinalNode );
            }
        }


        public static int Main()
        {

            Console.WriteLine("Test should return with ExitCode 100 ...");
            CreateObj temp = new CreateObj();

            if(temp.RunTest())
            {
                Console.WriteLine("Test Passed");
                return 100;
            }
            Console.WriteLine("Test Failed");
            return 1;
        }


        ~Continue()
        {
            Continue.StObj = ( this );
            Console.WriteLine( "Main class Finalize().");

        }

        public void CreateNode( int i )
        {
            BNode rgobj = new BNode( i );
        }
    }


    internal class BNode
    {
        public static int icCreateNode = 0;
        public static int icFinalNode = 0;
        public static int rlNodeCapacity = 2000;
        public static int rlNodeCount = 0;
        internal static BNode[] rlNode = new BNode[rlNodeCapacity];
        public int [] mem;
        public BNode( int i )
        {

            icCreateNode++;

            mem = new int[i];
            mem[0] = 99;
            if(i > 1 )
            {
                mem[mem.Length-1] = mem.Length-1;
            }


        }


        ~BNode()
        {
            icFinalNode++;

            //resurrect objects
            if (rlNodeCount == rlNodeCapacity)
            {
                rlNodeCapacity = rlNodeCapacity * 2;
                BNode[] newrlNode = new BNode[rlNodeCapacity*2];

                for (int i = 0; i < rlNodeCount; i++)
                {
                    newrlNode[i] = rlNode[i];
                }
                rlNode = newrlNode;
            }
            rlNode[rlNodeCount] = this;
            rlNodeCount++;
        }
    }
}
