// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        return falseBranch;  //<- should return the orignal trueBranch
    }

    private class Test
    {
        public static int Main()
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
