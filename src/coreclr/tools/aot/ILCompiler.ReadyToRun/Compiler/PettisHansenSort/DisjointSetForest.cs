// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ILCompiler.PettisHansenSort
{
    public class DisjointSetForest
    {
        private Node[] _nodes;

        /// <summary>
        /// Construct a new forest with the specified number of disjoint sets.
        /// </summary>
        public DisjointSetForest(int numNodes)
        {
            _nodes = new Node[numNodes];
            for (int i = 0; i < _nodes.Length; i++)
                _nodes[i].Parent = i;

            NumNodes = numNodes;
            NumDisjointSets = numNodes;
        }

        /// <summary>
        /// Gets the count of disjoint sets that are currently entered in this forest.
        /// </summary>
        public int NumDisjointSets { get; private set; }
        public int NumNodes { get; private set; }

        // Add a new disjoint set.
        public int Add()
        {
            if (NumNodes >= _nodes.Length)
                Array.Resize(ref _nodes, NumNodes * 2);

            int index = NumNodes;
            _nodes[index].Parent = index;
            NumDisjointSets++;
            NumNodes++;

            return index;
        }

        public int FindSet(int node)
        {
            if (node < 0 || node >= _nodes.Length)
                throw new ArgumentOutOfRangeException(nameof(node), node,
                                                      "Node must be positive and less than number of nodes");

            return FindSetInternal(node);
        }

        private int FindSetInternal(int node)
        {
            int parent = _nodes[node].Parent;
            if (parent != node)
                _nodes[node].Parent = parent = FindSetInternal(parent);

            return parent;
        }

        public bool Union(int x, int y)
        {
            x = FindSet(x);
            y = FindSet(y);

            if (x == y)
                return false;

            // Make smallest a child of the largest
            if (_nodes[y].Rank > _nodes[x].Rank)
                _nodes[x].Parent = y;
            else
            {
                _nodes[y].Parent = x;
                if (_nodes[x].Rank == _nodes[y].Rank)
                    _nodes[x].Rank++;
            }

            NumDisjointSets--;
            return true;
        }

        private struct Node
        {
            public int Parent;
            public int Rank;
        }
    }
}
