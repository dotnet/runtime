// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace ILCompiler.PettisHansenSort
{
    public class CallGraphNode
    {
        public CallGraphNode(int index)
        {
            Index = index;
        }

        public int Index { get; }
        public Dictionary<CallGraphNode, long> OutgoingEdges { get; } = new Dictionary<CallGraphNode, long>();

        public void IncreaseEdge(CallGraphNode callee, long count)
        {
            if (OutgoingEdges.TryGetValue(callee, out long curCount))
                OutgoingEdges[callee] = curCount + count;
            else
                OutgoingEdges.Add(callee, count);
        }

        public override string ToString() => Index.ToString();
    }
}
