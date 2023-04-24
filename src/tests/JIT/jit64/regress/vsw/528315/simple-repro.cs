// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
internal enum NodeType
{
    True, False, Not, Other
}

internal class Node
{
    public NodeType NodeType;
    public Node Child;
    public string name;

    public Node(string s) { name = s; }
}
internal class NodeFactory
{
    public Node Conditional(Node condition, Node trueBranch, Node falseBranch)
    {
        switch (condition.NodeType)
        {
            case NodeType.True:
                return trueBranch;
            case NodeType.False:
                return falseBranch;
            case NodeType.Not:
                return this.Conditional(condition.Child, falseBranch, trueBranch);  // <-- tail recursion
        }
        return falseBranch;  //<- should return the original trueBranch
    }

    public class Test_simple_repro
    {
        [Fact]
        public static int TestEntryPoint()
        {
            NodeFactory f = new NodeFactory();

            Node notNode = new Node("NotNode");
            notNode.NodeType = NodeType.Not;
            notNode.Child = new Node("otherNode");
            notNode.Child.NodeType = NodeType.Other;

            Node trueNode = new Node("True");
            Node falseNode = new Node("False");

            Node resultNode = f.Conditional(notNode, trueNode, falseNode);

            if (resultNode.name == "True")
            {
                System.Console.WriteLine("pass");
                return 100;
            }
            else
            {
                System.Console.WriteLine("Failed");
                return -1;
            }
        }
    }
}
