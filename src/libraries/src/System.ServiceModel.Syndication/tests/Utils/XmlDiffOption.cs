// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.ServiceModel.Syndication.Tests
{
    [Flags]
    public enum XmlDiffOption
    {
        None = 0x0,
        IgnoreEmptyElement = 0x1,
        IgnoreWhitespace = 0x2,
        IgnoreComments = 0x4,
        IgnoreAttributeOrder = 0x8,
        IgnoreNS = 0x10,
        IgnorePrefix = 0x20,
        IgnoreDTD = 0x40,
        IgnoreChildOrder = 0x80,
        InfosetComparison = 0xB,     //sets IgnoreEmptyElement, IgnoreWhitespace and IgnoreAttributeOrder
        CDataAsText = 0x100,
        NormalizeNewline = 0x200, // ignores newlines in text nodes only
        ConcatenateAdjacentTextNodes = 0x400, // treats adjacent text nodes as a single node
        TreatWhitespaceTextAsWSNode = 0x800, // if a text node contains only whitespaces, it will be considered a whitespace node
        ParseAttributeValuesAsQName = 0x1000, // <a xmlns:p1="n1" t="p1:foo"/> will be treated the same as <a xmlns:p2="n1" t="p2:foo"/>
        DontWriteMatchingNodesToOutput = 0x2000, // output will only contain different nodes
        DontWriteAnythingToOutput = 0x4000, // output will not contain anything (needed for very large XML docs, which could trigger OOM)
        IgnoreEmptyTextNodes = 0x8000, // empty text nodes (sometimes produced by the binary reader) are ignored
        WhitespaceAsText = 0x10000, // consider whitespace nodes as text nodes
    }

    public class XmlDiffAdvancedOptions
    {
        public XmlDiffAdvancedOptions() => AddedNamespaces = new Dictionary<string, string>();

        public string IgnoreNodesExpr { get; set; }

        public string IgnoreValuesExpr { get; set; }

        public string IgnoreChildOrderExpr { get; set; }

        public void AddNamespace(string prefix, string uri) => AddedNamespaces[prefix] = uri;

        public bool HadAddedNamespace() => AddedNamespaces.Count != 0;

        public IDictionary<string, string> AddedNamespaces { get; }
    }
}
