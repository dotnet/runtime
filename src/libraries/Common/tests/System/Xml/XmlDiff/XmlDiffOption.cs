// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        NormalizeNewline = 0x200,   // ignores newlines in text nodes only
        NormalizeSpaces = 0x400     // converts all forms of spaces to a normal space
    }

    public class XmlDiffAdvancedOptions
    {
        internal const string SpaceStripPattern = "[\u00A0\u180E\u2000-\u200B\u202F\u205F\u3000\uFEFF]";

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
