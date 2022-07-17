// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 * Changes:
 *   -removed hardcoded Dyanmo parameters
 *   -allow Dynamo parameters to be passed from command-line
 *   -checks 3 random nodes, as well as first & last nodes
 *   -added ability to pass random seed
 *   -outputs random seed for reproducibility
 *   -fixed crashes, caught exceptions
 *   -improved analysis logic
 *   -converted getters/setters to properties
 *   -removed unused/useless methods
 *   -general code cleanup
 *
 * As far as I can tell, this code creates one StaticNode that links to 2 DynamoNodes
 * which in turn links to an array of RandomNodes for each DynamoNode.  It then kills
 * 5 StaticNodes by  setting them to null.  It then checks to see if the StaticNodes'
 * finalizers have been run, thus cleaning up the linked nodes.
 *
 * There are quite a few arrays and ints that don't seem to be used for anything,
 * as labeled below.
 * Notes:
 *   -passes with complus_gcstress=0,1,2,3,4
 *   -passes with complus_hitminops
 *   -passes in debug
 */


namespace Dynamo {
    using System;
    using System.Threading;
    using System.Diagnostics;

    public class Dynamo
    {
        protected StaticNode [] NodeBank;
        protected int [] LessRandomValues;
        protected int [] LargeRandomValues;
        protected int [] DynamoValues = new int[2]; // what is this for?
        protected int StaticValue;                  // what is this for?

        protected int [] ChkRandomValues;
        protected int ChkRandomNum = 0;
        protected int [] ChkDynamoValues;           // what is this for?
        protected int ChkDynamoNum = 0;             // what is this for?
        protected int ChkStaticValue;               // what is this for?

        public static int Main( String [] args)
        {
            int seed = (int)DateTime.Now.Ticks;
            if ( args.Length==3 )
            {
                if (!Int32.TryParse(args[2], out seed))
                {
                    // incorrect value passed to Dynamo
                    Usage();
                    return 1;
                }
            }
            else if ( args.Length!=2 ) {
                // incorrect number of parameters
                Usage();
                return 1;
            }

            Dynamo Mv_Dynamo;
            int numElements, numDynamics;

            if (!Int32.TryParse(args[0], out numElements) || !Int32.TryParse(args[1], out numDynamics))
            {
                Usage();
                return 1;
            }

            try
            {
                Mv_Dynamo = new Dynamo( numElements, numDynamics );
            } catch ( ArgumentException e) {
                // incorrect value passed to Dynamo
                Console.WriteLine("Dynamo: " + e.Message);
                return 1;
            }

            if ( Mv_Dynamo.RunTest(seed ))
                return 100; //pass

            return 1;   //fail
        }


        // prints usage message to console
        public static void Usage() {
            Console.WriteLine("Usage: Dynamo n m [seed]");
            Console.WriteLine("       where n is the number of elements");
            Console.WriteLine("       and m is the number of dynamo nodes");
            Console.WriteLine("       ( m<=n; m,n>10 )");
            Console.WriteLine("       seed is an optional random seed, by default DateTime.Now.Ticks is used");
        }

        // begins the test
        public bool RunTest(int randomSeed )
        {
            Console.WriteLine( "Total amount of RandomNode Memory: "  );
            Console.WriteLine( RandomNode.TotalMemory );
            Console.WriteLine( " " );
            Console.WriteLine( "Running Finalize Test..." );
            Console.WriteLine( " ");

            Prep( );
            bool result = false;

            // kill the first and last nodes
            if (KillNode(0) && KillNode(NodeBank.Length-1) ) {

                // kill three random nodes
                result = true;
                int[] randNums = {0, 0, 0};
                for (int i=0; i<2; i++) {
                    // choose a random index to kill
                    Random rand = new Random(randomSeed);
                    do {
                        randNums[i] = rand.Next() % NodeBank.Length;
                        // make sure we don't kill a previously killed node!
                    } while ( (randNums[i]==0)
                               || (randNums[i]==NodeBank.Length-1)
                               || (randNums[i]==randNums[(i+1)%3])
                               || (randNums[i]==randNums[(i+2)%3]) );

                    if (!KillNode(randNums[i])) {
                        result = false;
                        break;
                    }
                }
            }

            Console.WriteLine();
            if ( result )
                Console.WriteLine("Test Passed with seed: " + randomSeed);
            else
                Console.WriteLine("Test Failed with seed: " + randomSeed);

            return result;
        }


