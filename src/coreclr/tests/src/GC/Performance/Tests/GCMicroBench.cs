// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace GC_Microbenchmarks
{
    // the type of objects allocated
    // value refers to object size
    public enum ObjectType
    {
        Undefined = -1,
        Small = 8000,
        SmallFinal = 8500, // small finalizable object
        Large = 85000,
        ExtraLarge = 20*1024*1024
    }


    // the condition to satisfy before ending the benchmark
    public enum AllocationCondition
    {
        Undefined,
        HeapSize,
        Segments,
        Objects
    }


    // the object that gets allocated
    public class Node
    {
        public Node(int dataSize)
        {
            data = new byte[dataSize];
        }

        public byte[] data;
    }


    // the finalizable object that gets allocated
    public class FNode : Node
    {
        public FNode(int dataSize)
            : base(dataSize)
        {
        }

        // puts object on finalization list
        ~FNode()
        {
        }
    }


    public class GCMicroBench
    {
        // the minimum size of a segment
        // if reserved mem increases by more than MinSegmentSize, then we can assume we've allocated a new segment
        public const long MinSegmentSize = 4 * 1024 * 1024;
        public const string ObjTypeParam = "/objtype:";
        public const string ConditionParam = "/condition:";
        public const string AmountParam = "/amount:";

        // test members
        private List<Node> m_list = new List<Node>();     // holds the allocated objects
        private ObjectType m_objType = ObjectType.Undefined;
        private AllocationCondition m_allocCondition = AllocationCondition.Undefined;
        private long m_amount = 0;


        // outputs the usage information for the app
        public static void Usage()
        {
            Console.WriteLine("USAGE: /objtype: /condition: /amount:");
            Console.WriteLine("where");
            Console.WriteLine("\tobjtype = [small|smallfinal|large|extralarge]");
            Console.WriteLine("\tcondition = [heapsize|segments|objects]");
            Console.WriteLine("\tamount = the number that satisfies the condition (ex: number of objects)");
        }

        public static void Main(string[] args)
        {
            GCMicroBench test = new GCMicroBench();
            if (!test.ParseArgs(args))
            {
                Usage();
                return;
            }
            test.RunTest();

        }


        public bool ParseArgs(string[] args)
        {
            if (args.Length != 3)
            {
                return false;
            }

            for (int i = 0; i < args.Length; i++)
            {
                args[i] = args[i].ToLower();

                if (args[i].StartsWith(ObjTypeParam))
                {

                    if (m_objType != ObjectType.Undefined)
                    {
                        return false;
                    }

                    switch (args[i].Substring(ObjTypeParam.Length))
                    {

                        case "small":
                            m_objType = ObjectType.Small;
                            break;
                        case "smallfinal":
                            m_objType = ObjectType.SmallFinal;
                            break;
                        case "large":
                            m_objType = ObjectType.Large;
                            break;
                        case "extralarge":
                            m_objType = ObjectType.ExtraLarge;
                            break;
                        default:
                            return false;
                    }
                }
                else if (args[i].StartsWith(ConditionParam))
                {

                    if (m_allocCondition != AllocationCondition.Undefined)
                    {
                        return false;
                    }

                    switch (args[i].Substring(ConditionParam.Length))
                    {

                        case "heapsize":
                            m_allocCondition = AllocationCondition.HeapSize;
                            break;
                        case "segments":
                            m_allocCondition = AllocationCondition.Segments;
                            break;
                        case "objects":
                            m_allocCondition = AllocationCondition.Objects;
                            break;
                        default:
                            return false;
                    }
                }
                else if (args[i].StartsWith(AmountParam))
                {

                    if (m_amount != 0)
                    {
                        return false;
                    }

                    if ((!Int64.TryParse(args[i].Substring(AmountParam.Length), out m_amount)) || (m_amount <= 0))
                    {
                        Console.WriteLine("amount must be greater than 0");
                        return false;
                    }

                    if ( (m_allocCondition == AllocationCondition.HeapSize) && ( m_amount <= GC.GetTotalMemory(false) ) )
                    {
                        Console.WriteLine("amount must be greater than current heap size");
                        return false;
                    }

                }
                else
                {
                    return false;
                }

            }

            return true;
        }


        public void RunTest()
        {
            // allocate objType objects until heap size >= amount bytes
            if (m_allocCondition == AllocationCondition.HeapSize)
            {
                while (GC.GetTotalMemory(false) <= m_amount)
                {
                    Allocate();
                }
            }
            // allocate amount objType objects
            else if (m_allocCondition == AllocationCondition.Objects)
            {
                for (long i = 0; i < m_amount; i++)
                {
                    Allocate();
                }
            }
            // allocate objType objects until reserved VM increases by minimum segment size, amount times
            // (this is an indirect way of determining if a new segment has been allocated)
            else if (m_allocCondition == AllocationCondition.Segments)
            {
                long reservedMem;

                for (long i = 0; i < m_amount; i++)
                {
                    reservedMem = Process.GetCurrentProcess().VirtualMemorySize64;

                    do
                    {
                        Allocate();
                    }
                    while (Math.Abs(Process.GetCurrentProcess().VirtualMemorySize64 - reservedMem) < MinSegmentSize);
                }
            }

            // allocations done
            Deallocate();
        }


        public void Allocate()
        {

            Node n;

            // create new finalizable object
            if (m_objType == ObjectType.SmallFinal)
            {
                n = new FNode((int)m_objType);
            }
            else
            {
                n = new Node((int)m_objType);
            }

            m_list.Add(n);

        }


        public bool ClearList()
        {
            if (m_list != null)
            {
                m_list.Clear();
                m_list = null;
                return ( (m_list.Count > 0) && (m_list[0] is FNode));
            }
            return false;
        }


        // releases references to allocated objects
        // times GC.Collect()
        // if objects are finalizable, also times GC.WaitForPendingFinalizers()
        public void Deallocate()
        {
            bool finalizable = ClearList();

            GC.Collect();

            if (finalizable)
            {
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

        }


    }
}
