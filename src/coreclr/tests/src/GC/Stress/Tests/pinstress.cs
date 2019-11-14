// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PinStress
{
    internal class Node
    {
        private byte[] _b;
        private GCHandle _gch;
        private bool _freed = true;

        public Node()
        {
            _b = new byte[84900];
            _gch = GCHandle.Alloc(_b, GCHandleType.Pinned);
            _freed = false;
        }


        ~Node()
        {
            // in case someone forgets to call Free()
            Free();
        }


        public void Free()
        {
            // this check is necessary to avoid possibly double-freeing the handle
            if (!_freed)
            {
                _gch.Free();
                _freed = true;
                GC.SuppressFinalize(this);
            }
        }


        public bool IsFree
        {
            get
            {
                return _freed;
            }
        }
    }



    public class PinStress
    {
        public static int Main(string[] args)
        {
            Random r;
            int randomSeed = 0;
            float percentDelete = 0.0F;
            float percentFree = 0.0F;
            int numNodes = 0;
            int numPasses = 0;

            if (args.Length == 0)
            {
                // use defaults
                percentDelete = 0.6F;
                percentFree = 0.99F;
                numNodes = 10000;
                numPasses = 5;
            }
            else if (args.Length == 5)
            {
                //take command-line args for random seed, %delete, %free, numNodes, numPasses

                if (!Int32.TryParse(args[0], out randomSeed))
                {
                    Console.WriteLine("Invalid randomSeed");
                    return 0;
                }

                if (!Single.TryParse(args[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out percentDelete))
                {
                    Console.WriteLine("Invalid percentDelete");
                    return 0;
                }
                if ((percentDelete < 0) || (percentDelete > 1.0))
                {
                    Console.WriteLine("Invalid percentDelete");
                    return 0;
                }

                if (!Single.TryParse(args[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out percentFree))
                {
                    Console.WriteLine("Invalid percentFree");
                    return 0;
                }
                if ((percentFree < 0) || (percentFree > 1.0))
                {
                    Console.WriteLine("Invalid percentFree");
                    return 0;
                }

                if (!Int32.TryParse(args[3], out numNodes))
                {
                    Console.WriteLine("Invalid numNodes");
                    return 0;
                }
                if (numNodes <= 0)
                {
                    Console.WriteLine("Invalid numNodes");
                    return 0;
                }

                if (!Int32.TryParse(args[4], out numPasses))
                {
                    Console.WriteLine("Invalid numPasses");
                    return 0;
                }
            }
            else
            {
                Console.WriteLine("USAGE: pinstress.exe <randomSeed> <percentDelete> <percentFree> <numNodes> <numPasses>");
                Console.WriteLine("\t where percent* is a float between 0.0 and 1.0");
                return 0;
            }

            r = new Random(randomSeed);
            List<Node> list = new List<Node>();

            // loop
            for (int j = 0; j != numPasses; j++)
            {
                // repopulate the list
                while (list.Count < numNodes)
                {
                    list.Add(new Node());
                }

                // unpin percentFree% of the nodes
                for (int i = 0; i < numNodes * percentFree; i++)
                {
                    int index = r.Next(list.Count - 1);
                    if (!list[index].IsFree)
                    {
                        // only unpin if we haven't already freed
                        list[index].Free();
                    }
                }

                // delete percentDelete% of the freed nodes
                for (int i = 0; i < numNodes * percentDelete; i++)
                {
                    int index = r.Next(list.Count - 1);
                    if (list[index].IsFree)
                    {
                        // this will fragment the heap
                        list.RemoveAt(index);
                    }
                }

                // shrink the List to free up as much memory as possible
                list.TrimExcess();

                Console.WriteLine("Pass {0}", j);
            }

            Console.WriteLine("Test Passed");
            return 100;
        }
    }
}