        public void Prep( )
        {
            StaticNode.Cv_Dynamo = this; // No worries - static fields are not references - they are roots
            ChkRandomValues = new int[ NodeBank[0].SmallNode.Length + NodeBank[0].LargeNode.Length ];
        }


        public void AnalyzeNode( int StaticNode )
        {
            LessRandomValues = new int[ NodeBank[ StaticNode ].SmallNode.Length];
            LargeRandomValues = new int [ NodeBank[ StaticNode ].LargeNode.Length ];

            DynamoValues[ 0 ] = NodeBank[ StaticNode ].SmallNode.Value;
            DynamoValues[ 1 ] = NodeBank[ StaticNode ].LargeNode.Value;

            Debug.Assert(NodeBank[ StaticNode ].SmallNode.Length == NodeBank[ StaticNode ].LargeNode.Length);

            for( int i = 0; i < NodeBank[ StaticNode ].SmallNode.Length; i++ )  {
                LessRandomValues[ i ] = NodeBank[ StaticNode ].SmallNode[i].Value;
                LargeRandomValues[ i ] = NodeBank[ StaticNode ].LargeNode[i].Value;
            }

            StaticValue = NodeBank[ StaticNode ].Value;
        }


        public bool Compare( )
        {
            Thread.CurrentThread.Join(10);

            // First check to see if the right number of Random Nodes are destroyed
            int ChkRan = ( LessRandomValues.Length + LargeRandomValues.Length );

            if( ChkRandomNum != ChkRan)
            {
                Console.WriteLine( "The registered number: " + ChkRan  );
                Console.WriteLine( "The analyzed number: " + ChkRandomNum  );
                Console.WriteLine( "The registered number and analyzed number are not equal" );
                return false;
            }

            // Note: This does not find a node if it was cleaned up by mistake.
            for( int i = 0; i < ChkRan; i++ )
            {
                int CheckValue = ChkRandomValues[ i ];
                bool foundLess = false, foundLarge = false;

                foreach (int j in LessRandomValues)
                {
                    if( CheckValue == j )  {
                        foundLess = true;
                        break;
                    }
                }

                if ( !foundLess )
                {
                    foreach (int j in LargeRandomValues)
                    {
                        if( CheckValue == j )  {
                            foundLarge = true;
                            break;
                        }
                    }

                    if ( !foundLarge )
                    {
                        Console.WriteLine( "Match not found! Random Node: ");
                        Console.WriteLine( CheckValue );
                        Console.WriteLine(" did not get cleaned up.");
                        return false;
                    }

                }

            }

            return true;
        }


        public void RegisterCleanup( int Type, int Value )
        {
            switch( Type )
            {
                case 0:
                    // static node
                    ChkStaticValue = Value;
                break;
                case 1:
                    // dynamo node
                    ChkDynamoValues[ ChkDynamoNum++ ] = Value;
                break;
                case 2:
                    // random node
                    if (ChkRandomNum<ChkRandomValues.Length)
                        ChkRandomValues[ ChkRandomNum++ ] = Value;
                    break;
                default:
                    Console.WriteLine( "Test error has occurred: Unknown Type - {0} given", Type );
                break;
            }
        }


        public bool KillNode( int Node)
        {
            Console.WriteLine("Deleting Node:");
            Console.WriteLine( Node );

            AnalyzeNode( Node );        // <- Finalizers not supported
            NodeBank[ Node ] = null;    // <- Finalizers not supported

            GC.Collect(); // Could attempt to cause GC instead of attempting to force it
            GC.WaitForPendingFinalizers();
            GC.Collect();

            bool Result = Compare( ); // finalizers not supported

            ChkDynamoNum = 0; // finalizers not supported
            ChkRandomNum = 0; // finalizers not supported

            return Result;
        }


        public Dynamo( int numElements, int numDynamics )
        {

            Console.WriteLine(" ");

            if ( (numElements < numDynamics ) || (numDynamics<10))
                throw new ArgumentException();
            else
            {
                ChkDynamoValues = new int[numDynamics];
                NodeBank = new StaticNode[numDynamics/2];
                int iDynamic = numElements / numDynamics;

                int Low = 0;
                int High = iDynamic * 2;

                for( int i = 0; i < NodeBank.Length; i++ )
                {
                    NodeBank[i] = new StaticNode( Low, High );

                    Low = High;
                    High += iDynamic * 2;
                }
            }

        }

    }
}
