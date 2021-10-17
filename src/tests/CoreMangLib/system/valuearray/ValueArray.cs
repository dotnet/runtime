

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class Program
{
    private static int returnVal = 100;

    public static int Main(string[] args)
    {
        TestSizeOf();
        TestLength();
        TestByValueSemantic();
        TestIndexing();
        TestSlice();
        TestGCRootsRef();
        TestGCRootsStruct();
        TestListT();
        TestAsField();
        TestInFixed();

        if (returnVal == 100)
            Console.WriteLine("PASS");

        return returnVal;
    }

    private unsafe static void TestInFixed()
    {
        Console.WriteLine(nameof(TestInFixed));

        var arr1 = new ValueArray<long, object[,,,,,,,,]>();
        for (int i = 0; i < arr1.Length; i++)
        {
            arr1[i] = i;
        }

        fixed (long* p = arr1)
        {
            for (long* pElement = p; pElement < p + 9; pElement++)
            {
                *pElement = *pElement + 1;
            }
        }

        for (int i = 0; i < arr1.Length; i++)
        {
            Test(i + 1, arr1[i]);
        }
    }

    class QuadTree
    {
        private ValueArray<QuadTree, object[,,,]> _nodes;

        // NB: intentionally returning byval here - just to test byval returning.
        public ValueArray<QuadTree, object[,,,]> Nodes
        {
            get
            {
                return _nodes;
            }
        }

        public QuadTree(int depth)
        {
            if (depth > 0)
            {
                for (int i = 0; i < _nodes.Length; i++)
                {
                    _nodes[i] = new QuadTree(depth - 1);
                }
            }
        }

        public int CountNodes()
        {
            int val = 1;
            for (int i = 0; i < Nodes.Length; i++)
            {
                val += Nodes[i]?.CountNodes() ?? 0;
            }

            return val;
        }
    }

    private static void TestAsField()
    {
        Console.WriteLine(nameof(TestAsField));
        QuadTree tree = new QuadTree(10);
        Test(1398101, tree.CountNodes());
    }

    private unsafe static void TestSizeOf()
    {
        Console.WriteLine(nameof(TestSizeOf));

        Test(sizeof(int) * 4, sizeof(ValueArray<int, object[,,,]>));
        Test(sizeof(nint) * 5, sizeof(ValueArray<nint, object[,,,,]>));
        Test(sizeof(ValueArray<byte, object[,,,,]>) * 6, sizeof(ValueArray<ValueArray<byte, object[,,,,]>, object[,,,,,]>));
    }

    private static void TestIndexing()
    {
        Console.WriteLine(nameof(TestIndexing));

        var arr1 = new ValueArray<bool, object[,,,,,,,,]>();
        for (int i = 0; i < arr1.Length; i++)
        {
            arr1[i] = i % 2 == 0;
        }

        for (int i = 0; i < arr1.Length; i++)
        {
            Test(i % 2 == 0, arr1[i]);
        }
    }

    private static void TestSlice()
    {
        Console.WriteLine(nameof(TestSlice));

        var arr1 = new ValueArray<bool, object[,,,,,,,,]>();
        for (int i = 0; i < arr1.Length; i++)
        {
            arr1[i] = i % 2 == 0;
        }

        var span = arr1.Slice(0);
        Test(9, span.Length);
        for (int i = 0; i < span.Length; i++)
        {
            Test(i % 2 == 0, span[i]);
        }
    }

    private static void TestLength()
    {
        Console.WriteLine(nameof(TestLength));

        Test(1, new ValueArray<int, object[]>().Length);
        Test(2, new ValueArray<int, object[,]>().Length);
        Test(3, new ValueArray<int, object[,,]>().Length);
        Test(4, new ValueArray<int, object[,,,]>().Length);
        Test(42, new ValueArray<int, object[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]>().Length);
    }

    private static void TestByValueSemantic()
    {
        Console.WriteLine(nameof(TestByValueSemantic));

        var arr1 = new ValueArray<int, object[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]>();
        for (int i = 0; i < arr1.Length; i++)
        {
            arr1[i] = i;
        }

        var arr2 = arr1;
        for (int i = 0; i < arr1.Length; i++)
        {
            arr1[i]++;
        }

        for (int i = 0; i < arr1.Length; i++)
        {
            Test(i, arr2[i]);
            Test(i + 1, arr1[i]);
        }

        // NB: also testing boxing here.
        IEquatable<ValueArray<int, object[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]>> ieq = arr1;

        var arr3 = arr1;
        Test(true, ieq.Equals(arr3));
        Test(arr3, arr1);
        arr3[10] = -1;

        Test(false, ieq.Equals(arr3));
        Test(false, arr1.Equals(arr3));
    }

    private static void TestGCRootsRef()
    {
        Console.WriteLine(nameof(TestGCRootsRef));

        var arrStr1 = new ValueArray<string, object[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]>();
        for (int i = 0; i < arrStr1.Length; i++)
        {
            arrStr1[i] = (i * 10000).ToString();
        }

        var junk = arrStr1;
        for (int i = 0; i < arrStr1.Length; i++)
        {
            Test((i * 10000).ToString(), junk[i]);
        }

        GC.Collect();
        GC.Collect();

        var arrStr2 = new ValueArray<string, object[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]>();
        for (int i = 0; i < arrStr2.Length; i++)
        {
            arrStr2[i] = (i * 12345).ToString();
        }

        for (int i = 0; i < 42; i++)
        {
            Test((i * 10000).ToString(), arrStr1[i]);
        }
    }

    public struct BSI
    {
        public byte B;
        public string S;
        public int I;
    }

    private static void TestGCRootsStruct()
    {
        Console.WriteLine(nameof(TestGCRootsStruct));

        var arrS1 = new ValueArray<BSI, object[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]>();
        for (int i = 0; i < 42; i++)
        {
            arrS1[i] = new BSI { S = (i * 10000).ToString() };
        }

        var arrS11 = arrS1;
        for (int i = 0; i < 42; i++)
        {
            Test((i * 10000).ToString(), arrS11[i].S);
        }

        GC.Collect();
        GC.Collect();

        var arrS2 = new ValueArray<BSI, object[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]>();
        for (int i = 0; i < 42; i++)
        {
            arrS2[i] = new BSI { S = (i * 12345).ToString() };
        }

        for (int i = 0; i < 42; i++)
        {
            Test((i * 10000).ToString(), arrS1[i].S);
        }
    }

    private static void TestListT()
    {
        Console.WriteLine(nameof(TestListT));

        var list = new List<ValueArray<BSI, object[,,,,,,,,]>>();

        for (int j = 0; j < 100; j++)
        {
            var arrS1 = new ValueArray<BSI, object[,,,,,,,,]>();
            for (int i = 0; i < arrS1.Length; i++)
            {
                arrS1[i] = new BSI { S = (i * j).ToString() };
            }

            list.Add(arrS1);
        }

        for (int j = 0; j < 100; j++)
        {
            var arrS1 = list[j];
            int i = 0;
            foreach (ref var refS in arrS1.Slice(0))
            {
                GC.Collect();

                Test((i++ * j).ToString(), refS.S);
            }
        }
    }

    public static void Test<T>(T expected, T actual) where T : IEquatable<T>
    {
        if (!expected.Equals(actual))
        {
            Console.WriteLine($"Fail: {expected}, {actual}");
            returnVal++;
        }
    }
}
