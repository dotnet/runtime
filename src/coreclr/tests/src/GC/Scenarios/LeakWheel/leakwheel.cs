// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//************************************************************************/
//*Test:    LeakWheel
//*Purpose: simulate real world objects allocating and deleting condation.
//*Description: It create an object table with "iTable" items. Random number
//* generator will generate number between 0 to iTable, that is ID in the
//* Table. object will be added or deleted from that table item.
//* Oject may be varied size, may create a new thread doing the same thing
//* like main thread. may be a link list. Delete Object may delete single
//* object, delete a list of object or delete all objects. While create
//* object, if the table item has had one object, put this object as it's
//* child object to make a link list. This tests covered link list, Variant
//* array, Binary tree, finalize, multi_thread, collections, WeakReference.
//*Arguments:   Arg1:iMem(MB), Arg2: iIter(Number of iterations), Arg3:iTable, Arg4: iSeed
//************************************************************************/
namespace DefaultNamespace {
    using System.Threading;
    using System;
    using System.IO;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    internal class LeakWheel
    {
        internal static int iSeed;
        internal static int iIter;
        internal static int iTable;
        internal static int iMem;
        internal Node LstNode;

        public static int Main( String [] Args)
        {
            // console synchronization Console.SetOut(TextWriter.Synchronized(Console.Out));

            /*max memory will be used. If heap size is bigger than this, */

            // delete all the objects. Default 10MB
            iMem = 10;

            //How many iterations
            iIter = 1500000;

            //Max items number in the object table
            iTable = 500;

            //Seed for generate random iKey
            iSeed = (int)DateTime.Now.Ticks; 

            switch( Args.Length )
            {
                case 1:
                    try{
                        iMem = Int32.Parse( Args[0] );
                    }
                    catch(FormatException)
                    {
                        Console.WriteLine("FormatException is caught");
                    }
                break;
                case 2:
                    try{
                        iMem = Int32.Parse( Args[0] );
                        iIter = Int32.Parse( Args[1] );
                    }
                    catch(FormatException )
                    {
                        Console.WriteLine("FormatException is caught");
                    }
                break;
                case 3:
                    try{
                        iMem = Int32.Parse( Args[0] );
                        iIter = Int32.Parse( Args[1] );
                        iTable = Int32.Parse( Args[2] );
                    }
                    catch(FormatException )
                    {
                        Console.WriteLine("FormatException is caught");
                    }
                 break;
                case 4:
                    try
                    {
                        iMem = Int32.Parse(Args[0]);
                        iIter = Int32.Parse(Args[1]);
                        iTable = Int32.Parse(Args[2]);
                        iSeed = Int32.Parse(Args[3]);
                    }
                    catch (FormatException)
                    {
                        Console.WriteLine("FormatException is caught");
                    }
                break;
            }

            Console.WriteLine("Repro with these values:");
            Console.WriteLine("iMem= {0} MB, iIter= {1}, iTable={2} iSeed={3}", iMem, iIter, iTable, iSeed );

            LeakWheel my_obj = new LeakWheel();

            while(!my_obj.RunGame())
            {
                GC.Collect(2);
                GC.WaitForPendingFinalizers();
                GC.Collect(2);

                Console.WriteLine( "After Delete and GCed all Objects: {0}", GC.GetTotalMemory(false) );
               
            }
            
            GC.Collect(2);
            GC.WaitForPendingFinalizers();

            Thread.Sleep(100);
            GC.Collect(2);
            GC.WaitForPendingFinalizers();
            GC.Collect(2);
            GC.WaitForPendingFinalizers();
            GC.Collect(2);
            GC.WaitForPendingFinalizers();

            Console.WriteLine("When test finished: {0}", GC.GetTotalMemory(false));
            Console.WriteLine("Created VarAry objects: {0} Finalized VarAry Objects: {1}", Node.iVarAryCreat, Node.iVarAryFinal);
            Console.WriteLine("Created BitArray objects: {0} Finalized BitArray Objects: {1}", Node.iBitAryCreat, Node.iBitAryFinal);
            Console.WriteLine("Created small objects: {0} Finalized small Objects: {1}", Node.iSmallCreat, Node.iSmallFinal);
            Console.WriteLine("Created BinaryTree objects: {0} Finalized BinaryTree Objects: {1}", Node.iBiTreeCreat, Node.iBiTreeFinal);
            Console.WriteLine("Created Thread objects: {0} Finalized Thread Objects: {1}", Node.iThrdCreat, Node.iThrdFinal);


            if (Node.iBitAryCreat == Node.iBitAryFinal &&
                    Node.iBiTreeCreat == Node.iBiTreeFinal &&
                    Node.iSmallCreat == Node.iSmallFinal &&
                    Node.iThrdCreat == Node.iThrdFinal &&
                    Node.iVarAryCreat == Node.iVarAryFinal)
            {
                Console.WriteLine("Test Passed!");
                return 100;
            }

            Console.WriteLine("Test Failed!");
            return 1;

        }
        
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public void DestroyLstNode() {
            LstNode = null;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public bool RunGame()
        {
            Dictionary<int, WeakReference> oTable = new Dictionary<int, WeakReference>(10);
            DestroyLstNode(); //the last node in the node chain//
            Random r = new Random (LeakWheel.iSeed);

            for(int i=0; i<iIter; i++)
            {
                SpinWheel(oTable, LstNode, r);
               
                if( GC.GetTotalMemory(false)/(1024*1024) >= iMem )
                {
                    DestroyLstNode();

                    iIter -= i;  // Reduce the iteration count by how far we went
                    return false;
                } 
            } 
            DestroyLstNode();
            return true;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public void SpinWheel( Dictionary<int, WeakReference> oTable,  Node node, Random r )
        {
            int iKey;//the index which the new node will be set at
            Node nValue;//the new node
            bool bDel;

            //Console.WriteLine( "start spinwheel ");

            iKey = r.Next( 0, LeakWheel.iTable );

            if( iKey%2 == 0 ) //decide whether delete or create a node.
                bDel = true; //delete
            else
                bDel = false;

            if( !bDel )
            {
                nValue = CreateNode( iKey );

                if( oTable.ContainsKey(iKey) && (oTable[iKey]).IsAlive )
                {
                    SetChildNode(oTable, iKey, nValue );
                }
                else
                {
                    LstNode = SetNodeInTable(iKey, nValue, node, oTable);
                }
            }
            else
            {
                DeleteNode( iKey, oTable);
            }
            //if (iKey % 100 == 0)
            //{
            //    Console.WriteLine("HeapSize: {0}", GC.GetTotalMemory(false));
            //}
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public void DeleteNode( int iKey, Dictionary<int, WeakReference> oTable)
        {
            //iSwitch is 0, delete one child Node at iKey;
            //is 1, del one object and its childred;
            //is 2, del the object at iKey and all the next objects;
            //is 3, del the all objects in object chain.
            int iSwitch = iKey%4;
            Node thisNode;

            if( oTable.ContainsKey( iKey ) )
            {
                WeakReference wRef = oTable[iKey];
                if( wRef.IsAlive )
                {
                    thisNode = (Node)wRef.Target;
                    switch( iSwitch )
                    {
                        case 0:
                            Node childNode = thisNode;
                            if( childNode.Child != null )
                            {//delete one child Node at iKey if there is,
                                while( childNode.Child != null )
                                {
                                    childNode = childNode.Child;
                                }
                                childNode = childNode.Parent;
                                childNode.Child = null;
                                break;
                            }
                            else goto case 1; //otherwise del this Node in "case 1" (the node is shared with "case 1" );
                        case 1: //del one object and its childred from nodes chain;
                            if( thisNode.Last != null )
                            {
                                thisNode.Last.Next = thisNode.Next;
                                if( thisNode.Next != null )
                                {
                                    thisNode.Next.Last = thisNode.Last;
                                }
                            }
                            else
                            {
                                if( thisNode.Next != null )
                                    thisNode.Next.Last = null;
                            }
                        break;
                        case 2: //del the object at iKey and all the next objects;
                            if( thisNode.Last != null )
                                thisNode.Last = null;
                            else
                                thisNode = null;
                        break;
                        case 3://del the all objects in object chain.
                            Node Last = thisNode;
                            while( Last.Last != null )
                            {
                                Last = Last.Last;
                            }
                            Last = null;
                        break;
                    }//end of switch
                }
                else
                    oTable[iKey] = null;
            }
        }

        public Node SetNodeInTable(int iKey, Node nValue, Node LstNode, Dictionary<int, WeakReference> oTable )
        {
            /**************************************************/
            /* save new node in a chain, all the node is      */
            /* refereced by this chain, Table only have their */
            /* Weakreferece. So when delete a node, only need */
            /* to delete the ref in this chain.               */
            /**************************************************/
            if( LstNode == null )
                LstNode = nValue;
            else
            {
                LstNode.Next = nValue ;
                LstNode.Next.Last = LstNode;
                LstNode = LstNode.Next;
            }
            WeakReference wRef = new WeakReference( LstNode, false );
            if( oTable.ContainsKey(iKey) )
            {
                oTable[iKey] = wRef;
            }
            else
            {
                oTable.Add( iKey, wRef );
            }
            return LstNode; //keep the last node fresh in chain
        }

        public void SetChildNode( Dictionary<int, WeakReference> oTable, int iKey, Node nValue )
        {
            WeakReference wRef= oTable[iKey];
            WeakReference wRefChild = wRef;
            Node thisNode = (Node)wRefChild.Target;
            Node ChildNode = thisNode;

            while( ChildNode.Child != null )
            {
                ChildNode = ChildNode.Child;
            }
            ChildNode.Child = nValue;
            ChildNode.Child.Parent = ChildNode;
        }

        public Node CreateNode( int iKey )
        {
            Node newNode = new Node( );
            switch( iKey%5 )
            {
            //case 0://1 out of 4 nodes are thread node.
            //    newNode.SetThread( );
            //break;
            case 1://This node include a binary tree
                newNode.SettreeNode( iKey );
            break;
            case 2: //This node with a Variant array.
                newNode.SetVararyNode( iKey );
            break;
            case 3: //This node with a BitArray
                newNode.SetBitArrayNode( iKey );
            break;
            case 0:
            case 4: //small node
                newNode.SetSmallNode( iKey );
            break;
            }
            return newNode;
        }

        public void ThreadNode()
        {
            Dictionary<int, WeakReference> oTable = new Dictionary<int, WeakReference>( 10);
            DestroyLstNode(); //the last node in the node chain//
            Random  r = new Random (LeakWheel.iSeed);
            LeakWheel mv_obj = new LeakWheel();

            while (true)
            {
                mv_obj.SpinWheel( oTable, LstNode, r );

                if( GC.GetTotalMemory(false) >= LeakWheel.iMem*60 )
                {
                    DestroyLstNode();

                    GC.Collect( );
                    GC.WaitForPendingFinalizers();
                    GC.Collect( );
                    Console.WriteLine( "After Delete and GCed all Objects: {0}", GC.GetTotalMemory(false) );
                }
            }
        }
    }


    internal class Node
    {
        internal static int iVarAryCreat=0;
        internal static int iVarAryFinal=0;
        internal static int iBitAryCreat=0;
        internal static int iBitAryFinal=0;
        internal static int iSmallCreat=0;
        internal static int iSmallFinal=0;
        internal static int iBiTreeCreat=0;
        internal static int iBiTreeFinal=0;
        internal static int iThrdCreat=0;
        internal static int iThrdFinal=0;

        internal Node Last;
        internal Node Next;
        internal Node Parent;
        internal Node Child;
// disabling unused variable warning
#pragma warning disable 0414
        internal Object vsMem;
#pragma warning restore 0414
        internal Thread ThrdNode;
        internal int itype; //0=VarAry;1=BitAry;2=small;3=binarytree;4=Thread

        public Node()
        {
            Last = null;
            Next = null;
            Parent = null;
            Child = null;
            ThrdNode = null;
            itype = -1;
        }

        public void SetVararyNode( int iKey )
        {
            int iSize = iKey%30;
            if (iSize == 0)
            {
                iSize = 30;
            }
            Object [] VarAry = new Object[iSize];
            double [] dmem;
            for( int i=0; i < iSize; i++ )
            {
                dmem= new double[1+i];
                dmem[0] = (double)0;
                dmem[i] = (double)i;
                VarAry[i] = ( dmem );
            }

            vsMem = ( VarAry );
            itype = 0;
            AddObjectToRecord();
        }

        public void SetBitArrayNode( int iKey )
        {
            vsMem = ( new BitArray( iKey, true ) );
            itype = 1;
            AddObjectToRecord();
        }

        public void SetSmallNode( int iKey )
        {
            itype = 2;
            AddObjectToRecord();
            vsMem = ( iKey );
        }

        public void SettreeNode( int iKey )
        {
            itype = 3;
            AddObjectToRecord();
            TreeNode nTree = new TreeNode();
            nTree.Populate(iKey%10, nTree);
            vsMem = ( nTree );
        }

        public void SetThread()
        {
            itype = 4;
            AddObjectToRecord();
            LeakWheel mv_obj = new LeakWheel();
            mv_obj.ThreadNode();
        }



        ~Node()
        {
            //that whould be interesting to see what happens if we don't stop the thread
            //this thread is created in this node, this node go away, this the object chain
            //is local variable in ThreadNode, it will go away too. What this thread is going to do?

            //if( ThrdNode != null )
            //{
            //    ThrdNode.Abort();
            //    ThrdNode.Join();
            //}
            DelObjectFromRecord( );
        }

        public void AddObjectToRecord()
        {
            lock(this) {
                switch( itype )
                {
                case 0:
                    Node.iVarAryCreat++;
                    break;
                case 1:
                    Node.iBitAryCreat++;
                    break;
                case 2:
                    Node.iSmallCreat++;
                    break;
                case 3:
                    Node.iBiTreeCreat++;
                    break;
                case 4:
                    Node.iThrdCreat++;
                    break;
                }
            }
        }

        public void DelObjectFromRecord( )
        {
            lock(this)
            {
                switch( itype )
                {
                case 0:
                    Node.iVarAryFinal++;
                    break;
                case 1:
                    Node.iBitAryFinal++;
                    break;
                case 2:
                    Node.iSmallFinal++;
                    break;
                case 3:
                    Node.iBiTreeFinal++;
                    break;
                case 4:
                    Node.iThrdFinal++;
                    break;
                }
            }
        }
    }

    internal class TreeNode
    {
        internal TreeNode left;
        internal TreeNode right;
        internal byte [] mem;
        public TreeNode() { }

        // Build tree top down, assigning to older objects.
        internal void Populate(int iDepth, TreeNode thisNode)
        {
            if (iDepth<=0)
            {
                return;
            }
            else
            {
              mem = new byte[iDepth];
              mem[0] = 0;
              mem[iDepth-1] = (byte)iDepth;
              iDepth--;
              thisNode.left  = new TreeNode();
              thisNode.right = new TreeNode();
              Populate (iDepth, thisNode.left);
              Populate (iDepth, thisNode.right);
            }
        }
    }
}
