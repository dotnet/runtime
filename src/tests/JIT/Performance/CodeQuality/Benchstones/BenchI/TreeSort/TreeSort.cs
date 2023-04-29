// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Benchstone.BenchI
{
public static class TreeSort
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 1200;
#endif

    const int SortElements = 5000;
    const int Modulus = 65536;

    sealed class Node
    {
        public Node Left;
        public Node Right;
        public int Val;
        public Node(int n)
        {
            Left = null;
            Right = null;
            Val = n;
        }
    }

    static int s_biggest;
    static int s_littlest;
    static int s_seed;

    static void InitRand() {
        s_seed = 33;
    }

    static int Rand(ref int seed) {
        int multiplier = 25173;
        int increment = 13849;
        seed = (multiplier * seed + increment) % Modulus;
        return seed;
    }

    static void InitArray(int[] sortList) {
        InitRand();
        s_biggest = 0;
        s_littlest = Modulus;
        for (int i = 1; i <= SortElements; i++) {
            sortList[i] = Rand(ref s_seed) - 1;
            if (sortList[i] > s_biggest) {
                s_biggest = sortList[i];
            }
            else if (sortList[i] < s_littlest) {
                s_littlest = sortList[i];
            }
        }
    }

    static void Insert(int n, Node t) {
        if (n > t.Val) {
            if (t.Left == null) {
                t.Left = new Node(n);
            }
            else {
                Insert(n, t.Left);
            }
        }
        else if (n < t.Val) {
            if (t.Right == null) {
                t.Right = new Node(n);
            }
            else {
                Insert(n, t.Right);
            }
        }
    }

    static bool CheckTree(Node p) {
        bool result = true;
        if (p.Left != null) {
            if (p.Left.Val <= p.Val) {
                result = false;
            }
            else {
                result &= CheckTree(p.Left);
            }
        }

        if (p.Right != null) {
            if (p.Right.Val >= p.Val) {
                result = false;
            }
            else {
                result &= CheckTree(p.Right);
            }
        }

        return result;
    }

    static bool Trees(int[] sortList) {
        InitArray(sortList);
        Node tree = new Node(sortList[1]);
        for (int i = 2; i <= SortElements; i++) {
            Insert(sortList[i], tree);
        }
        bool result = CheckTree(tree);
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Bench() {
        int[] sortList = new int[SortElements + 1];
        bool result = Trees(sortList);
        return result;
    }

    static bool TestBase() {
        bool result = true;
        for (int i = 0; i < Iterations; i++) {
            result &= Bench();
        }
        return result;
    }

    [Fact]
    public static int TestEntryPoint() {
        bool result = TestBase();
        return (result ? 100 : -1);
    }
}
}
