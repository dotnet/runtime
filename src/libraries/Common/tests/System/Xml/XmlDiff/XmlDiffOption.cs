// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.XmlDiff
{
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
        NormalizeNewline = 0x200 // ignores newlines in text nodes only
    }

    public class XmlDiffAdvancedOptions
    {
        private string _IgnoreNodesExpr;
        private string _IgnoreValuesExpr;
        private string _IgnoreChildOrderExpr;
        private XmlNamespaceManager _mngr;

        public XmlDiffAdvancedOptions()
        {
        }
        public string IgnoreNodesExpr
        {
            get
            {
                return _IgnoreNodesExpr;
            }
            set
            {
                _IgnoreNodesExpr = value;
            }
        }
        public string IgnoreValuesExpr
        {
            get
            {
                return _IgnoreValuesExpr;
            }
            set
            {
                _IgnoreValuesExpr = value;
            }
        }
        public string IgnoreChildOrderExpr
        {
            get
            {
                return _IgnoreChildOrderExpr;
            }
            set
            {
                _IgnoreChildOrderExpr = value;
            }
        }
        public XmlNamespaceManager Context
        {
            get
            {
                return _mngr;
            }
            set
            {
                _mngr = value;
            }
        }
    }
}
