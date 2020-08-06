// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml.XPath;

namespace System.Xml.Xsl.XPath
{
    // TODO-NULLABLE: Replace [MaybeNull] with ? once https://github.com/dotnet/runtime/pull/40197 is in.
    internal interface IXPathBuilder<Node>
    {
        // Should be called once per build
        void StartBuild();

        // Should be called after build for result tree post-processing
        [return: MaybeNull]
        [return: NotNullIfNotNull("result")]
        Node EndBuild([AllowNull] Node result);

        Node String(string value);

        Node Number(double value);

        Node Operator(XPathOperator op, [AllowNull] Node left, [AllowNull] Node right);

        Node Axis(XPathAxis xpathAxis, XPathNodeType nodeType, string? prefix, string? name);

        Node JoinStep(Node left, Node right);

        // http://www.w3.org/TR/xquery-semantics/#id-axis-steps
        // reverseStep is how parser comunicates to builder diference between "ansestor[1]" and "(ansestor)[1]"
        Node Predicate(Node node, Node condition, bool reverseStep);

        Node Variable(string prefix, string name);

        Node Function(string prefix, string name, IList<Node> args);
    }
}
